using HomeKitAccessory.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace HomeKitAccessory.Serialization
{
    public class CharacteristicSerializer : JsonConverter<Characteristic>
    {
        private readonly bool includeMeta;
        private readonly bool includePerms;
        private readonly bool includeType;
        private readonly ulong? accessoryId;
        private readonly ISet<ulong> subscribedIds;

        /// <summary>
        /// Create a serializer for characteristic objects
        /// </summary>
        /// <param name="accessoryId">If the "aid" property should be included, such as in a characteristic read operation, provide it.</param>
        /// <param name="subscribedIds">If the "ev" property should be included, provide an ISet of the subscribed characteristic iids within this accessory.</param>
        /// <param name="includeType">If the "type" property should be included, set this to true.</param>
        /// <param name="includePerms">If the "perms" array should be included, set this to true.</param>
        /// <param name="includeMeta">If the metadata about format, unit, minValue, maxValue, etc, should be included, set this to true.</param>
        public CharacteristicSerializer(
            ulong? accessoryId,
            ISet<ulong> subscribedIds,
            bool includeType,
            bool includePerms,
            bool includeMeta)
        {
            this.accessoryId = accessoryId;
            this.subscribedIds = subscribedIds;
            this.includeType = includeType;
            this.includePerms = includePerms;
            this.includeMeta = includeMeta;
        }

        public override Characteristic ReadJson(JsonReader reader, Type objectType, Characteristic existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, Characteristic value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("iid");
            writer.WriteValue(value.Id);

            if (value.CanRead)
            {
                writer.WritePropertyName("value");
                writer.WriteValue(value.Value);
            }

            if (accessoryId.HasValue)
            {
                writer.WritePropertyName("aid");
                writer.WriteValue(accessoryId.Value);
            }

            if (includeType)
            {
                writer.WritePropertyName("type");
                writer.WriteValue(value.Type.ToString());
            }

            if (includePerms)
            {
                writer.WritePropertyName("perms");
                writer.WriteStartArray();
                if (value.CanRead) writer.WriteValue("pr");
                if (value.CanWrite) writer.WriteValue("pw");
                if (value is IObservable<object>) writer.WriteValue("ev");
                writer.WriteEndArray();
            }

            if (subscribedIds != null)
            {
                writer.WritePropertyName("ev");
                writer.WriteValue(subscribedIds.Contains(value.Id));
            }

            if (includeMeta)
            {
                writer.WritePropertyName("format");
                writer.WriteValue(TypeFormat(value.Format));
                if (value.Unit.HasValue)
                {
                    writer.WritePropertyName("unit");
                    writer.WriteValue(UnitName(value.Unit.Value));
                }
                if (value.MinValue.HasValue)
                {
                    writer.WritePropertyName("minValue");
                    writer.WriteValue(value.MinValue.Value);
                }
                if (value.MaxValue.HasValue)
                {
                    writer.WritePropertyName("maxValue");
                    writer.WriteValue(value.MaxValue.Value);
                }
            }

            writer.WriteEndObject();
        }

        public static string TypeFormat(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(int))
                return "int";
            if (type == typeof(double))
                return "float";
            if (type == typeof(byte[]))
                return "data";
            //if (type == typeof(TLVCollection))
            //    return "tlv8";
            if (type == typeof(byte))
                return "uint8";
            if (type == typeof(ushort))
                return "uint16";
            if (type == typeof(uint))
                return "uint32";
            if (type == typeof(ulong))
                return "uint64";
            throw new ArgumentException(string.Format("{0} is not a supported type for serialization", type), nameof(type));
        }

        public static string UnitName(CharacteristicUnit unit)
        {
            return unit.ToString().ToLowerInvariant();
        }
    }
}
