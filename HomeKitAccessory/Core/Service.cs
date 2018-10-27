using System;
using System.Collections;
using System.Collections.Generic;

namespace HomeKitAccessory.Core
{
    public class Service : IEnumerable<Characteristic>
    {
        public ulong Id { get; set; }
        public Guid Type { get; private set;}
        private List<Characteristic> characteristics = new List<Characteristic>();
        public IEnumerable<Characteristic> Characteristics => characteristics;
        public bool Primary { get; private set; }
        public bool Hidden { get; set; }

        public Service(Guid type, bool primary = false, bool hidden = false)
        {
            Type = type;
            Primary = primary;
            Hidden = hidden;
        }

        public void Add(ulong id, Characteristic characteristic)
        {
            characteristic.Id = id;
            characteristics.Add(characteristic);
        }

        public IEnumerator<Characteristic> GetEnumerator()
        {
            return characteristics.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)characteristics).GetEnumerator();
        }
    }
}
