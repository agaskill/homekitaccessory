using System;
using System.Text;

namespace HomeKitAccessory.Core
{
    public abstract class Characteristic
    {
        public ulong Id { get; set; }
        public abstract Guid Type { get; }
        public abstract Type Format { get; }
        public virtual object Value
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public virtual bool CanRead => false;
        public virtual bool CanWrite => false;
        public virtual IObservable<object> Observable => null;
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
