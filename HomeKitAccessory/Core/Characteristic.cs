using System;
using System.Text;

namespace HomeKitAccessory.Core
{
    public abstract class Characteristic
    {
        public ulong Id { get; set; }
        public abstract Guid Type { get; }
        public abstract Type Format { get; }
        public abstract object Value { get; set; }
        public abstract bool CanRead { get; }
        public abstract bool CanWrite { get; }
        public virtual CharacteristicUnit? Unit => null;
        public virtual double? MinValue => null;
        public virtual double? MaxValue => null;
        public virtual double? MinStep => null;
        public virtual int? MaxLen => null;
        public virtual int? MaxDataLen => null;
        public virtual double[] ValidValues => null;
        public virtual Tuple<double,double> ValidValuesRange => null;
    }
}
