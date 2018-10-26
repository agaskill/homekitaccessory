using System;

namespace HomeKitAccessory.Core
{
    public class Observer<T> : IObserver<T>
    {
        private Action onCompleted;
        private Action<Exception> onError;
        private Action<T> onNext;

        public Observer(Action<T> onNext)
        {
            this.onNext = onNext;
        }

        public Observer(Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            this.onNext = onNext;
            this.onError = onError;
            this.onCompleted = onCompleted;
        }

        public void OnCompleted()
        {
            onCompleted?.Invoke();
        }

        public void OnError(Exception error)
        {
            onError?.Invoke(error);
        }

        public void OnNext(T value)
        {
            onNext(value);
        }
    }
}