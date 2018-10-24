using System.Collections.Generic;
using Newtonsoft.Json;

namespace HomeKitAccessory
{
    public abstract class Accessory
    {
        [JsonProperty("aid")]
        public int Id {get;set;}

        [JsonIgnore]
        public abstract IEnumerable<Characteristic> Characteristics {get;}

        [JsonProperty("services")]
        public abstract IEnumerable<Service> Services {get;}
    }
}