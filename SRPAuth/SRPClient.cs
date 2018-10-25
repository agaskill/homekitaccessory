using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace SRPAuth
{
    public class SRPClient : SRPBase
    {
        private BigInteger a;

        public SRPClient(HashAlgorithm hashAlgorithm) : base(hashAlgorithm) { }

        public ClientSRPSession ServerExchange(string name, string password, ServerSRPParams serverSRP)
        {
            return ServerExchange(name, password, serverSRP, GenRandom(32));
        }

        public ClientSRPSession ServerExchange(string name, string password, ServerSRPParams serverSRP, byte[] secret)
        {
            N = serverSRP.Prime.ToUnsignedBigEndian();
            g = serverSRP.Generator.ToUnsignedBigEndian();
            B = serverSRP.ServerPublic.ToUnsignedBigEndian();

            if (B % N == 0)
                throw new ArgumentException("illegal parameter", nameof(serverSRP));

            //a = Utilities.ParseByteArray(@"60975527035CF2AD1989806F0407210BC81EDC04E2762A56AFD529DDDA2D4393").ToUnsignedBigEndian();
            a = secret.ToUnsignedBigEndian();

            A = BigInteger.ModPow(g, a, N);
            var u = CalculateU();
            var k = CalculateK();
            var x = UserVerifier.CalculateX(hashAlgorithm, name, password, serverSRP.Salt);

            var premasterSecret = BigInteger.ModPow(
                ((B - (k * BigInteger.ModPow(g, x, N))) % N + N) % N,
                (a + (u * x)) % N,
                N);

            return new ClientSRPSession
            {
                ClientPublic = A.ToUnsignedBigEndian(),
                PremasterSecret = premasterSecret.ToUnsignedBigEndian()
            };
        }
    }
}
