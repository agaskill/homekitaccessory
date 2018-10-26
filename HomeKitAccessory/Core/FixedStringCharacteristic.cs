using System;

namespace HomeKitAccessory.Core
{
    public abstract class FixedStringCharacteristic : TypedCharacteristic<string>
    {
        private readonly string value;

        protected FixedStringCharacteristic(string value)
        {
            this.value = value;
        }

        public override string TypedValue
        {
            get => value;
            set => throw new NotImplementedException();
        }

        public override bool CanWrite => false;
        public override bool CanRead => true;

        public override int? MaxLen => 64;
    }
}
