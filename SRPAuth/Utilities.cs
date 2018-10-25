using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace SRPAuth
{
    public static class Utilities
    {
        public static BigInteger ToUnsignedBigEndian(this byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) throw new ArgumentException("zero-length buffer not allowed", nameof(data));

            byte[] sourcedata;
            if (data[0] >= 0x80)
            {
                sourcedata = new byte[data.Length + 1];
                sourcedata[0] = 0;
                Array.Copy(data, 0, sourcedata, 1, data.Length);
            }
            else
            {
                sourcedata = new byte[data.Length];
                Array.Copy(data, sourcedata, data.Length);
            }
            Array.Reverse(sourcedata);
            return new BigInteger(sourcedata);
        }

        public static byte[] ToUnsignedBigEndian(this BigInteger bigInteger)
        {
            var data = bigInteger.ToByteArray();
            if (data.Length > 1 && data[data.Length - 1] == 0)
            {
                var newdata = new byte[data.Length - 1];
                Array.Copy(data, newdata, newdata.Length);
                data = newdata;
            }
            Array.Reverse(data);
            return data;
        }

        public static byte[] ComputeHash(this HashAlgorithm hashAlgorithm, params byte[][] blocks)
        {
            for (var i = 0; i < blocks.Length - 1; i++)
            {
                hashAlgorithm.TransformBlock(blocks[i], 0, blocks[i].Length, blocks[i], 0);
            }
            hashAlgorithm.TransformFinalBlock(blocks[blocks.Length - 1], 0, blocks[blocks.Length - 1].Length);
            return hashAlgorithm.Hash;
        }

        public static byte[] ParseByteArray(this string bytes)
        {
            var ms = new MemoryStream();
            for (var i = 0; i < bytes.Length; i += 2)
            {
                while (i < bytes.Length && char.IsWhiteSpace(bytes[i])) i++;
                var octet = bytes.Substring(i, 2);
                ms.WriteByte(byte.Parse(octet, System.Globalization.NumberStyles.HexNumber));
            }
            return ms.ToArray();
        }

    }
}
