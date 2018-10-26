using System;
using HomeKitAccessory.Core;

namespace HomeKitAccessory.StandardCharacteristics
{
    public class Name : FixedStringCharacteristic
    {
        public static readonly Guid KnownType = new Guid("00000023-0000-1000-8000-0026BB765291");

        public Name(string value) : base(value)
        {
        }

        public override Guid Type => KnownType;
    }
}