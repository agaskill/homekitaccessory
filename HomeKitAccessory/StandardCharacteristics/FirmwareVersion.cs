using System;
using System.Runtime.InteropServices;
using HomeKitAccessory.Core;

namespace HomeKitAccessory.StandardCharacteristics
{
    public class FirmwareVersion : FixedStringCharacteristic
    {
        private static readonly Guid type = new Guid("00000052-0000-1000-8000-0026BB765291");
        public FirmwareVersion(string value) : base(value)
        {}

        public override Guid Type => type;
    }
}