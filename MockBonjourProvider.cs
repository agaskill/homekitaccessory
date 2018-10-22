namespace HomeKitAccessory
{
    using System;

    class MockBonjourProvider : IBonjourProvider
    {
        public void Advertise(DiscoveryInfo info)
        {
            Console.WriteLine("dns-sd -R \"{info.Name}\" _hap._tcp local {info.Port} c#={info.ConfigNumber} id={info.DeviceId} md=\"{info.Name}\" s#=1 pv=1.0 ff=0 sf={(int)info.StatusFlags} ci={info.CategoryId}");
        }

        public void Dispose()
        {
        }
    }
}