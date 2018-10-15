using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace HomeKitAccessory
{
    public class TLV
    {
        private byte[] value;
        private byte tag;

        public byte Tag => tag;

        public byte[] DataValue => value;

        public string StringValue => Encoding.UTF8.GetString(value);

        public BigInteger IntegerValue
        {
            get
            {
                return new BigInteger(value, isUnsigned: true);
            }
        }

        public TLV(byte tag, byte[] dataValue)
        {
            this.tag = tag;
            this.value = dataValue;
        }

        public TLV(byte tag, string stringValue)
        {
            this.tag = tag;
            this.value = Encoding.UTF8.GetBytes(stringValue);
        }

        public TLV(byte tag, BigInteger integerValue)
        {
            this.tag = tag;
            this.value = integerValue.ToByteArray(isUnsigned: true);
        }

        void Write(Stream stream)
        {
            int remaining = value.Length;
            do {
                stream.WriteByte(tag);
                if (remaining > 255)
                {
                    stream.WriteByte(255);
                    stream.Write(value, value.Length - remaining, 255);
                    remaining -= 255;
                }
                else
                {
                    stream.WriteByte((byte)remaining);
                    stream.Write(value, value.Length - remaining, remaining);
                    remaining = 0;
                }
            } while (remaining > 0);
        }

        public static List<TLV> Deserialize(byte[] data)
        {
            return Deserialize(new MemoryStream(data));
        }

        public static List<TLV> Deserialize(Stream stream)
        {
            var tlvs = new List<TLV>();
            int lastTag = -1;
            byte[] lastValue = null;
            int tag = stream.ReadByte();
            while (tag >= 0)
            {
                int length = stream.ReadByte();
                if (tag == lastTag)
                {
                    var newValue = new byte[lastValue.Length + length];
                    Array.Copy(lastValue, newValue, lastValue.Length);
                    stream.Read(newValue, lastValue.Length, length);
                    lastValue = newValue;
                    tlvs[tlvs.Count - 1] = new TLV((byte)tag, newValue);
                }
                else
                {
                    var value = new byte[length];
                    stream.Read(value, 0, length);
                    lastTag = tag;
                    lastValue = value;
                    tlvs.Add(new TLV((byte)tag, value));
                }
                tag = stream.ReadByte();
            }
            return tlvs;
        }

        public static void Serialize(Stream stream, IEnumerable<TLV> tLVs)
        {
            foreach (var tlv in tLVs)
            {
                tlv.Write(stream);
            }
        }

        public static void Serialize(Stream stream, params TLV[] tLVs)
        {
            Serialize(stream, (IEnumerable<TLV>)tLVs);
        }

        public static byte[] Serialize(IEnumerable<TLV> tLVs)
        {
            var ms = new MemoryStream();
            Serialize(ms, tLVs);
            return ms.ToArray();
        }

        public static byte[] Serialize(params TLV[] tLVs)
        {
            return Serialize((IEnumerable<TLV>)tLVs);
        }
    }
}