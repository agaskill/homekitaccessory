using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HomeKitAccessory
{
    public class CharacteristicConverter : JsonConverter<Characteristic>
    {
        public bool IncludeMeta {get;set;}
        public bool IncludeType {get;set;}
        public bool IncludePerms {get;set;}
        public bool IncludeAccessoryId {get;set;}

        public ISet<AccessoryCharacteristicId> CurrentEvents  {get;set;}

        public CharacteristicConverter()
        {
            CurrentEvents = new HashSet<AccessoryCharacteristicId>();
        }

        public override bool CanRead => false;

        public override Characteristic ReadJson(JsonReader reader, Type objectType, Characteristic existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public static JArray FormatPerms(Characteristic value)
        {
            var perms = new JArray();
            if (value.Read != null)
                perms.Add("pr");
            if (value.Write != null)
                perms.Add("pw");
            if (value.Observable != null)
                perms.Add("ev");
            return perms;
        }

        public override void WriteJson(JsonWriter writer, Characteristic value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            if (IncludeAccessoryId)
            {
                writer.WritePropertyName("aid");
                writer.WriteValue(value.Accessory.Id);
            }
            writer.WritePropertyName("iid");
            writer.WriteValue(value.InstanceId);

            if (IncludeType)
            {
                writer.WritePropertyName("type");
                writer.WriteValue(HapTypeConverter.Format(value.Type));
            }

            if (IncludePerms)
            {
                writer.WritePropertyName("perms");
                FormatPerms(value).WriteTo(writer);
            }
            
            if (IncludeMeta)
            {
                value.Format.WriteMeta(writer);

                if (value.Unit.HasValue)
                {
                    writer.WritePropertyName("unit");
                    writer.WriteValue(FormatUnit(value.Unit.Value));
                }
            }
        }

        public static void PopulateMeta(Characteristic value, JObject obj)
        {
            value.Format.PopulateMeta(obj);
            if (value.Unit.HasValue) {
                obj["unit"] = FormatUnit(value.Unit.Value);
            }
        }

        public static string FormatUnit(CharacteristicUnit unit)
        {
            switch (unit) {
                case CharacteristicUnit.ARCDEGREES:
                    return "arcdegrees";
                case CharacteristicUnit.CELSIUS:
                    return "celsius";
                case CharacteristicUnit.LUX:
                    return "lux";
                case CharacteristicUnit.PERCENTAGE:
                    return "percentage";
                case CharacteristicUnit.SECONDS:
                    return "seconds";
            }
            throw new ArgumentException(nameof(unit));
        }
    }
}