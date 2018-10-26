using System;

namespace HomeKitAccessory.Core
{
    public class DisposeAction : IDisposable
    {
        private Action fn;

        public DisposeAction(Action fn)
        {
            this.fn = fn;
        }
        public void Dispose()
        {
            fn();
        }
    }
}