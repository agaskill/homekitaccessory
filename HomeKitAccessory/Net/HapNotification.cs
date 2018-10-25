using System.Collections.Generic;
using Newtonsoft.Json;

namespace HomeKitAccessory.Net
{
    class HapNotification
    {
        [JsonProperty("characteristics")]
        public List<HapNotificationCharacteristic> Characteristics {get;set;}
    }

    class HapNotificationCharacteristic
    {
        [JsonProperty("aid")]
        public ulong AccessoryId {get;set;}

        [JsonProperty("iid")]
        public ulong InstanceId {get;set;}

        [JsonProperty("value")]
        public object Value {get;set;}
    }
}