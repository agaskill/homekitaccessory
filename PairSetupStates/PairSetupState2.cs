using System;
using System.Collections.Generic;

namespace HomeKitAccessory.PairSetupStates
{
    class PairSetupState2 : PairSetupState
    {
        private SRPAuth.SRPServer sRPServer;

        public PairSetupState2(Server server, SRPAuth.SRPServer sRPServer)
            : base(server)
        {
            this.sRPServer = sRPServer;
        }

        public override List<TLV> HandlePairSetupRequest(List<TLV> request, out PairSetupState newState)
        {
            Console.WriteLine("Handling pair setup request in state 2");

            var state = GetState(request);
            if (state != 3)
                throw new InvalidOperationException("Invalid request state " + state);

            var devicePublic = request.Find(x => x.Tag == TLVType.PublicKey).DataValue;
            var deviceProof = request.Find(x => x.Tag == TLVType.Proof).DataValue;

            //TODO: Verify client proof

            var premasterSecret = sRPServer.ClientExchange(devicePublic);
            var srpKey = sRPServer.ComputeStandardKey(premasterSecret);
            var serverProof = sRPServer.ComputeServerProof(deviceProof, srpKey);

            newState = new PairSetupState4(server, srpKey);

            Console.WriteLine("Sending state 4 response");

            return new List<TLV>() {
                new TLV(TLVType.State, 4),
                new TLV(TLVType.Proof, serverProof)
            };
        }
    }
}