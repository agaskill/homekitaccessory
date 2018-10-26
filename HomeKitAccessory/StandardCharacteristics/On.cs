using System;
using HomeKitAccessory.Core;

namespace HomeKitAccessory.StandardCharacteristics
{
    public class On : TypedCharacteristic<bool>, IObservable<object>
    {
        private static readonly Guid type = new Guid("00000025-0000-1000-8000-0026BB765291");
        private Action<bool> setState;
        private Func<bool> getState;
        private IObservable<bool> source;

        public On(Action<bool> setState, Func<bool> getState, IObservable<bool> source)
        {
            this.setState = setState;
            this.getState = getState;
            this.source = source;
        }

        public override Guid Type => type;

        public override bool TypedValue
        {
            get => getState();
            set => setState(value);
        }

        public override bool CanRead => true;

        public override bool CanWrite => true;

        public IDisposable Subscribe(IObserver<object> observer)
        {
            return source.Subscribe(new Observer(observer));
        }

        class Observer : IObserver<bool>
        {
            private IObserver<object> observer;

            public Observer(IObserver<object> observer)
            {
                this.observer = observer;
            }

            public void OnCompleted()
            {
                observer.OnCompleted();
            }

            public void OnError(Exception error)
            {
                observer.OnError(error);
            }

            public void OnNext(bool value)
            {
                observer.OnNext(value);
            }
        }
    }
}