using HomeKitAccessory.StandardServices;
using System.Collections.Generic;

namespace HomeKitAccessory.Core
{
    public class Accessory
    {
        private List<Service> services = new List<Service>();
        public ulong Id { get; set; }
        public IEnumerable<Service> Services => services;
        private Dictionary<ulong, Characteristic> lookup = new Dictionary<ulong, Characteristic>();

        public void AddService(ulong id, Service service)
        {
            id = 1 + ((id - 1) << 16);
            service.Id = id;
            foreach (var characteristic in service.Characteristics)
            {
                characteristic.Id += id;
                lookup.Add(characteristic.Id, characteristic);
            }

            services.Add(service);
        }

        public Characteristic FindCharacteristic(ulong characteristicId)
        {
            if (lookup.TryGetValue(characteristicId, out Characteristic characteristic))
                return characteristic;
            return null;
        }
    }
}
