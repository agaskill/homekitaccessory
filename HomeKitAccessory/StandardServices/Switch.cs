using System;
using HomeKitAccessory.Core;
using HomeKitAccessory.StandardCharacteristics;

namespace HomeKitAccessory.StandardServices
{
    class Switch : Service
    {
        private readonly static Guid type = new Guid("00000049-0000-1000-8000-0026BB765291");
        public override Guid Type => type;

        public Switch(Action<bool> setState, Func<bool> getState, IObservable<bool> source)
        {
            AddCharacteristic(1, new On(setState, getState, source));
        }
    }
}