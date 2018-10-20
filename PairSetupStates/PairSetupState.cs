using System.Collections.Generic;

namespace HomeKitAccessory.PairSetupStates
{
    abstract class PairSetupState
    {
        public abstract List<TLV> HandlePairSetupRequest(List<TLV> request, out PairSetupState newState);

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