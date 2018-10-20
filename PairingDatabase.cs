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

        public IList<PairingEntry> Pairings {get;set;}

        public static string GenerateDeviceId()
        {
            var random = RandomNumberGenerator.Create();
            var bytes = new byte[6];
            random.GetNonZeroBytes(bytes);
            return BitConverter.ToString(bytes).Replace('-', ':');
        }

        public void AddPairing(string deviceId, byte[] longTermPublic)
        {
            var entry = new PairingEntry
            {
                DeviceId = deviceId,
                PublicKey = longTermPublic
            };
            Pairings.Add(entry);
            Save();
        }

        public void Save()
        {
            using (var writer = new JsonTextWriter(
                    new StreamWriter("pairingdb.json")))
            {
                JsonSerializer.CreateDefault()
                    .Serialize(writer, this);
            }
        }

        public static PairingDatabase LoadOrInitialize()
        {
            PairingDatabase pairingDatabase;
            if (File.Exists("pairingdb.json"))
            {
                using (var reader = new JsonTextReader(
                    new StreamReader("pairingdb.json")))
                {
                    pairingDatabase = JsonSerializer.CreateDefault()
                        .Deserialize<PairingDatabase>(reader);
                }
            }
            else
            {
                pairingDatabase = new PairingDatabase();
                pairingDatabase.DeviceId = GenerateDeviceId();
                pairingDatabase.SignKeyPair = Sodium.SignKeypair();
                pairingDatabase.Pairings = new List<PairingEntry>();
                pairingDatabase.Save();
            }
            return pairingDatabase;
        }
    }

    public class PairingEntry
    {
        public string DeviceId {get;set;}
        public byte[] PublicKey {get;set;}
    }
}