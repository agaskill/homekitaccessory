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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    class HttpServer
    {
        private volatile bool running;
        TcpListener tcpServer;

        private Func<IHttpConnection, OwinMiddleware> middlewareFactory;

        public HttpServer(Func<IHttpConnection, OwinMiddleware> middlewareFactory)
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
                var httpConnection = new HttpConnection(client, middlewareFactory);
                new Thread(() =>
                {
                    try
                    {
                        httpConnection.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }).Start();
            }
        }

        public void Stop()
        {
            running = false;
            tcpServer.Stop();
        }
    }

    public interface IHttpConnection
    {
        void SendEvent(JObject data);
    }

    class HttpConnection : IHttpConnection
    {
        private TcpClient client;
        private Stream stream;
        private OwinMiddleware middleware;
        byte[] readBuffer = new byte[102400];
        int readPos = 0;
        private object sendLock = new object();

        public HttpConnection(TcpClient client, Func<IHttpConnection, OwinMiddleware> middlewareFactory)
        {
            this.client = client;
            stream = client.GetStream();
            middleware = middlewareFactory(this);
        }

        public void SendEvent(JObject data)
        {
            var bodyms = new MemoryStream();
            var sw = new StreamWriter(bodyms);
            var jw = new JsonTextWriter(sw);
            data.WriteTo(jw);
            jw.Flush();
            bodyms.Position = 0;

            var ms = new MemoryStream();
            sw = new StreamWriter(ms);
            sw.NewLine = "\r\n";
            sw.WriteLine("EVENT/1.0 200 OK");
            sw.WriteLine("Date: {0:r}", DateTime.UtcNow);
            sw.WriteLine("Content-Type: application/hap+json");
            sw.WriteLine("Content-Length: {0}", bodyms.Length);
            sw.WriteLine();
            sw.Flush();

            bodyms.CopyTo(ms);
            ms.Position = 0;

            lock (sendLock)
            {
                ms.CopyTo(stream);
            }
        }

        public void Start()
        {
            Console.WriteLine("Connection from {0}", client.Client.RemoteEndPoint);
            for(;;)
            {
                var readlen = stream.Read(readBuffer, readPos, readBuffer.Length - readPos);
                if (readlen == 0) break;
                Console.WriteLine("Read {0} bytes from client", readlen);
                readPos += readlen;
                Console.WriteLine("Total bytes in buffer {0}", readPos);
                for (var i = 0; i < readPos - 3; i++) {
                    if (readBuffer[i] == 0x0d && readBuffer[i + 1] == 0x0a && readBuffer[i + 2] == 0x0d && readBuffer[i + 3] == 0x0a)
                    {
                        // Complete header block available
                        var headerEnd = i + 4;
                        Console.WriteLine("Headers {0} bytes", headerEnd);
                        var reader = new StreamReader(new MemoryStream(readBuffer, 0, headerEnd));
                        var requestLineRaw = reader.ReadLine();
                        Console.WriteLine("requestLine: " + requestLineRaw);
                        var requestLine = requestLineRaw.Split(' ');
                        var requestMethod = requestLine[0];
                        var pathAndQuery = requestLine[1].Split('?', 2);
                        var requestProtocol = requestLine[2];
                        Console.WriteLine("Method: " + requestMethod);
                        Console.WriteLine("Path: " + requestLine[1]);
                        Console.WriteLine("Protocol: " + requestProtocol);
                        var ctx = new OwinContext();
                        var responseBody = new MemoryStream();
                        ctx.Request.Scheme = "http";
                        ctx.Request.Method = requestMethod;
                        ctx.Request.Path = new PathString(pathAndQuery[0]);
                        ctx.Request.PathBase = new PathString();
                        ctx.Request.Protocol = requestProtocol;
                        if (pathAndQuery.Length > 1)
                            ctx.Request.QueryString = new QueryString(pathAndQuery[1]);
                        else
                            ctx.Request.QueryString = new QueryString();
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
                            Console.WriteLine("{0} bytes already read", readPos - headerEnd);
                            var body = new byte[contentLength];
                            Array.Copy(readBuffer, headerEnd, body, 0, readPos - headerEnd);
                            contentLength -= (readPos - headerEnd);
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
                            middleware.Invoke(ctx).Wait();
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
                        responseBody.Position = 0;

                        lock (sendLock)
                        {
                            ms.CopyTo(stream);
                            responseBody.CopyTo(stream);
                        }

                        Console.WriteLine("Request complete");

                        if (ctx.Environment.TryGetValue("hap.ReadKey", out object readKey) &&
                            ctx.Environment.TryGetValue("hap.WriteKey", out object writeKey))
                        {
                            Console.WriteLine("Setting encryption keys");
                            stream = new HapEncryptedStream(stream, (Sodium.Key)readKey, (Sodium.Key)writeKey);
                        }

                        readPos = 0;
                        continue;
                    }
                }
            }

            client.Dispose();
            Console.WriteLine("Client disconnect normally");
        }
    }
}