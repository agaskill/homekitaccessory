using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HomeKitAccessory
{
    class Program
    {
        
        static void Main(string[] args)
        {
            var pairingDb = PairingDatabase.LoadOrInitialize();
            var bonjourProvider = new DnsSdBonjourProvider();
            var serverInfo = new ServerInfo
            {
                Name = "MyTestDevice2",
                Model = "TestDevice1,2",
                CategoryId = 1,
                Port = 5002
            };
            var server = new Server(pairingDb, serverInfo, bonjourProvider);
            server.Run().Wait();
        }
    }
}
