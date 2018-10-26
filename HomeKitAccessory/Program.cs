﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HomeKitAccessory.Core;
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
            //var bonjourProvider = new Net.MockBonjourProvider();
            var serverInfo = new ServerInfo
            {
                Name = "MyTestDevice9",
                Model = "TestDevice",
                CategoryId = 1,
                Port = 5002
            };
            var server = new Server(pairingDb, serverInfo, bonjourProvider);
            server.ConfigNumber = 5;

            var accessory = new Core.Accessory();
            accessory.Id = 1;
            accessory.AddService(1, new StandardServices.AccessoryInformation(
                "Andrew",
                "TestDevice",
                "MyTestDevice9",
                "B53238EB-1BFC-477E-ADDE-6E05A97401DE",
                "1.0.0",
                () => Console.WriteLine("Identify!")));
            var switchState = new MySwitchState();
            accessory.AddService(2, new StandardServices.Switch(switchState));
            
            server.RegisterAccessory(accessory);
            
            var runTask = server.Run();

            for(;;)
            {
                var line = Console.ReadLine();
                if (line == "") break;
                if (line == "x") {
                    switchState.TypedValue = !switchState.TypedValue;
                }
            }
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

    class MySwitchState : StandardCharacteristics.On, IObservable<object>
    {
        private Observable<object> observable;
        private bool currentState;

        public override bool TypedValue {
            get => currentState;
            set {
                Console.WriteLine("Changing state to " + value);
                currentState = value;
                observable.Notify(value);
            }
        }

        public IDisposable Subscribe(IObserver<object> observer)
        {
            return observable.Subscribe(observer);
        }
    }
}
