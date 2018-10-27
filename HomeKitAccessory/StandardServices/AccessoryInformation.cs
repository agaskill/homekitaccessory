using HomeKitAccessory.Core;
using HomeKitAccessory.StandardCharacteristics;
using System;

namespace HomeKitAccessory.StandardServices
{
    public class AccessoryInformation : Service
    {

        public AccessoryInformation(
            string manufacturer,
            string model,
            string name,
            string serial,
            string firmware,
            Action identifyRoutine)
            : base(ServiceTypes.AccessoryInformation)
        {
            Add(2, new Identify(identifyRoutine));
            Add(1, new FixedStringCharacteristic(
                CharacteristicTypes.Manufacturer, manufacturer));
            Add(3, new FixedStringCharacteristic(
                CharacteristicTypes.Model ,model));
            Add(4, new FixedStringCharacteristic(
                CharacteristicTypes.Name ,name));
            Add(5, new FixedStringCharacteristic(
                CharacteristicTypes.SerialNumber, serial));
            Add(6, new FixedStringCharacteristic(
                CharacteristicTypes.FirmwareRevision, firmware));
        }
    }
}
