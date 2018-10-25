using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HomeKitAccessory
{
    public class Service
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(HapTypeConverter))]
        public Guid Type {get;set;}

        [JsonProperty("iid")]
        public int InstanceId {get;set;}

        [JsonProperty("characteristics")]
        public IEnumerable<Characteristic> Characteristics {get;set;}

        [JsonProperty("hidden")]
        public bool Hidden {get;set;}

        [JsonProperty("primary")]
        public bool Primary {get;set;}

        [JsonProperty("linked")]
        public List<int> Linked {get;set;}
    }
}