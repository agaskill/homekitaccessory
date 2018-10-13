using System;
using Newtonsoft.Json;

namespace HomeKitAccessory
{
    public class HapTypeConverter : JsonConverter<Guid>
    {
        public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var guid = reader.ReadAsString();
            if (guid.Length <= 8) {
                guid = new string('0', 8 - guid.Length) + guid.ToUpperInvariant() + "-0000-1000-8000-0026BB765291";
            }
            return new Guid(guid);
        }

        public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
        {
            writer.WriteValue(Format(value));
        }

        public static string Format(Guid value)
        {
            var guid = value.ToString().ToUpperInvariant();
            if (guid.EndsWith("-0000-1000-8000-0026BB765291"))
                guid = guid.Substring(0, 8).TrimStart('0');
            return guid;
        }
    }
}