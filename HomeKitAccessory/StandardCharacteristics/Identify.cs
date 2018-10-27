using HomeKitAccessory.Core;
using System;

namespace HomeKitAccessory.StandardCharacteristics
{
    public class Identify : ControlCharacteristic<bool>
    {
        private readonly Action identifyRoutine;

        public Identify(Action identifyRoutine)
        {
            this.identifyRoutine = identifyRoutine;
        }
        public override Guid Type => CharacteristicTypes.Identify;
        public override bool TypedValue { set => identifyRoutine(); }
    }
}
