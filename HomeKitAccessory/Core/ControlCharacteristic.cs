using System;

namespace HomeKitAccessory.Core
{
    public abstract class ControlCharacteristic<T> : TypedCharacteristic<T>
    {
        public override bool CanWrite => true;
    }
}
