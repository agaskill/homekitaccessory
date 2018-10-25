using HomeKitAccessory.Core;
using HomeKitAccessory.StandardCharacteristics;
using System;

namespace HomeKitAccessory.StandardServices
{
    public class AccessoryInformation : Service
    {
        private readonly static Guid type = new Guid("0000003E-0000-1000-8000-0026BB765291");
        public override Guid Type => type;

        public AccessoryInformation(
            string manufacturer,
            Action identifyRoutine)
        {
            AddCharacteristic(new Manufacturer(manufacturer));
            AddCharacteristic(new Identify(identifyRoutine));
        }
    }
}
