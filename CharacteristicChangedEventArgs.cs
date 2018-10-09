using System;

namespace HomeKitAccessory
{
    public class CharacteristicChangedEventArgs : EventArgs
    {
        public CharacteristicChangedEventArgs(object value)
        {
            Value = value;
        }
        public object Value {get; private set;}
    }
}