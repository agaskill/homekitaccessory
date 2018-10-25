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
            server.RegisterAccessory(new SimulatedAccessory());
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

    class SimulatedAccessory : Accessory
    {
        List<Characteristic> characteristics;
        List<Service> services;

        public SimulatedAccessory()
        {
            characteristics = new List<Characteristic>();
            services = new List<Service>();
            var infoCharacteristics = new List<Characteristic>();
            services.Add(new Service
            {
                InstanceId = 1,
                Type = new Guid("0000003E-0000-1000-8000-0026BB765291"),
                Characteristics = infoCharacteristics
            });
            infoCharacteristics.Add(new Characteristic(
                this, 1, new Guid("00000014-0000-1000-8000-0026BB765291"),
                new BoolFormat(), null, null, Identify, null));
            infoCharacteristics.Add(new Characteristic(
                this, 2, new Guid("00000020-0000-1000-8000-0026BB765291"),
                new StringFormat(64), null,
                () => Task.FromResult((object)"Andrew Gaskill"),
                null, null));
            infoCharacteristics.Add(new Characteristic(
                this, 3, new Guid("00000021-0000-1000-8000-0026BB765291"),
                new StringFormat(64), null,
                () => Task.FromResult((object)"TestDevice1"),
                null, null));
            
            characteristics.AddRange(infoCharacteristics);
        }

        Task Identify(object value)
        {
            Console.WriteLine("Identify!");
            return Task.CompletedTask;
        }
        public override IEnumerable<Characteristic> Characteristics => characteristics;
        public override IEnumerable<Service> Services => services;
    }
}
