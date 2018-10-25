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
            if (args.Length > 0 && args[0] == "testclient")
            {
                try
                {
                    var testClient = new TestClient("127.0.0.1", 5002);
                    testClient.PairSetup("547-07-173");
                    testClient.PairVerify();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                return;
            }

            var pairingDb = new PairingDatabase();
            pairingDb.LoadOrInitialize();
            var bonjourProvider = new Net.DnsSdBonjourProvider();
            //var bonjourProvider = new MockBonjourProvider();
            var serverInfo = new ServerInfo
            {
                Name = "MyTestDevice9",
                Model = "TestDevice",
                CategoryId = 1,
                Port = 5002
            };
            var server = new Server(pairingDb, serverInfo, bonjourProvider);
            server.ConfigNumber = 5;
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
