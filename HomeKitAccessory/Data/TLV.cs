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

        public int IntegerValue
        {
            get
            {
                switch (value.Length)
                {
                    case 1:
                        return (int)value[0];
                    case 2:
                        return value[0] | (value[1] << 8);
                    case 3:
                        return value[0] | (value[1] << 8) | value[2] << 16;
                    case 4:
                        return value[0] | (value[1] << 8) | value[2] << 16 | value[3] << 32;
                    default:
                        throw new InvalidOperationException();
                }
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

        public TLV(byte tag, int integerValue)
        {
            this.tag = tag;
            if (integerValue < 0)
                throw new ArgumentException(nameof(integerValue));
            if (integerValue < (1 << 8))
            {
                value = new byte[1];
                value[0] = (byte)integerValue;
            }
            else if (integerValue < (1 << 16))
            {
                value = new byte[2];
                value[0] = (byte)(integerValue & 0xff);
                value[1] = (byte)(integerValue >> 8);
            }
            else if (integerValue < (1 << 24))
            {
                value = new byte[3];
                value[0] = (byte)(integerValue & 0xff);
                value[1] = (byte)((integerValue >> 8) & 0xff);
                value[2] = (byte)(integerValue >> 16);
            }
            else
            {
                throw new ArgumentException(nameof(integerValue));
            }
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