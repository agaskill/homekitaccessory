using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace HomeKitAccessory
{
    public class PairingDatabase
    {
        public string DeviceId {get;set;}
        public Sodium.Ed25519Keypair SignKeyPair {get;set;}

        public List<PairingEntry> Pairings {get;set;}

        public static string GenerateDeviceId()
        {
            var random = RandomNumberGenerator.Create();
            var bytes = new byte[6];
            random.GetNonZeroBytes(bytes);
            return BitConverter.ToString(bytes).Replace('-', ':');
        }

        public void AddPairing(string deviceId, Sodium.Ed25519PublicKey longTermPublic)
        {
            var entry = new PairingEntry
            {
                DeviceId = deviceId,
                PublicKey = longTermPublic
            };
            Pairings.Add(entry);
            Save();
        }

        public Sodium.Ed25519PublicKey FindKey(string deviceId)
        {
            return Pairings.Find(p => p.DeviceId == deviceId)?.PublicKey;
        }

        public virtual void Save()
        {
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