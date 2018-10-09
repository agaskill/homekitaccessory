using System;
using Newtonsoft.Json;

namespace HomeKitAccessory
{
    public class CharacteristicReadRequest
    {
        public AccessoryCharacteristicId[] Ids {get;set;}
        public bool IncludeMeta {get;set;}
        public bool IncludePerms {get;set;}
        public bool IncludeType {get;set;}
        public bool IncludeEvent {get;set;}
    }
}