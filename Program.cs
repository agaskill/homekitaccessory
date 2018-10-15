using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HomeKitAccessory
{
    using System.Security.Cryptography;
    using System.Text;
    using SRPAuth;
    using AppFunc = Func<IDictionary<string, object>, Task>;

    class Program
    {
        static void Main(string[] args)
        {
            var signKey = Sodium.SignKeypair();

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(signKey));
            
            var server = new HttpServer(() => new Connection().Handle);

            server.Listen(3001).Wait();
        }

        class Connection
        {
            private void EnsureContentType(string[] contentTypes, string contentType)
            {
                if (contentTypes == null)
                    throw new InvalidOperationException("Invalid content type");
                if (contentTypes.Length != 1)
                    throw new InvalidOperationException("Wrong number of Content-Type headers");
                if (contentTypes[0].Split(';')[0] != contentType)
                    throw new InvalidOperationException("Wrong content type");
            }

            private List<TLV> ReadTLVRequest(IDictionary<string, object> env)
            {
                var requestHeaders = (IDictionary<string,string[]>)env["owin.RequestHeaders"];
                EnsureContentType(requestHeaders["Content-Type"], "application/pairing+tlv8");
                var requestBody = (Stream)env["owin.RequestBody"];
                return TLV.Deserialize(requestBody);
            }

            private PairSetupState pairState = new PairSetupState0();

            public Task Handle(IDictionary<string, object> env)
            {
                var requestHeaders = (IDictionary<string,string[]>)env["owin.RequestHeaders"];
                var responseHeaders = (IDictionary<string,string[]>)env["owin.ResponseHeaders"];
                var responseBody = (Stream)env["owin.ResponseBody"];
                var requestBody = (Stream)env["owin.RequestBody"];

                var path = (string)env["owin.RequestPath"];

                if (path == "/pair-setup") {
                    var req = ReadTLVRequest(env);
                    var res = pairState.HandleRequest(req, out PairSetupState newState);
                    if (newState != null) pairState = newState;
                    responseHeaders["Content-Type"] = new[] { "application/pairing+tlv8" };
                    var body = TLV.Serialize(res);
                    responseHeaders["Content-Length"] = new[] { body.Length.ToString() };
                    responseBody.Write(body);
                    return Task.CompletedTask;
                }

                Console.WriteLine(
                    new StreamReader((Stream)env["owin.RequestBody"]).ReadToEnd());

                env["owin.ResponseStatusCode"] = 500;
                env["owin.ResponseReasonPhrase"] = "Internal Server Error";
                responseHeaders["Content-Length"] = new[] { "0" };
                /*
                var ms = new MemoryStream();
                var sw = new StreamWriter(ms);
                sw.WriteLine("Working");
                sw.Flush();
                responseHeaders["Content-Type"] = new[] { "text/plain" };
                responseHeaders["Content-Length"] = new[] { ms.Length.ToString() };
                ms.Position = 0;
                ms.CopyTo(responseBody);
                */
                return Task.CompletedTask;
            }
        }
    }
}
