using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace SRPAuth
{
    public class SRPBase
    {
        protected BigInteger N;
        protected BigInteger g;
        protected HashAlgorithm hashAlgorithm;
        protected BigInteger A;
        protected BigInteger B;

        private int? _lengthN;

        protected int LengthN
        {
            get {
                if (!_lengthN.HasValue) {
                    _lengthN = N.ToUnsignedBigEndian().Length;
                }
                return _lengthN.Value;
            }
        }

        protected SRPBase(HashAlgorithm hashAlgorithm)
        {
            this.hashAlgorithm = hashAlgorithm;
        }

        public static byte[] Pad(byte[] data, int length)
        {
            if (data.Length < length)
            {
                var newdata = new byte[length];
                Array.Copy(data, 0, newdata, newdata.Length - data.Length, data.Length);
                data = newdata;
            }
            return data;
        }

        protected byte[] Pad(byte[] data)
        {
            return Pad(data, LengthN);
        }

        protected byte[] GenRandom(int size)
        {
            var secret = new byte[size];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetNonZeroBytes(secret);
            }
            return secret;
        }

        protected BigInteger CalculateK()
        {
            var nbytes = N.ToUnsignedBigEndian();
            var gbytes = Pad(g.ToUnsignedBigEndian(), nbytes.Length);
            var khash = hashAlgorithm.ComputeHash(nbytes, gbytes);
            return khash.ToUnsignedBigEndian();
        }

        protected BigInteger CalculateU()
        {
            var abytes = Pad(A.ToUnsignedBigEndian());
            var bbytes = Pad(B.ToUnsignedBigEndian());
            var abhash = hashAlgorithm.ComputeHash(abytes, bbytes);
            return abhash.ToUnsignedBigEndian();
        }

        public byte[] ComputeStandardKey(byte[] premasterSecret)
        {
            return hashAlgorithm.ComputeHash(premasterSecret);
        }

        public byte[] ComputeClientProof(string username, byte[] salt, byte[] key)
        {
            var hash1 = hashAlgorithm.ComputeHash(N.ToUnsignedBigEndian());
            var hash2 = hashAlgorithm.ComputeHash(g.ToUnsignedBigEndian());
            for (var i = 0; i < hash1.Length; i++)
            {
                hash1[i] = (byte)(hash1[i] ^ hash2[i]);
            }
            return hashAlgorithm.ComputeHash(
                hash1,
                hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(username)),
                salt,
                A.ToUnsignedBigEndian(),
                B.ToUnsignedBigEndian(),
                key);

        }

        public byte[] ComputeServerProof(byte[] clientProof, byte[] key)
        {
            return hashAlgorithm.ComputeHash(A.ToUnsignedBigEndian(), clientProof, key);
        }
    }
}
