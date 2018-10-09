using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace HomeKitAccessory
{
    public class Characteristic
    {
        public Accessory Accessory {get; private set;}
        public int InstanceId {get; private set;}
        public Guid Type {get; private set;}
        public CharacteristicFormat Format {get; private set;}
        public CharacteristicUnit? Unit {get; private set;}

        public Func<Task<object>> Read {get; private set;}
        public Func<object, Task> Write {get; private set;}
        public Func<Action<object>, IDisposable> Subscribe {get; private set;}

        public Characteristic(
            Accessory accessory,
            int instanceId,
            Guid type,
            Func<Task<object>> read,
            Func<object, Task> write,
            Func<Action<object>, IDisposable> subscribe)
        {
            Accessory = accessory;
            InstanceId = instanceId;
            Type = type;
            Read = read;
            Write = write;
            Subscribe = subscribe;
        }
    }

    public enum CharacteristicUnit
    {
            CELSIUS,
            PERCENTAGE,
            ARCDEGREES,
            LUX,
            SECONDS
    }
}