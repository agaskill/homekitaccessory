using System.Collections.Generic;
using System;
using HomeKitAccessory.Data;

namespace HomeKitAccessory.Pairing.PairSetupStates
{
    abstract class PairSetupState
    {
        public virtual TLVCollection HandlePairSetupRequest(TLVCollection request, out PairSetupState newState)
        {
            throw new InvalidOperationException("Pair setup is not permitted in current state");
        }

        public virtual TLVCollection HandlePairVerifyRequest(TLVCollection request, out PairSetupState newState)
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