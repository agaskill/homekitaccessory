using System;

namespace HomeKitAccessory.Net
{
    public interface IBonjourProvider : IDisposable
    {
        void Advertise(DiscoveryInfo info);
    }
}