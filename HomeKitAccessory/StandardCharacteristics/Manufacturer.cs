using HomeKitAccessory.Core;
using System;

namespace HomeKitAccessory.StandardCharacteristics
{
    public class Manufacturer : FixedStringCharacteristic
    {
        private readonly static Guid type = new Guid("00000020-0000-1000-8000-0026BB765291");
        public Manufacturer(string value) : base(value) { }
        public override Guid Type => type;
    }
}
