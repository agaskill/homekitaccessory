using System.Security.Cryptography;
using SRPAuth;

namespace HomeKitAccessory
{
    class PairSetupUserStore : IUserStore
    {
        private string setupCode;

        public PairSetupUserStore(string setupCode)
        {
            this.setupCode = setupCode;
        }

        public UserVerifier Lookup(string identity)
        {
            if (identity != "Pair-Setup") return null;
            return SRPAuth.UserVerifier.Create(
                identity, setupCode,
                SRPAuth.SRPGroupParameters.Rfc5054_3072,
                SHA512.Create());
        }
    }
}