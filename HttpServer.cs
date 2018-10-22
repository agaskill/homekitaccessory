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
    using Microsoft.Owin;
    using Newtonsoft.Json.Linq;
    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    class HttpServer
    {
        private volatile bool running;
        TcpListener tcpServer;

        private Func<Action<JObject>, OwinMiddleware> middlewareFactory;

        public HttpServer(Func<Action<JObject>, OwinMiddleware> middlewareFactory)
        {
            this.middlewareFactory = middlewareFactory;
        }

        public async Task Listen(int port)
        {
            tcpServer = new TcpListener(IPAddress.Any, port);
            tcpServer.Start();
            running = true;

            while (running) {
                TcpClient client;
                try
                {
                    client = await tcpServer.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                Task.Run(() => HandleClient(client, middlewareFactory(OnNotification)));
            }
        }

        private void OnNotification(JObject data)
        {

        }

        public void Stop()
        {
            running = false;
            tcpServer.Stop();
        }

        private async Task HandleClient(TcpClient client, OwinMiddleware middleware)
        {
            Console.WriteLine("Connection from {0}", client.Client.RemoteEndPoint);
            var stream = client.GetStream();
            byte[] buffer = new byte[102400];
            var pos = 0;
            for(;;)
            {
                var readlen = await stream.ReadAsync(buffer, pos, buffer.Length - pos);
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
                        var ctx = new OwinContext();
                        var responseBody = new MemoryStream();
                        ctx.Request.Scheme = "http";
                        ctx.Request.Method = requestMethod;
                        ctx.Request.Path = new PathString(url.AbsolutePath);
                        ctx.Request.PathBase = new PathString();
                        ctx.Request.Protocol = requestProtocol;
                        ctx.Request.QueryString = string.IsNullOrEmpty(url.Query) ?
                            new QueryString() :
                            new QueryString(url.Query.Substring(1));
                        ctx.Response.Body = responseBody;
                        ctx.Request.CallCancelled = new CancellationToken();
                        ctx.Response.Headers.Append("Server", "EventedHttpServer");

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
                            ctx.Request.Headers.Append(headerName, headerValue);
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
                            ctx.Request.Body = new MemoryStream(body);
                        } else {
                            Console.WriteLine("No body content");
                            ctx.Request.Body = Stream.Null;
                        }

                        Console.WriteLine("Calling app function");
                        try
                        {
                            await middleware.Invoke(ctx);
                            Console.WriteLine("App function complete");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            ctx.Response.StatusCode = 500;
                            ctx.Response.ReasonPhrase = ex.Message;
                            responseBody = new MemoryStream(Encoding.UTF8.GetBytes(ex.ToString()));
                            ctx.Response.ContentLength = responseBody.Length;
                            ctx.Response.ContentType = "text/plain";
                        }

                        var ms = new MemoryStream();
                        var rw = new StreamWriter(ms);
                        rw.NewLine = "\r\n";

                        var responseProtocol = ctx.Response.Protocol ?? ctx.Request.Protocol;

                        rw.Write(ctx.Response.Protocol ?? ctx.Request.Protocol);
                        rw.Write(" ");
                        rw.Write(ctx.Response.StatusCode);
                        if (!string.IsNullOrWhiteSpace(ctx.Response.ReasonPhrase)) {
                            rw.Write(" ");
                            rw.Write(ctx.Response.ReasonPhrase);
                        }
                        rw.WriteLine();
                        foreach (var headerkv in ctx.Response.Headers)
                        {
                            foreach (var headerValue in headerkv.Value) {
                                rw.Write(headerkv.Key);
                                rw.Write(": ");
                                rw.WriteLine(headerValue);
                            }
                        }
                        rw.WriteLine();

                        rw.Flush();

                        ms.Position = 0;

                        await ms.CopyToAsync(stream);

                        responseBody.Position = 0;

                        await responseBody.CopyToAsync(stream);

                        Console.WriteLine("Request complete");

                        pos = 0;
                        continue;
                    }
                }
            }

            client.Dispose();
            Console.WriteLine("Client disconnect normally");
        }
    }
}