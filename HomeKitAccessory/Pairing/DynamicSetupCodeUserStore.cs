namespace HomeKitAccessory.Pairing
{
    using System;
    using System.Security.Cryptography;
    using SRPAuth;
    using NLog;

    public class DynamicSetupCodeUserStore : IUserStore
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private Action<string> displaySetupCode;
        private string setupCode;
        private UserVerifier verifier;

        public static string GenerateCode()
        {
            var code = "";
            var random = new Random();
            for (var i = 0; i < 8; i++)
            {
                code += random.Next(10);
                if (i == 2 || i == 4)
                {
                    code += "-";
                }
            }
            return code;
        }

        public DynamicSetupCodeUserStore(Action<string> displaySetupCode)
        {
            this.displaySetupCode = displaySetupCode;
        }
        public UserVerifier Lookup(string identity)
        {
            if (identity != "Pair-Setup")
                throw new ArgumentException("Only Pair-Setup identity supported", nameof(identity));
            
            if (setupCode == null)
            {
                setupCode = GenerateCode();
                logger.Info("Generated setup code {0}", setupCode);
                verifier = UserVerifier.Create(
                    identity,
                    setupCode,
                    SRPGroupParameters.Rfc5054_3072,
                    SHA512.Create());
            }
            
            displaySetupCode(setupCode);

            return verifier;
        }
    }
}