using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace HomeKitAccessory.Data
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

        public void Write(Stream stream)
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
    }
}