using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeKitAccessory.Net;
using HomeKitAccessory.Core;
using HomeKitAccessory.Pairing;
using NLog;

namespace HomeKitAccessory
{
    public class Server
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IBonjourProvider bonjourProvider;
        private readonly List<Accessory> accessories;
        private readonly SRPAuth.IUserStore userStore;
        private readonly PairingDatabase pairingDatabase;
        private readonly ServerInfo serverInfo;

        private HttpServer server;

        public PairingDatabase PairingDatabase => pairingDatabase;
        public SRPAuth.IUserStore UserStore => userStore;
        public int ConfigNumber {get;set;}
        public IEnumerable<Accessory> Accessories => accessories.AsReadOnly();
        public bool IsPaired => PairingDatabase.Pairings.Count > 0;
        public string PairingId => PairingDatabase.DeviceId;


        public void Identify()
        {
            logger.Debug("Server-level identify called");
            Guid identifyType = StandardCharacteristics.CharacteristicTypes.Identify;
            foreach (var accessory in accessories)
            {
                accessory.Services.First().Characteristics.First(c => c.Type == identifyType).Value = true;
            }
        }

        public Server(
            PairingDatabase pairingDatabase,
            ServerInfo serverInfo,
            IBonjourProvider bonjourProvider,
            SRPAuth.IUserStore userStore)
        {
            this.bonjourProvider = bonjourProvider;
            this.pairingDatabase = pairingDatabase;
            this.userStore = userStore;
            this.serverInfo = serverInfo;

            accessories = new List<Accessory>();
        }


        public void AddPairing(string deviceId, Sodium.Ed25519PublicKey publicKey)
        {
            PairingDatabase.AddPairing(deviceId, publicKey);
            bonjourProvider.Advertise(DiscoveryInfo);
        }

        public void RemovePairing(string deviceId)
        {
            PairingDatabase.RemovePairing(deviceId);
            bonjourProvider.Advertise(DiscoveryInfo);
        }

        public DiscoveryInfo DiscoveryInfo
        {
            get
            {
                var di = new DiscoveryInfo() {
                    Name = serverInfo.Name,
                    Model = serverInfo.Model,
                    ConfigNumber = ConfigNumber,
                    Port = serverInfo.Port,
                    DeviceId = pairingDatabase.DeviceId,
                    CategoryId = serverInfo.CategoryId
                };
                if (PairingDatabase.Pairings.Count == 0)
                    di.StatusFlags |= DiscoveryStateFlags.Unpaired;
                return di;
            }
        }

        public void RegisterAccessory(Accessory accessory)
        {
            logger.Debug("Adding accessory {0}", accessory.Id);
            accessories.Add(accessory);
        }

        public Task Run()
        {
            logger.Debug("Server.Run - listening on port {0}", serverInfo.Port);
            bonjourProvider.Advertise(DiscoveryInfo);
            server = new HttpServer(this);
            return server.Listen(serverInfo.Port);
        }

        public void Stop()
        {
            logger.Info("Server.Stop - shutting down");
            bonjourProvider.Dispose();
            server.Stop();
        }
    }
}