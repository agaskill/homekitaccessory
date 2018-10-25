using HomeKitAccessory.StandardServices;
using System.Collections.Generic;

namespace HomeKitAccessory.Core
{
    public abstract class Accessory
    {
        private List<Service> services = new List<Service>();
        private ulong idPool;

        public ulong Id { get; set; }
        public IEnumerable<Service> Services => services;

        protected void AddService(Service service)
        {
            // A null service can be added, in order to replace one that was removed
            if (service != null)
            {
                // Assign iids to the service and the characteristics
                service.Id = ++idPool;
                foreach (var characteristic in service.Characteristics)
                {
                    // The characteristics list can have null entries, if one is removed
                    if (characteristic == null)
                    {
                        ++idPool;
                    }
                    else
                    {
                        characteristic.Id = ++idPool;
                    }
                }
            }

            services.Add(service);

            // Advance the id pool to the next block, reserving room for new characteristics to be
            // added to an existing service without affecting the iids of any other services
            idPool = (ulong)(services.Count * 1000);
        }

        public Characteristic FindCharacteristic(ulong characteristicId)
        {
            foreach (var service in services)
            {
                foreach (var characteristic in service.Characteristics)
                {
                    if (characteristic.Id == characteristicId)
                    {
                        return characteristic;
                    }
                }
            }
            return null;
        }
    }
}
