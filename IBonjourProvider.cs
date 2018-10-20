using System;

namespace HomeKitAccessory
{
    public interface IBonjourProvider : IDisposable
    {
        void Advertise(DiscoveryInfo info);
    }
}