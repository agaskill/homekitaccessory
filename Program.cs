using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                Name = "MyTestDevice6",
                Model = "TestDevice",
                CategoryId = 1,
                Port = 5002
            };
            var server = new Server(pairingDb, serverInfo, bonjourProvider);
            server.ConfigNumber = 4;
            var runTask = server.Run();
            Console.ReadLine();
            server.Stop();

            try
            {
                runTask.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }
    }
}
