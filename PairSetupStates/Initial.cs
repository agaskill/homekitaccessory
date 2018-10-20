using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace HomeKitAccessory.PairSetupStates
{
    class Initial : PairSetupState
    {
        public Initial(Server server)
            : base(server) {}
        
        public override List<TLV> HandlePairSetupRequest(List<TLV> request, out PairSetupState newState)
        {
            var state = GetState(request);
            if (state != 1)
                throw new InvalidOperationException("Invalid request state");
            var method = (int)request.Find(x => x.Tag == TLVType.Method).IntegerValue;
            if (method != 0 && method != 1)
                throw new InvalidOperationException(string.Format("Invalid method {0}", method));
            var srpServer = new SRPAuth.SRPServer(new PairSetupUserStore(server.SetupCode), SHA512.Create());
            var userVerifier = srpServer.ClientHello("Pair-Setup");
            Console.WriteLine("Sending state 2 response");
            newState = new PairSetupState2(server, srpServer);
            return new List<TLV>() {
                new TLV(TLVType.State, 2),
                new TLV(TLVType.PublicKey, userVerifier.ServerPublic),
                new TLV(TLVType.Salt, userVerifier.Salt)
            };
        }
    }
}