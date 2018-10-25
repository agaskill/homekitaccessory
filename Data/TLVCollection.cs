using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HomeKitAccessory.Data
{
    public class TLVCollection : IEnumerable<TLV>
    {
        private List<TLV> items;

        public TLVCollection()
        {
            items = new List<TLV>();
        }

        public static TLVCollection Deserialize(byte[] data)
        {
            return Deserialize(new MemoryStream(data));
        }

        public static TLVCollection Deserialize(Stream stream)
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
            return new TLVCollection { items = tlvs };
        }

        public void Serialize(Stream stream)
        {
            foreach (var tlv in items)
            {
                tlv.Write(stream);
            }
        }

        public byte[] Serialize()
        {
            var ms = new MemoryStream();
            Serialize(ms);
            return ms.ToArray();
        }

        public void Add(TLV item)
        {
            items.Add(item);
        }

        public TLV Find(byte tag)
        {
            return items.Find(x => x.Tag == tag);
        }

        public IEnumerator<TLV> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public int State
        {
            get => (int)Find(TLVType.State).IntegerValue;
            set => Add(new TLV(TLVType.State, value));
        }

        public int Error
        {
            get => (int)Find(TLVType.Error).IntegerValue;
            set => Add(new TLV(TLVType.Error, value));
        }
    }
}