using System;
using System.Collections.Concurrent;

namespace HomeKitAccessory.Core
{
    public class Observable<T> : IObservable<T>
    {
        private ConcurrentDictionary<Guid, IObserver<T>> subscribers = new ConcurrentDictionary<Guid, IObserver<T>>();
        private T lastValue;
        private bool keepLastValue;

        public Observable(bool keepLastValue)
        {
            this.keepLastValue = keepLastValue;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var id = Guid.NewGuid();
            subscribers[id] = observer;
            if (keepLastValue)
            {
                observer.OnNext(lastValue);
            }
            return new DisposeAction(() => subscribers.TryRemove(id, out _));
        }

        public void Notify(T value)
        {
            if (keepLastValue)
            {
                lastValue = value;
            }
            foreach (var subscriber in subscribers.Values)
            {
                subscriber.OnNext(value);
            }
        }
    }
}