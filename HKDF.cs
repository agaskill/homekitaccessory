namespace HomeKitAccessory
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    class HKDF
    {
        public static byte[] SHA512(byte[] inputKey, string salt, string info, int outputSize)
        {
            return SHA512(
                inputKey,
                Encoding.UTF8.GetBytes(salt),
                Encoding.UTF8.GetBytes(info),
                outputSize);
        }

        public static byte[] SHA512(byte[] inputKey, byte[] salt, byte[] info, int outputSize)
        {
            using (var hash = new HMACSHA512(salt))
            {
                var prk = hash.ComputeHash(inputKey);
                hash.Key = prk;
                var okm = new MemoryStream();
                var t = new byte[0];
                for (var i = 0; i <= outputSize / (hash.HashSize / 8); i++)
                {
                    var counter = new byte[] { (byte)(i + 1) };
                    hash.TransformBlock(t, 0, t.Length, t, 0);
                    hash.TransformBlock(info, 0, info.Length, info, 0);
                    hash.TransformFinalBlock(counter, 0, counter.Length);
                    t = hash.Hash;
                    okm.Write(t, 0, t.Length);
                }
                okm.Position = 0;
                var result = new byte[outputSize];
                if (okm.Read(result, 0, outputSize) != outputSize)
                    throw new InvalidOperationException("Failed to read correct length from memory stream");
                return result;
            }
        }
    }
}