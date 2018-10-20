using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeKitAccessory
{
    using AppFunc = Func<IDictionary<string,object>, Task>;

    public class Server
    {
        public PairingDatabase PairingDatabase {get; private set;}
        public ServerInfo ServerInfo {get; private set;}
        public int ConfigNumber {get;set;}
        public IEnumerable<Accessory> Accessories => accessories.AsReadOnly();

        private IBonjourProvider bonjourProvider;
        private List<Accessory> accessories;
        
        private HttpServer server;

        public Server(PairingDatabase pairingDatabase, ServerInfo serverInfo, IBonjourProvider bonjourProvider)
        {
            PairingDatabase = pairingDatabase;
            ServerInfo = serverInfo;
            this.bonjourProvider = bonjourProvider;
            accessories = new List<Accessory>();
        }

        public string PairingId => PairingDatabase.DeviceId;

        public void AddPairing(string deviceId, byte[] publicKey)
        {
            PairingDatabase.AddPairing(deviceId, publicKey);
            bonjourProvider.Advertise(DiscoveryInfo);
        }

        public DiscoveryInfo DiscoveryInfo
        {
            get
            {
                var di = new DiscoveryInfo() {
                    Name = ServerInfo.Name,
                    Model = ServerInfo.Model,
                    ConfigNumber = ConfigNumber,
                    Port = ServerInfo.Port,
                    DeviceId = PairingDatabase.DeviceId,
                    CategoryId = ServerInfo.CategoryId
                };
                if (PairingDatabase.Pairings.Count == 0)
                    di.StatusFlags |= DiscoveryStateFlags.Unpaired;
                return di;
            }
        }

        private string setupCode;

        public string SetupCode
        {
            get
            {
                if (setupCode == null)
                {
                    var code = "";
                    var random = new Random();
                    for (var i = 0; i < 8; i++)
                    {
                        code += random.Next(10);
                        if (i == 2 || i == 4)
                        {
                            code += "-";
                        }
                    }
                    setupCode = code;
                }
                Console.WriteLine(setupCode);
                return setupCode;
            }
        }

        public void RegisterAccessory(Accessory accessory)
        {
            accessories.Add(accessory);
        }

        public Task Run()
        {
            bonjourProvider.Advertise(DiscoveryInfo);
            server = new HttpServer(callback => new HapConnection(this, callback));

            return server.Listen(ServerInfo.Port);
        }

        public void Stop()
        {
            bonjourProvider.Dispose();
            server.Stop();
        }
    }
}