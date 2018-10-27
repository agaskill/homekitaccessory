using System;

namespace HomeKitAccessory.Core
{
    public class FixedStringCharacteristic : TypedCharacteristic<string>
    {
        private readonly Guid type;
        private readonly string value;

        public FixedStringCharacteristic(Guid type, string value)
        {
            this.type = type;
            this.value = value;
        }

        public override Guid Type => type;

        public override string TypedValue
        {
            get => value;
        }

        public override bool CanRead => true;

        public override int? MaxLen => 64;
    }
}
