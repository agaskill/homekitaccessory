using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using HomeKitAccessory.Net.PairSetupStates;
using Newtonsoft.Json;
using HomeKitAccessory.Data;

namespace HomeKitAccessory.Net
{
    public class HapConnection : OwinMiddleware, IDisposable
    {
        private CharacteristicHandler characteristicHandler;
        private PairSetupState pairState;
        private Server server;
        private IHttpConnection connection;

        public HapConnection(Server server, IHttpConnection connection)
            : base(null)
        {
            this.connection = connection;
            characteristicHandler = new CharacteristicHandler(server, connection.SendEvent);
            pairState = new Initial(server);
        }

        public void Dispose()
        {
            characteristicHandler.Dispose();
        }

        public override Task Invoke(IOwinContext context)
        {
            if (!context.Request.Path.HasValue)
                return NotFound(context);

            var path = context.Request.Path.Value;
            
            if (path == "/pair-setup")
            {
                return HandlePairSetup(context);
            }
            if (path == "/pair-verify")
            {
                return HandlePairVerify(context);
            }
            if (path == "/accessories")
            {
                return GetAccessoryDatabase(context);
            }
            if (path == "/characteristics")
            {
                return HandleCharacteristics(context);
            }
            return NotFound(context);
        }

        private Task GetAccessoryDatabase(IOwinContext ctx)
        {
            var response = characteristicHandler.GetAccessoryDatabase();
            return HapResponse(ctx, response);
        }

        private Task HapResponse(IOwinContext ctx, HapResponse response)
        {
            var body = new MemoryStream();
            var jw = new JsonTextWriter(new StreamWriter(body));
            response.Body.WriteTo(jw);
            jw.Flush();
            ctx.Response.ContentType = "application/hap+json";
            ctx.Response.ContentLength = body.Length;
            body.Position = 0;
            body.CopyTo(ctx.Response.Body);
            return Task.CompletedTask;
        }

        private TLVCollection ReadTLVRequest(IOwinContext ctx)
        {
            if (ctx.Request.ContentType != "application/pairing+tlv8")
                throw new InvalidOperationException("Expected TLV content type");
            return TLVCollection.Deserialize(ctx.Request.Body);
        }

        private Task TLVResponse(IOwinContext ctx, TLVCollection tLVs)
        {
            var body = tLVs.Serialize();
            ctx.Response.ContentType = "application/pairing+tlv8";
            ctx.Response.ContentLength = body.Length;
            ctx.Response.Body.Write(body);
            return Task.CompletedTask;
        }

        private Task HandlePairSetup(IOwinContext ctx)
        {
            var req = ReadTLVRequest(ctx);
            var res = pairState.HandlePairSetupRequest(req, out PairSetupState newState);
            if (newState != null) pairState = newState;
            return TLVResponse(ctx, res);
        }

        Task HandlePairVerify(IOwinContext ctx)
        {
            var req = ReadTLVRequest(ctx);
            var res = pairState.HandlePairVerifyRequest(req, out PairSetupState newState);
            if (newState != null)
            {
                pairState = newState;
                newState.UpdateEnvironment(ctx.Environment);
            }
            return TLVResponse(ctx, res);
        }

        Task HandleCharacteristics(IOwinContext context)
        {
            throw new NotImplementedException();
        }

        Task NotFound(IOwinContext context)
        {
            context.Response.StatusCode = 404;
            context.Response.ReasonPhrase = "Not found";
            context.Response.ContentType = "text/plain";
            var msg = "Not found";
            context.Response.ContentLength = msg.Length;
            return context.Response.WriteAsync(msg);
        }
    }
}