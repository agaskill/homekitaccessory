using System;
using HomeKitAccessory.Core;
using HomeKitAccessory.StandardCharacteristics;

namespace HomeKitAccessory.StandardServices
{
    public class Switch : Service
    {
        private readonly static Guid type = new Guid("00000049-0000-1000-8000-0026BB765291");
        public override Guid Type => type;

        public Switch(string name, On on)
        {
            AddCharacteristic(1, on);
            if (name != null)
                AddCharacteristic(2, new Name(name));
        }

        public Switch(On on) : this(null, on) {}
    }
}