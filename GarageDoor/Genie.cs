using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeKitAccessory;

namespace HomeKitAccessory.GarageDoor
{
    public class Genie : Accessory
    {
        private List<Characteristic> characteristics;

        public override IEnumerable<Characteristic> Characteristics => characteristics;

        public Genie() {
            characteristics.Add(new Characteristic(
                this,
                1,
                Guid.NewGuid(),
                new UInt8Format(),
                null,
                GetDoorState,
                SetDoorState,
                null));
        }

        public Task<object> GetDoorState()
        {
             throw new NotImplementedException();
        }

        public Task SetDoorState(object value)
        {
            throw new NotImplementedException();
        }
    }
}