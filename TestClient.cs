namespace HomeKitAccessory
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Security.Cryptography;
    using System.Text;
    using HomeKitAccessory.Data;

    public class TestClient
    {
        Sodium.Ed25519Keypair deviceSign;
        string deviceId;

        public TestClient(string host, int port)
        {
            deviceSign = Sodium.SignKeypair();
            deviceId = Guid.NewGuid().ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri("http://" + host + ":" + port);
        }

        private HttpClient client;

        private MediaTypeHeaderValue mediaTypePairingTlv = new MediaTypeHeaderValue("application/pairing+tlv8");

        private TLVCollection SendTLVMessage(string path, TLVCollection msg)
        {
            var req = msg.Serialize();
            var content = new ByteArrayContent(req);
            content.Headers.ContentType = mediaTypePairingTlv;

            var response = client.PostAsync(path, content).Result;

            if (response.Content.Headers.ContentType.MediaType == mediaTypePairingTlv.MediaType)
            {
                var tlvs = TLVCollection.Deserialize(response.Content.ReadAsByteArrayAsync().Result);
                Console.WriteLine("----------------------");
                foreach (var tlv in tlvs) {
                    Console.WriteLine("{0}: {1}", tlv.Tag, BitConverter.ToString(tlv.DataValue));
                }                
                Console.WriteLine("----------------------");
                return tlvs;
            }
            throw new InvalidDataException(response.Content.Headers.ContentType.ToString());
        }

        private void Assert(string message, bool condition)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        public void PairSetup(string setupCode)
        {

            var res = SendTLVMessage("/pair-setup", new TLVCollection() {
                new TLV(TLVType.State, 1),
                new TLV(TLVType.Method, 0)
            });
            Assert("Expect state 2", res.Find(TLVType.State).IntegerValue == 2);
            Assert("Expect no error in state 2", res.Find(TLVType.Error) == null);

            var accessorySRPPublic = res.Find(TLVType.PublicKey).DataValue;
            var accessorySRPSalt = res.Find(TLVType.Salt).DataValue;

            var srpClient = new SRPAuth.SRPClient(SHA512.Create());

            var groupParameters = SRPAuth.SRPGroupParameters.Rfc5054_3072;
            var srpSession = srpClient.ServerExchange("Pair-Setup", setupCode, new SRPAuth.ServerSRPParams
            {
                Prime = groupParameters.Prime,
                Generator = groupParameters.Generator,
                Salt = accessorySRPSalt,
                ServerPublic = accessorySRPPublic
            });

            var srpKey = srpClient.ComputeStandardKey(srpSession.PremasterSecret);

            res = SendTLVMessage("/pair-setup", new TLVCollection() {
                new TLV(TLVType.State, 3),
                new TLV(TLVType.PublicKey, srpSession.ClientPublic),
                new TLV(TLVType.Proof, srpClient.ComputeClientProof("Pair-Setup", accessorySRPSalt, srpKey))
            });

            Assert("Expect state 4", res.Find(TLVType.State).IntegerValue == 4);
            Assert("Expect no error in state 4", res.Find(TLVType.Error) == null);

            var iosDeviceX = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Controller-Sign-Salt",
                "Pair-Setup-Controller-Sign-Info",
                32);
            
            var iosDeviceInfo = new MemoryStream();
            iosDeviceInfo.Write(iosDeviceX);
            iosDeviceInfo.Write(Encoding.ASCII.GetBytes(deviceId));
            iosDeviceInfo.Write(deviceSign.PublicKey.Data);
            var iosDeviceSignature = Sodium.SignDetached(iosDeviceInfo.ToArray(), deviceSign.SecretKey);

            var iosInfoTlv = new TLVCollection() {
                new TLV(TLVType.Identifier, deviceId),
                new TLV(TLVType.PublicKey, deviceSign.PublicKey.Data),
                new TLV(TLVType.Signature, iosDeviceSignature)
            }.Serialize();

            var sessionKey = new Sodium.Key(HKDF.SHA512(
                srpKey,
                "Pair-Setup-Encrypt-Salt",
                "Pair-Setup-Encrypt-Info",
                32));
            
            var iosEncData = Sodium.Encrypt(
                iosInfoTlv, null,
                "PS-Msg05", sessionKey);
            
            res = SendTLVMessage("/pair-setup", new TLVCollection() {
                new TLV(TLVType.State, 5),
                new TLV(TLVType.EncryptedData, iosEncData)
            });

            Assert("Expect state 6", res.Find(TLVType.State).IntegerValue == 6);
            Assert("Expect no error in state 6", res.Find(TLVType.Error) == null);

            var accessoryEncData = res.Find(TLVType.EncryptedData).DataValue;
            var accessoryPlainData = Sodium.Decrypt(
                accessoryEncData, null,
                "PS-Msg06", sessionKey);
            var accessoryData = TLVCollection.Deserialize(accessoryPlainData);
            accessoryPairingId = accessoryData.Find(TLVType.Identifier).StringValue;
            accessorySignPublic = new Sodium.Ed25519PublicKey(accessoryData.Find(TLVType.PublicKey).DataValue);
            var accessorySignature = accessoryData.Find(TLVType.Signature).DataValue;
            var accessoryX = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Accessory-Sign-Salt",
                "Pair-Setup-Accessory-Sign-Info",
                32);

            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(accessoryX);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(accessoryPairingId));
            accessoryInfo.Write(accessorySignPublic.Data);
            Assert("Signature verifies", Sodium.VerifyDetached(accessorySignature, accessoryInfo.ToArray(), accessorySignPublic));

            Console.WriteLine("Pair setup succeeded");
            Console.WriteLine("Accessory pairing Id: " + accessoryPairingId);
            Console.WriteLine("Accessory public key: " + Convert.ToBase64String(accessorySignPublic.Data));
        }

        public void PairVerify()
        {
            var deviceCurve = new Sodium.Curve25519Keypair();

            Console.WriteLine("device private key " + BitConverter.ToString(deviceCurve.SecretKey.Data));
            Console.WriteLine("Sending device public key " + BitConverter.ToString(deviceCurve.PublicKey.Data));

            var res = SendTLVMessage("/pair-verify", new TLVCollection() {
                 new TLV(TLVType.State, 1),
                 new TLV(TLVType.PublicKey, deviceCurve.PublicKey.Data)
            });
            
            Assert("Expect state 2", res.Find(TLVType.State).IntegerValue == 2);
            Assert("No error in state 2", res.Find(TLVType.Error) == null);

            var accessoryCurvePublic = new Sodium.Curve25519PublicKey(res.Find(TLVType.PublicKey).DataValue);
            var accessoryEncData = res.Find(TLVType.EncryptedData).DataValue;
            Console.WriteLine("Received accessory public key " + BitConverter.ToString(accessoryCurvePublic.Data));

            var sharedSecret = deviceCurve.SecretKey.ComputeSharedSecret(accessoryCurvePublic);
            Console.WriteLine("Shared secret: " + BitConverter.ToString(sharedSecret.Data));

            var sessionKey = new Sodium.Key(HKDF.SHA512(
                sharedSecret.Data,
                "Pair-Verify-Encrypt-Salt",
                "Pair-Verify-Encrypt-Info",
                32));

            var controlReadKey = new Sodium.Key(HKDF.SHA512(
                sharedSecret.Data,
                "Control-Salt",
                "Control-Read-Encryption-Key",
                32));
                
            var controlWriteKey = new Sodium.Key(HKDF.SHA512(
                sharedSecret.Data,
                "Control-Salt",
                "Control-Write-Encryption-Key",
                32));

            var accessoryPlainData = Sodium.Decrypt(
                accessoryEncData, null,
                "PV-Msg02", sessionKey);
            Assert("Decryption failed", accessoryPlainData != null);
            
            var accessoryData = TLVCollection.Deserialize(accessoryPlainData);
            var assertedPairingId = accessoryData.Find(TLVType.Identifier).StringValue;
            Assert("PairingId matches", assertedPairingId == accessoryPairingId);
            var accessorySignature = accessoryData.Find(TLVType.Signature).DataValue;
            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(accessoryCurvePublic.Data);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(accessoryPairingId));
            accessoryInfo.Write(deviceCurve.PublicKey.Data);
            Assert("Signature validates", Sodium.VerifyDetached(accessorySignature, accessoryInfo.ToArray(), accessorySignPublic));

            var deviceInfo = new MemoryStream();
            deviceInfo.Write(deviceCurve.PublicKey.Data);
            deviceInfo.Write(Encoding.ASCII.GetBytes(deviceId));
            deviceInfo.Write(accessoryCurvePublic.Data);

            var iosDeviceSignature = Sodium.SignDetached(deviceInfo.ToArray(), deviceSign.SecretKey);
            var iosSubTlv = new TLVCollection() {
                new TLV(TLVType.Identifier, deviceId),
                new TLV(TLVType.Signature, iosDeviceSignature)
            }.Serialize();
            var iosEncData = Sodium.Encrypt(iosSubTlv, null,
                "PV-Msg03", sessionKey);
            
            res = SendTLVMessage("/pair-verify", new TLVCollection() {
                new TLV(TLVType.State, 3),
                new TLV(TLVType.EncryptedData, iosEncData)
            });
            
            Assert("Expect state 4", res.Find(TLVType.State).IntegerValue == 4);
            Assert("Error in state 4", res.Find(TLVType.Error) == null);

            Console.WriteLine("Pair verify complete");
        }

        private string accessoryPairingId;
        private Sodium.Ed25519PublicKey accessorySignPublic;
    }
}