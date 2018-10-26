namespace HomeKitAccessory.Net
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using HomeKitAccessory.Data;
    using HomeKitAccessory.Net.PairSetupStates;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    class HttpServer
    {
        private volatile bool running;
        private TcpListener tcpServer;
        private Server server;

        public HttpServer(Server server)
        {
            this.server = server;
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
                var httpConnection = new HttpConnection(server, client);
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

    class HttpConnection
    {
        private TcpClient client;
        private Stream stream;
        byte[] readBuffer = new byte[102400];
        int readPos = 0;
        private object sendLock = new object();
        private PairSetupState pairState;
        private Server server;
        private CharacteristicHandler characteristicHandler;

        public HttpConnection(Server server, TcpClient client)
        {
            this.server = server;
            this.client = client;
            stream = client.GetStream();
            pairState = new Initial(server);
            characteristicHandler = new CharacteristicHandler(server, SendEvent);
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

        public static NameValueCollection ParseQueryString(string queryString)
        {
            var qs = new NameValueCollection();
            foreach (var item in queryString.Split('&'))
            {
                var kv = item.Split('=');
                qs.Add(
                    Uri.UnescapeDataString(kv[0]),
                    kv.Length > 1 ?
                        Uri.UnescapeDataString(kv[1]) :
                        "");
            }
            return qs;
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
                        var req = new HttpRequest();
                        var requestLine = requestLineRaw.Split(' ');
                        req.Method = requestLine[0];
                        var pathAndQuery = requestLine[1].Split('?', 2);
                        req.Path = pathAndQuery[0];
                        req.QueryString = pathAndQuery.Length > 1 ? ParseQueryString(pathAndQuery[1]) : null;
                        var requestProtocol = requestLine[2];

                        if (requestProtocol != "HTTP/1.1")
                        {
                            client.Dispose();
                            throw new InvalidOperationException("Unsupported protocol " + requestProtocol);
                        }

                        Console.WriteLine("Method: " + req.Method);
                        Console.WriteLine("Path: " + req.Path);
                        Console.WriteLine("Path and Query: " + requestLine[1]);

                        var headerLine = reader.ReadLine();
                        int contentLength = 0;
                        while (!string.IsNullOrEmpty(headerLine)) {
                            Console.WriteLine("headerLine: {0}", headerLine);
                            var header = headerLine.Split(':', 2);
                            var headerName = header[0].Trim();
                            var headerValue = header[1].Trim();
                            if (StringComparer.OrdinalIgnoreCase.Equals(headerName, "Content-Length"))
                            {
                                contentLength = int.Parse(headerValue);
                            }
                            else
                            {
                                req.RequestHeaders.Add(headerName, headerValue);
                            }
                            headerLine = reader.ReadLine();
                        }

                        byte[] body;
                        if (contentLength > 0) {
                            Console.WriteLine("Reading {0} bytes of body", contentLength);
                            Console.WriteLine("{0} bytes already read", readPos - headerEnd);
                            body = new byte[contentLength];
                            Array.Copy(readBuffer, headerEnd, body, 0, readPos - headerEnd);
                            contentLength -= (readPos - headerEnd);
                            while (contentLength > 0) {
                                readlen = stream.Read(body, body.Length - contentLength, contentLength);
                                Console.WriteLine("Read {0} bytes of body", readlen);
                                if (readlen == 0) throw new EndOfStreamException();
                                contentLength -= readlen;
                            }
                        } else {
                            Console.WriteLine("No body content");
                            body = new byte[0];
                        }

                        req.Body = body;

                        var res = new HttpResponse();

                        try
                        {
                            Console.WriteLine("Dispatching request");
                            DispatchRequest(req, res);
                            Console.WriteLine("Request complete");
                        }
                        catch (HttpException ex)
                        {
                            Console.WriteLine(ex);
                            res.StatusCode = ex.StatusCode;
                            res.StatusPhrase = ex.Message;
                            res.Body = Encoding.UTF8.GetBytes(ex.ToString());
                            res.ResponseHeaders.Clear();
                            res.ResponseHeaders["Content-Type"] = "text/plain";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            res.StatusCode = 500;
                            res.StatusPhrase = "Internal server error";
                            res.Body = Encoding.UTF8.GetBytes(ex.ToString());
                            res.ResponseHeaders.Clear();
                            res.ResponseHeaders["Content-Type"] = "text/plain";
                        }

                        res.ResponseHeaders["Server"] = "EventedHttpServer";
                        res.ResponseHeaders["Date"] = DateTime.UtcNow.ToString("r");

                        var ms = new MemoryStream();
                        var rw = new StreamWriter(ms);
                        rw.NewLine = "\r\n";

                        rw.Write("HTTP/1.1 ");
                        rw.Write(res.StatusCode);
                        if (!string.IsNullOrWhiteSpace(res.StatusPhrase)) {
                            rw.Write(" ");
                            rw.Write(res.StatusPhrase);
                        }
                        rw.WriteLine();
                        foreach (string header in res.ResponseHeaders)
                        {
                            foreach (var headerValue in res.ResponseHeaders.GetValues(header)) {
                                rw.Write(header);
                                rw.Write(": ");
                                rw.WriteLine(headerValue);
                            }
                        }
                        rw.WriteLine("Content-Length: " + (res.Body?.Length ?? 0));
                        rw.WriteLine();

                        rw.Flush();

                        ms.Position = 0;

                        lock (sendLock)
                        {
                            ms.CopyTo(stream);
                            if (res.Body != null)
                            {
                                stream.Write(res.Body);
                            }
                        }

                        Console.WriteLine("Request complete");

                        if (res.Context.TryGetValue("hap.ReadKey", out object readKey) &&
                            res.Context.TryGetValue("hap.WriteKey", out object writeKey))
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

        private void DispatchRequest(HttpRequest req, HttpResponse res)
        {
            if (req.Path == "/pair-setup")
            {
                HandlePairSetup(req, res);
            }
            else if (req.Path == "/pair-verify")
            {
                HandlePairVerify(req, res);
            }
            else if (req.Path == "/accessories")
            {
                HandleAccessoryDatabase(req, res);
            }
            else if (req.Path == "/characteristics")
            {
                HandleCharacteristics(req, res);
            }
            else if (req.Path == "/identify")
            {
                HandleIdentify(req, res);
            }
            else
            {
                throw new HttpException(404, "Not found");
            }
        }
        
        private TLVCollection GetRequestTLV(HttpRequest request)
        {
            if (request.Method != "POST")
                throw new HttpException(405, "Method not allowed");
            if (request.RequestHeaders["Content-Type"] != "application/pairing+tlv8")
                throw new HttpException(415, "Unsupported media type");
            return TLVCollection.Deserialize(request.Body);
        }

        private void SetResponseTLV(HttpResponse response, TLVCollection tLVs)
        {
            response.StatusCode = 200;
            response.StatusPhrase = "Ok";
            response.ResponseHeaders["Content-Type"] = "application/pairing+tlv8";
            response.Body = tLVs.Serialize();
        }

        private void HandlePairSetup(HttpRequest request, HttpResponse response)
        {
            var tlvRequest = GetRequestTLV(request);

            var tlvResponse = pairState.HandlePairSetupRequest(tlvRequest, out PairSetupState newState);

            if (newState != null)
            {
                pairState = newState;
            }

            SetResponseTLV(response, tlvResponse);
        }

        private void HandlePairVerify(HttpRequest request, HttpResponse response)
        {
            var tlvRequest = GetRequestTLV(request);

            var tlvResponse = pairState.HandlePairVerifyRequest(tlvRequest, out PairSetupState newState);

            if (newState != null)
            {
                pairState = newState;
                newState.UpdateEnvironment(response.Context);
            }

            SetResponseTLV(response, tlvResponse);
        }

        private const string HapContentType = "application/hap+json";
        private void SetHapResponse(HttpResponse response, HapResponse data)
        {
            response.StatusCode = data.Status;
            response.ResponseHeaders["Content-Type"] = HapContentType;
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            var jw = new JsonTextWriter(sw);
            data.Body.WriteTo(jw);
            jw.Flush();
            response.Body = ms.ToArray();
        }

        private void HandleIdentify(HttpRequest request, HttpResponse response)
        {
            if (server.IsPaired)
            {
                SetHapResponse(response, new HapResponse {
                    Status = 400,
                    Body = new JObject() { "status", -70401 }
                });
            }
            else
            {
                server.Identify();
                response.StatusCode = 204;
            }
        }

        private void HandleAccessoryDatabase(HttpRequest request, HttpResponse response)
        {
            if (pairState is Verified)
            {
                if (request.Method != "GET")
                    throw new HttpException(405, "Not implemented");
                var data = characteristicHandler.GetAccessoryDatabase();
                SetHapResponse(response, data);
            }
            else
            {
                SetHapResponse(response, new HapResponse {
                    Status = 400, 
                    Body = new JObject() {
                        { "status", -70401 }
                    }
                });
            }
        }

        private void HandleCharacteristics(HttpRequest request, HttpResponse response)
        {
            if (pairState is Verified)
            {
                if (request.Method == "GET")
                {
                    var ids = new List<AccessoryCharacteristicId>();
                    foreach (var id in request.QueryString["id"].Split(','))
                    {
                        var idpair = id.Split('.');
                        ulong aid = ulong.Parse(idpair[0]);
                        ulong iid = ulong.Parse(idpair[1]);
                        ids.Add(new AccessoryCharacteristicId(aid, iid));
                    }
                    var readRequest = new CharacteristicReadRequest();
                    readRequest.Ids = ids.ToArray();
                    readRequest.IncludeEvent = request.QueryString["ev"] == "1";
                    readRequest.IncludeMeta = request.QueryString["meta"] == "1";
                    readRequest.IncludePerms = request.QueryString["perms"] == "1";
                    readRequest.IncludeType = request.QueryString["type"] == "1";

                    var hapResponse = characteristicHandler.HandleCharacteristicReadRequest(readRequest);

                    SetHapResponse(response, hapResponse);
                }
                else if (request.Method == "POST")
                {
                    var writeRequest = new JsonSerializer().Deserialize<CharacteristicWriteRequest>(
                        new JsonTextReader(
                            new StreamReader(
                                new MemoryStream(request.Body))));
                    var hapResponse = characteristicHandler.HandleCharacteristicWriteRequest(writeRequest);

                    SetHapResponse(response, hapResponse);
                }
                else
                {
                    throw new HttpException(405, "Not implemented");
                }
            }
        }
    }
}