using System;
using HomeKitAccessory.Core;

namespace HomeKitAccessory.StandardCharacteristics
{
    public abstract class On : TypedCharacteristic<bool>
    {
        private static readonly Guid type = new Guid("00000025-0000-1000-8000-0026BB765291");
        public override Guid Type => type;

        public override bool CanRead => true;

        public override bool CanWrite => true;
    }
}