using System.Collections.Generic;
using System;

namespace HomeKitAccessory.PairSetupStates
{
    abstract class PairSetupState
    {
        public virtual List<TLV> HandlePairSetupRequest(List<TLV> request, out PairSetupState newState)
        {
            throw new InvalidOperationException("Pair setup is not permitted in current state");
        }

        public virtual List<TLV> HandlePairVerifyRequest(List<TLV> request, out PairSetupState newState)
        {
            throw new InvalidOperationException("Pair verify is not permitted in current state");
        }

        public virtual void UpdateEnvironment(IDictionary<string, object> env)
        { }

        protected Server server;

        public PairSetupState(Server server)
        {
            this.server = server;
        }

        protected int GetState(List<TLV> request)
        {
            return (int)request.Find(x => x.Tag == TLVType.State).IntegerValue;
        }
    }
}