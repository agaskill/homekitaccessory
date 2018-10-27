using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HomeKitAccessory.Core;
using HomeKitAccessory.Pairing;
using HomeKitAccessory.StandardCharacteristics;
using HomeKitAccessory.StandardServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace HomeKitAccessory
{
    class Program
    {
        static void Main(string[] args)
        {
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            var logConsole = new NLog.Targets.ConsoleTarget("logconsole");
            loggingConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            LogManager.Configuration = loggingConfig;

            LogManager.GetLogger("test").Info("Started");

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
            var userStore = new Pairing.DynamicSetupCodeUserStore(code => Console.WriteLine("*** {0} ***", code));
            var server = new Server(pairingDb, serverInfo, bonjourProvider, userStore);
            server.ConfigNumber = 8;

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
            accessory.AddService(2, new Service(ServiceTypes.Switch, primary: true)
            {
                { 1, switchState }
            });
            
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

    class MySwitchState : ObservableTypedCharacteristic<bool>
    {
        private bool currentState;

        public override Guid Type => CharacteristicTypes.On;

        public override bool CanWrite => true;

        public override bool TypedValue {
            get => currentState;
            set {
                Console.WriteLine("Changing state to " + value);
                currentState = value;
                Notify(value);
            }
        }
    }
}
