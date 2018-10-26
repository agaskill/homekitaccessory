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
            string model,
            string name,
            string serial,
            string firmware,
            Action identifyRoutine)
        {
            AddCharacteristic(1, new Manufacturer(manufacturer));
            AddCharacteristic(2, new Identify(identifyRoutine));
            AddCharacteristic(3, new Model(model));
            AddCharacteristic(4, new Name(name));
            AddCharacteristic(5, new Serial(serial));
            AddCharacteristic(6, new FirmwareVersion(firmware));
        }
    }
}
