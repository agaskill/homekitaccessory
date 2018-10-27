using System;

namespace HomeKitAccessory.Core
{
    public abstract class TypedCharacteristic<T> : Characteristic
    {
        public sealed override object Value
        {
            get => TypedValue;
            set { TypedValue = (T)value; }
        }

        public sealed override Type Format => typeof(T);

        public virtual T TypedValue
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
