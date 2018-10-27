using System;

namespace HomeKitAccessory.Core
{
    public abstract class ObservableTypedCharacteristic<T> : TypedCharacteristic<T>
    {
        private Observable<object> observable = new Observable<object>(false);

        public override bool CanRead => true;
        public override IObservable<object> Observable => observable;

        public IDisposable Subscribe(IObserver<object> observer)
        {
            return observable.Subscribe(observer);
        }

        protected void Notify(T value)
        {
            observable.Notify(value);
        }
    }
}