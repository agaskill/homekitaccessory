namespace HomeKitAccessory.StandardCharacteristics
{
    using System;
    using HomeKitAccessory.Core;

    public class Serial : FixedStringCharacteristic
    {
        public static readonly Guid KnownType = new Guid("00000030-0000-1000-8000-0026BB765291");

        public Serial(string value) : base(value)
        {
        }

        public override Guid Type => KnownType;
    }
}