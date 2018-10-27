using System;

namespace HomeKitAccessory.Core
{
    public abstract class ObservableTypedCharacteristic<T> : TypedCharacteristic<T>, IObservable<object>
    {
        private Observable<object> observable = new Observable<object>(false);

        public override bool CanRead => true;

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