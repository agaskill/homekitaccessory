using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace SRPAuth
{
    public class UserVerifier
    {
        public byte[] Salt { get; set; }
        public byte[] Verifier { get; set; }
        public byte[] Prime { get; set; }
        public byte[] Generator { get; set; }

        public static UserVerifier Create(string username, string password, SRPGroupParameters groupParameters, HashAlgorithm hashAlgorithm)
        {
            var salt = new byte[hashAlgorithm.HashSize / 8];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetNonZeroBytes(salt);
            }
            return Create(username, password, groupParameters, salt, hashAlgorithm);
        }

        public static BigInteger CalculateX(HashAlgorithm hashAlgorithm, string name, string password, byte[] salt)
        {
            var idhash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(name + ":" + password));

            var xhash = hashAlgorithm.ComputeHash(salt, idhash);

            return xhash.ToUnsignedBigEndian();
        }

        public static UserVerifier Create(string username, string password, SRPGroupParameters groupParameters, byte[] salt, HashAlgorithm hashAlgorithm)
        {
            var g = groupParameters.Generator.ToUnsignedBigEndian();
            var N = groupParameters.Prime.ToUnsignedBigEndian();
            var x = CalculateX(hashAlgorithm, username, password, salt);
            var v = BigInteger.ModPow(g, x, N);
            return new UserVerifier
            {
                Prime = groupParameters.Prime,
                Generator = groupParameters.Generator,
                Salt = salt,
                Verifier = v.ToUnsignedBigEndian()
            };
        }
    }
}
