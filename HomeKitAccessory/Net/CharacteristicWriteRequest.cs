using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HomeKitAccessory.Data;

namespace HomeKitAccessory.Net
{
    public class CharacteristicWriteRequest
    {
        [JsonProperty("characteristics")]
        public List<CharacteristicWriteItem> Characteristics {get;set;}

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }
    }

    public class CharacteristicWriteItem
    {
        [JsonProperty("aid")]
        public ulong AccessoryId {get;set;}

        [JsonProperty("iid")]
        public ulong InstanceId {get;set;}

        [JsonProperty("value")]
        public JToken Value {get;set;}

        [JsonProperty("ev")]
        public bool? Events {get;set;}

        [JsonProperty("authData")]
        public string AuthData {get;set;}

        [JsonProperty("remote")]
        public bool? Remote {get;set;}

        public static explicit operator AccessoryCharacteristicId(CharacteristicWriteItem item)
        {
            return new AccessoryCharacteristicId(item.AccessoryId, item.InstanceId);
        }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }
    }
}