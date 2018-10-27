namespace HomeKitAccessory.Pairing
{
    using System;
    using SRPAuth;

    public class StaticSetupCodeUserStore : IUserStore
    {
        private byte[] salt;
        private byte[] verifier;

        public StaticSetupCodeUserStore(byte[] salt, byte[] verifier)
        {
            this.salt = salt;
            this.verifier = verifier;
        }
        
        public UserVerifier Lookup(string identity)
        {
            if (identity != "Pair-Setup")
                throw new ArgumentException("Only Pair-Setup identity is supported");
            
            var groupParameters = SRPGroupParameters.Rfc5054_3072;
            return new UserVerifier
            {
                Prime = groupParameters.Prime,
                Generator = groupParameters.Generator,
                Salt = salt,
                Verifier = verifier
            };
        }
    }
}