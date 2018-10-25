using System;

namespace HomeKitAccessory.Net
{
    public class DiscoveryInfo
    {
        public string Name {get;set;}
        public int Port {get;set;}
        public int ConfigNumber {get;set;}
        public string DeviceId {get;set;}
        public string Model {get;set;}
        public int CategoryId {get;set;}
        public DiscoveryStateFlags StatusFlags {get;set;}
    }

    [Flags]
    public enum DiscoveryStateFlags
    {
        None = 0,
        Unpaired = 1,
        WiFiNotConfigured = 2,
        ProblemDetected = 4
    }
}