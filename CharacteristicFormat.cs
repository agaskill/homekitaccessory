using System;
using Newtonsoft.Json.Linq;

namespace HomeKitAccessory
{
    public abstract class CharacteristicFormat
    {
        public abstract string Format {get;}

        public abstract object Coerce(JToken value);

        public virtual void PopulateMeta(JObject obj)
        {
            obj["format"] = Format;
        }
    }

    public class BoolFormat : CharacteristicFormat
    {
        public override string Format => "bool";

        public override object Coerce(JToken value)
        {
            return (bool)value;
        }
    }

    public class UInt8Format : CharacteristicFormat
    {
        public override string Format => "uint8";

        public override object Coerce(JToken value)
        {
            return (byte)value;
        }
    }

    public class UInt16Format : CharacteristicFormat
    {
        public override string Format => "uint16";

        public override object Coerce(JToken value)
        {
            return (ushort)value;
        }
    }

    public class UInt32Format : CharacteristicFormat
    {
        public override string Format => "uint32";

        public override object Coerce(JToken value)
        {
            return (uint)value;
        }
    }

    public class UInt64Format : CharacteristicFormat
    {
        public override string Format => "uint64";

        public override object Coerce(JToken value)
        {
            return (ulong)value;
        }
    }

    public class TLV8Format : CharacteristicFormat
    {
        public override string Format => "tlv8";

        public override object Coerce(JToken value)
        {
            return Convert.FromBase64String((string)value);
        }
    }

    public class StringFormat : CharacteristicFormat
    {
        public override string Format => "string";

        public override object Coerce(JToken value)
        {
            var strval = (string)value;
            if (strval.Length > (MaxLen ?? 256))
                throw new ArgumentOutOfRangeException();
            return strval;
        }

        public override void PopulateMeta(JObject obj)
        {
            base.PopulateMeta(obj);
            if (MaxLen.HasValue)
                obj["maxLen"] = MaxLen;
        }

        public int? MaxLen {get; private set;}

        public StringFormat(int? maxLen)
        {
            MaxLen = maxLen;
        }
    }

    public class IntFormat : CharacteristicFormat
    {
        public override string Format => "int";

        public override object Coerce(JToken value)
        {
            var intval = (int)value;
            if (intval < MinValue || intval > MaxValue)
                throw new ArgumentOutOfRangeException();
            return (int)value;
        }

        public override void PopulateMeta(JObject obj)
        {
            base.PopulateMeta(obj);
            if (MinValue.HasValue)
                obj["minValue"] = MinValue.Value;
            if (MaxValue.HasValue)
                obj["maxValue"] = MaxValue.Value;
            if (MinStep.HasValue)
                obj["minStep"] = MinStep.Value;
        }

        public int? MinValue {get; private set;}
        public int? MaxValue {get; private set;}
        public int? MinStep {get; private set;}

        public IntFormat(int? minValue, int? maxValue, int? minStep)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            MinStep = minStep;
        }
    }

    public class FloatFormat : CharacteristicFormat
    {
        public override string Format => "float";

        public override object Coerce(JToken value)
        {
            var dval = (double)value;
            if (dval < MinValue || dval > MaxValue)
                throw new ArgumentOutOfRangeException();
            return dval;
        }

        public override void PopulateMeta(JObject obj)
        {
            base.PopulateMeta(obj);
            if (MinValue.HasValue)
                obj["minValue"] = MinValue.Value;
            if (MaxValue.HasValue)
                obj["maxValue"] = MaxValue.Value;
            if (MinStep.HasValue)
                obj["minStep"] = MinStep.Value;
        }

        public double? MinValue {get; private set;}
        public double? MaxValue {get; private set;}
        public double? MinStep {get; private set;}

        public FloatFormat(double? minValue, double? maxValue, double? minStep)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            MinStep = minStep;
        }
    }

    public class DataFormat : CharacteristicFormat
    {
        public override string Format => "data";

        public override object Coerce(JToken value)
        {
            return Convert.FromBase64String((string)value);
        }
    }
}