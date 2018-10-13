using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        public IObservable<object> Observable {get; private set;}

        public Characteristic(
            Accessory accessory,
            int instanceId,
            Guid type,
            CharacteristicFormat format,
            CharacteristicUnit? unit,
            Func<Task<object>> read,
            Func<object, Task> write,
            IObservable<object> observable)
        {
            Accessory = accessory;
            InstanceId = instanceId;
            Type = type;
            Format = format;
            Unit = unit;
            Read = read;
            Write = write;
            Observable = observable;
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