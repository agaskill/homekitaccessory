using System;
using Newtonsoft.Json;
using HomeKitAccessory.Data;

namespace HomeKitAccessory.Net
{
    public class CharacteristicReadRequest
    {
        public AccessoryCharacteristicId[] Ids {get;set;}
        public bool IncludeMeta {get;set;}
        public bool IncludePerms {get;set;}
        public bool IncludeType {get;set;}
        public bool IncludeEvent {get;set;}

        public override string ToString()
        {
            return Newtonsoft.Json.Linq.JObject.FromObject(this).ToString();
        }
    }
}