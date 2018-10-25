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

        public abstract T TypedValue { get; set; }
    }
}
