using System;
using HomeKitAccessory.Core;

namespace HomeKitAccessory.StandardCharacteristics
{
    public class Model : FixedStringCharacteristic
    {
        public readonly static Guid KnownType = new Guid("00000021-0000-1000-8000-0026BB765291");

        public override Guid Type => KnownType;

        public Model(string value) : base(value)
        { }
    }
}