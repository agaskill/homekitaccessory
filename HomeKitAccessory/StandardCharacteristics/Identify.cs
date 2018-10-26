using HomeKitAccessory.Core;
using System;

namespace HomeKitAccessory.StandardCharacteristics
{
    public class Identify : ControlCharacteristic<bool>
    {
        private readonly static Guid type = new Guid("00000014-0000-1000-8000-0026BB765291");
        public static Guid KnownType => type;
        private readonly Action identifyRoutine;

        public Identify(Action identifyRoutine)
        {
            this.identifyRoutine = identifyRoutine;
        }
        public override Guid Type => type;
        public override bool TypedValue { set => identifyRoutine(); }
    }
}
