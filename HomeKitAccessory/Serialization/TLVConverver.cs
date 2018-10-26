using System;
using HomeKitAccessory.Data;
using Newtonsoft.Json;

namespace HomeKitAccessory.Serialization
{
    public class TLVConverter : JsonConverter<TLVCollection>
    {
        public override TLVCollection ReadJson(JsonReader reader, Type objectType, TLVCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return TLVCollection.Deserialize(Convert.FromBase64String(reader.ReadAsString()));
        }

        public override void WriteJson(JsonWriter writer, TLVCollection value, JsonSerializer serializer)
        {
            writer.WriteValue(Convert.ToBase64String(value.Serialize()));
        }
    }
}