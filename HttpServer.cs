namespace HomeKitAccessory
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    class HttpServer
    {
        private volatile bool running;
        TcpListener tcpServer;

        private Func<AppFunc> appFuncFactory;

        public HttpServer(Func<AppFunc> appFuncFactory)
        {
            this.appFuncFactory = appFuncFactory;
        }

        public async Task Listen(int port)
        {
            tcpServer = new TcpListener(IPAddress.Any, port);
            tcpServer.Start();
            running = true;

            while (running) {
                var client = await tcpServer.AcceptTcpClientAsync();
                var clientThread = new Thread(ClientThread);
                clientThread.Start(new Connection {
                    appFunc = appFuncFactory(),
                    client = client
                });
            }
        }

        class Connection
        {
            public AppFunc appFunc;
            public TcpClient client;
        }

        public void Stop()
        {
            running = false;
            tcpServer.Stop();
        }

        private void ClientThread(object state)
        {
            var connection = (Connection)state;
            try
            {
                HandleClient(connection.client, connection.appFunc);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            connection.client.Dispose();
        }

        private void HandleClient(TcpClient client, AppFunc appFunc)
        {
            Console.WriteLine("Connection from {0}", client.Client.RemoteEndPoint);
            var stream = client.GetStream();
            byte[] buffer = new byte[102400];
            var pos = 0;
            for(;;)
            {
                var readlen = stream.Read(buffer, pos, buffer.Length - pos);
                if (readlen == 0) break;
                Console.WriteLine("Read {0} bytes from client", readlen);
                pos += readlen;
                Console.WriteLine("Total bytes in buffer {0}", pos);
                for (var i = 0; i < pos - 3; i++) {
                    if (buffer[i] == 0x0d && buffer[i + 1] == 0x0a && buffer[i + 2] == 0x0d && buffer[i + 3] == 0x0a)
                    {
                        // Complete header block available
                        var headerEnd = i + 4;
                        Console.WriteLine("Headers {0} bytes", headerEnd);
                        var reader = new StreamReader(new MemoryStream(buffer, 0, headerEnd));
                        var requestLine = reader.ReadLine().Split(' ');
                        Console.WriteLine("requestLine: {0}", requestLine);
                        var requestMethod = requestLine[0];
                        var url = new Uri(requestLine[1]);
                        var requestProtocol = requestLine[2];
                        var env = new Dictionary<string,object>();
                        var requestHeaders = new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
                        var responseBody = new MemoryStream();
                        var responseHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                        env["owin.RequestScheme"] = "http";
                        env["owin.RequestMethod"] = requestMethod;
                        env["owin.RequestPath"] = url.AbsolutePath;
                        env["owin.RequestPathBase"] = string.Empty;
                        env["owin.RequestProtocol"] = requestProtocol;
                        env["owin.RequestQueryString"] = url.Query;
                        env["owin.RequestHeaders"] = requestHeaders;
                        env["owin.ResponseBody"] = responseBody;
                        env["owin.ResponseHeaders"] = responseHeaders;
                        env["owin.CallCancelled"] = new CancellationToken();
                        env["owin.Version"] = "1.0";
                        responseHeaders["Server"] = new[] { "EventedHttpServer" };
                        var headerLine = reader.ReadLine();
                        int contentLength = 0;
                        while (!string.IsNullOrEmpty(headerLine)) {
                            Console.WriteLine("headerLine: {0}", headerLine);
                            var header = headerLine.Split(':', 2);
                            var headerName = header[0].Trim();
                            var headerValue = header[1].Trim();
                            if (StringComparer.OrdinalIgnoreCase.Equals(headerName, "Content-Length")) {
                                contentLength = int.Parse(headerValue);
                            }
                            if (requestHeaders.TryGetValue(headerName, out string[] headerValues)) {
                                headerValues = headerValues.Append(headerValue);
                            } else {
                                headerValues = new[] { headerValue };
                            }
                            requestHeaders[headerName] = headerValues;
                            headerLine = reader.ReadLine();
                        }

                        if (contentLength > 0) {
                            Console.WriteLine("Reading {0} bytes of body", contentLength);
                            Console.WriteLine("{0} bytes already read", pos - headerEnd);
                            var body = new byte[contentLength];
                            Array.Copy(buffer, headerEnd, body, 0, pos - headerEnd);
                            contentLength -= (pos - headerEnd);
                            while (contentLength > 0) {
                                readlen = stream.Read(body, body.Length - contentLength, contentLength);
                                Console.WriteLine("Read {0} bytes of body", readlen);
                                if (readlen == 0) throw new EndOfStreamException();
                                contentLength -= readlen;
                            }
                            env["owin.RequestBody"] = new MemoryStream(body);
                        } else {
                            Console.WriteLine("No body content");
                            env["owin.RequestBody"] = Stream.Null;
                        }

                        Console.WriteLine("Calling app function");
                        try
                        {
                            appFunc(env).Wait();
                            Console.WriteLine("App function complete");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            env["owin.ResponseStatusCode"] = 500;
                            env["owin.ResponseReasonPhrase"] = ex.Message;
                            responseBody = new MemoryStream(Encoding.UTF8.GetBytes(ex.ToString()));
                            responseHeaders["Content-Length"] = new[] { responseBody.Length.ToString() };
                            responseHeaders["Content-Type"] = new[] { "text/plain" };
                        }

                        if (!env.TryGetValue("owin.ResponseStatusCode", out object responseStatusCode))
                            responseStatusCode = 200;

                        var rw = new StreamWriter(new BufferedStream(stream));

                        if (!env.TryGetValue("owin.ResponseProtocol", out object responseProtocol))
                            responseProtocol = env["owin.RequestProtocol"];
                        rw.Write((string)responseProtocol);
                        rw.Write(" ");
                        rw.Write((int)responseStatusCode);
                        if (env.TryGetValue("owin.ResponseReasonPhrase", out object reasonPhrase)) {
                            rw.Write(" ");
                            rw.Write((string)reasonPhrase);
                        }
                        rw.WriteLine();
                        foreach (var headerkv in responseHeaders)
                        {
                            foreach (var headerValue in headerkv.Value) {
                                rw.Write(headerkv.Key);
                                rw.Write(": ");
                                rw.WriteLine(headerValue);
                            }
                        }
                        rw.WriteLine();

                        rw.Flush();

                        responseBody.Position = 0;

                        responseBody.CopyTo(stream);

                        Console.WriteLine("Request complete");

                        pos = 0;
                        continue;
                    }
                }
            }

            Console.WriteLine("Client disconnect normally");
        }
    }
}