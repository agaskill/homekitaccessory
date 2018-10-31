using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NLog;

namespace HomeKitAccessory.Pairing
{
    public class PairingDatabase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public string DeviceId {get;set;}
        public Sodium.Ed25519Keypair SignKeyPair {get;set;}

        public List<PairingEntry> Pairings {get;set;}

        public static string GenerateDeviceId()
        {
            var random = RandomNumberGenerator.Create();
            var bytes = new byte[6];
            random.GetNonZeroBytes(bytes);
            var deviceId = BitConverter.ToString(bytes).Replace('-', ':');
            logger.Info("Generated device id {0}", deviceId);
            return deviceId;
        }

        public void AddPairing(string deviceId, Sodium.Ed25519PublicKey longTermPublic)
        {
            logger.Info("Adding pairing for {0}", deviceId);

            var entry = new PairingEntry
            {
                DeviceId = deviceId,
                PublicKey = longTermPublic
            };
            Pairings.Add(entry);
            Save();
        }

        public void RemovePairing(string deviceId)
        {
            logger.Info("Removing pairing for {0}", deviceId);

            if (Pairings.RemoveAll(p => p.DeviceId == deviceId) > 0)
            {
                Save();
            }
        }

        public Sodium.Ed25519PublicKey FindKey(string deviceId)
        {
            return Pairings.Find(p => p.DeviceId == deviceId)?.PublicKey;
        }

        public virtual void Save()
        {
            logger.Debug("Saving pairing state");
            using (var writer = new JsonTextWriter(
                    new StreamWriter("pairingdb.json")))
            {
                JsonSerializer.CreateDefault()
                    .Serialize(writer, this);
            }
        }

        protected virtual bool Load()
        {
            if (File.Exists("pairingdb.json"))
            {
                logger.Debug("Reading pairing database from file");
                using (var reader = new JsonTextReader(
                    new StreamReader("pairingdb.json")))
                {
                    var pairingDatabase = JsonSerializer.CreateDefault()
                        .Deserialize<PairingDatabase>(reader);
                    SignKeyPair = pairingDatabase.SignKeyPair;
                    DeviceId = pairingDatabase.DeviceId;
                    Pairings = pairingDatabase.Pairings;
                }
                return true;
            }
            return false;
        }

        public void LoadOrInitialize()
        {
            if (!Load())
            {
                logger.Debug("No pairing database, creating new");
                DeviceId = GenerateDeviceId();
                SignKeyPair = Sodium.SignKeypair();
                Pairings = new List<PairingEntry>();
                Save();
            }
        }
    }

    public class PairingEntry
    {
        public string DeviceId {get;set;}
        public Sodium.Ed25519PublicKey PublicKey {get;set;}
    }
}