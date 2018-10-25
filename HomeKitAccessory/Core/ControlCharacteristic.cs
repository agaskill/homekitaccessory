using System;

namespace HomeKitAccessory.Core
{
    public abstract class ControlCharacteristic<T> : TypedCharacteristic<T>
    {
        public sealed override bool CanRead => false;
        public sealed override bool CanWrite => true;
        public override T TypedValue
        {
            get => throw new NotImplementedException();
        }
    }
}
