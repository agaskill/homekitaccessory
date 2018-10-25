using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Numerics;

namespace SRPAuth
{
    public class SRPServer : SRPBase
    {
        private IUserStore userStore;
        private BigInteger b;
        private BigInteger v;

        public SRPServer(IUserStore userStore, HashAlgorithm hashAlgorithm)
            : base(hashAlgorithm)
        {
            this.userStore = userStore;
        }

        public ServerSRPParams ClientHello(string name)
        {
            return ClientHello(name, GenRandom(32));
        }

        public ServerSRPParams ClientHello(string name, byte[] secret)
        {
            var user = userStore.Lookup(name);
            if (user == null) throw new ArgumentException("user not found");

            N = user.Prime.ToUnsignedBigEndian();
            g = user.Generator.ToUnsignedBigEndian();

            var k = CalculateK();
            v = user.Verifier.ToUnsignedBigEndian();

            //b = Utilities.ParseByteArray(@"E487CB59D31AC550471E81F00F6928E01DDA08E974A004F49E61F5D105284D20").ToUnsignedBigEndian();
            b = secret.ToUnsignedBigEndian();

            B = ((k * v) + BigInteger.ModPow(g, b, N)) % N;

            return new ServerSRPParams
            {
                Generator = g.ToUnsignedBigEndian(),
                Prime = N.ToUnsignedBigEndian(),
                Salt = user.Salt,
                ServerPublic = B.ToUnsignedBigEndian()
            };
        }

        public byte[] ClientExchange(byte[] clientPublic)
        {
            A = clientPublic.ToUnsignedBigEndian();

            if (A % N == 0)
                throw new ArgumentException("illegal parameter", nameof(clientPublic));

            var u = CalculateU();

            var premasterSecret = BigInteger.ModPow((A * BigInteger.ModPow(v, u, N) % N), b, N);

            return premasterSecret.ToUnsignedBigEndian();
        }
    }
}
