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

    public class TestClient
    {
        Sodium.Ed25519Keypair signKey;
        string deviceId;

        public TestClient(string host, int port)
        {
            signKey = Sodium.SignKeypair();
            deviceId = Guid.NewGuid().ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri("http://" + host + ":" + port);
        }

        private HttpClient client;

        private MediaTypeHeaderValue mediaTypePairingTlv = new MediaTypeHeaderValue("application/pairing+tlv8");

        private List<TLV> SendTLVMessage(string path, byte[] req)
        {
            var content = new ByteArrayContent(req);
            content.Headers.ContentType = mediaTypePairingTlv;

            var response = client.PostAsync(path, content).Result;

            if (response.Content.Headers.ContentType.MediaType == mediaTypePairingTlv.MediaType)
            {
                var tlvs = TLV.Deserialize(response.Content.ReadAsByteArrayAsync().Result);
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

            var res = SendTLVMessage("/pair-setup", TLV.Serialize(
                new TLV(TLVType.State, 1),
                new TLV(TLVType.Method, 0)));
            Assert("Expect state 2", TLV.Find(res, TLVType.State).IntegerValue == 2);
            Assert("Expect no error in state 2", TLV.Find(res, TLVType.Error) == null);

            var accessorySRPPublic = TLV.Find(res, TLVType.PublicKey).DataValue;
            var accessorySRPSalt = TLV.Find(res, TLVType.Salt).DataValue;

            var srpClient = new SRPAuth.SRPClient(SHA512.Create());

            var srpSession = srpClient.ServerExchange("Pair-Setup", setupCode, new SRPAuth.ServerSRPParams
            {
                Prime = N3072,
                Generator = g3072,
                Salt = accessorySRPSalt,
                ServerPublic = accessorySRPPublic
            });

            var srpKey = srpClient.ComputeStandardKey(srpSession.PremasterSecret);

            res = SendTLVMessage("/pair-setup", TLV.Serialize(
                new TLV(TLVType.State, 3),
                new TLV(TLVType.PublicKey, srpSession.ClientPublic),
                new TLV(TLVType.Proof, srpClient.ComputeClientProof("Pair-Setup", accessorySRPSalt, srpKey))
            ));

            Assert("Expect state 4", TLV.Find(res, TLVType.State).IntegerValue == 4);
            Assert("Expect no error in state 4", TLV.Find(res, TLVType.Error) == null);

            var iosDeviceX = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Controller-Sign-Salt",
                "Pair-Setup-Controller-Sign-Info",
                32);
            
            var iosDeviceInfo = new MemoryStream();
            iosDeviceInfo.Write(iosDeviceX);
            iosDeviceInfo.Write(Encoding.ASCII.GetBytes(deviceId));
            iosDeviceInfo.Write(signKey.publicKey);
            var iosDeviceSignature = Sodium.SignDetached(iosDeviceInfo.ToArray(), signKey.secretKey);

            var iosInfoTlv = TLV.Serialize(
                new TLV(TLVType.Identifier, deviceId),
                new TLV(TLVType.PublicKey, signKey.publicKey),
                new TLV(TLVType.Signature, iosDeviceSignature));

            var sessionKey = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Encrypt-Salt",
                "Pair-Setup-Encrypt-Info",
                32);
            
            var iosEncData = Sodium.Encrypt(
                iosInfoTlv, null,
                Encoding.ASCII.GetBytes("\0\0\0\0PS-Msg05"),
                sessionKey);
            
            res = SendTLVMessage("/pair-setup", TLV.Serialize(
                new TLV(TLVType.State, 5),
                new TLV(TLVType.EncryptedData, iosEncData)));

            Assert("Expect state 6", TLV.Find(res, TLVType.State).IntegerValue == 6);
            Assert("Expect no error in state 6", TLV.Find(res, TLVType.Error) == null);

            var accessoryEncData = TLV.Find(res, TLVType.EncryptedData).DataValue;
            var accessoryPlainData = Sodium.Decrypt(accessoryEncData, null,
                Encoding.ASCII.GetBytes("\0\0\0\0PS-Msg06"),
                sessionKey);
            var accessoryData = TLV.Deserialize(accessoryPlainData);
            accessoryPairingId = TLV.Find(accessoryData, TLVType.Identifier).StringValue;
            accessoryPublic = TLV.Find(accessoryData, TLVType.PublicKey).DataValue;
            var accessorySignature = TLV.Find(accessoryData, TLVType.Signature).DataValue;
            var accessoryX = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Accessory-Sign-Salt",
                "Pair-Setup-Accessory-Sign-Info",
                32);

            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(accessoryX);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(accessoryPairingId));
            accessoryInfo.Write(accessoryPublic);
            Assert("Signature verifies", Sodium.VerifyDetached(accessorySignature, accessoryInfo.ToArray(), accessoryPublic));

            Console.WriteLine("Pair setup succeeded");
            Console.WriteLine("Accessory pairing Id: " + accessoryPairingId);
            Console.WriteLine("Accessory public key: " + Convert.ToBase64String(accessoryPublic));
        }

        public void PairVerify()
        {
            var keypair = Sodium.BoxKeypair();

            Console.WriteLine("device private key " + BitConverter.ToString(keypair.secretKey));
            Console.WriteLine("Sending device public key " + BitConverter.ToString(keypair.publicKey));

            var res = SendTLVMessage("/pair-verify", TLV.Serialize(
                 new TLV(TLVType.State, 1),
                 new TLV(TLVType.PublicKey, keypair.publicKey)));
            
            Assert("Expect state 2", TLV.Find(res, TLVType.State).IntegerValue == 2);
            Assert("No error in state 2", TLV.Find(res, TLVType.Error) == null);

            var accessoryCurvePublic = TLV.Find(res, TLVType.PublicKey).DataValue;
            var accessoryEncData = TLV.Find(res, TLVType.EncryptedData).DataValue;
            Console.WriteLine("Received accessory public key " + BitConverter.ToString(accessoryCurvePublic));

            var sharedSecret = Sodium.SharedSecret(accessoryCurvePublic, keypair.secretKey);
            Console.WriteLine("Shared secret: " + BitConverter.ToString(sharedSecret));

            var sessionKey = HKDF.SHA512(
                sharedSecret,
                "Pair-Verify-Encrypt-Salt",
                "Pair-Verify-Encrypt-Info",
                32);

            var accessoryPlainData = Sodium.Decrypt(
                accessoryEncData, null,
                Encoding.ASCII.GetBytes("\0\0\0\0PV-Msg02"),
                sessionKey);
            Assert("Decryption failed", accessoryPlainData != null);
            
            var accessoryData = TLV.Deserialize(accessoryPlainData);
            var assertedPairingId = TLV.Find(accessoryData, TLVType.Identifier).StringValue;
            Assert("PairingId matches", assertedPairingId == accessoryPairingId);
            var accessorySignature = TLV.Find(accessoryData, TLVType.Signature).DataValue;
            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(accessoryCurvePublic);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(accessoryPairingId));
            accessoryInfo.Write(keypair.publicKey);
            Assert("Signature validates", Sodium.VerifyDetached(accessorySignature, accessoryInfo.ToArray(), accessoryPublic));

            var deviceInfo = new MemoryStream();
            deviceInfo.Write(signKey.publicKey);
            deviceInfo.Write(Encoding.ASCII.GetBytes(deviceId));
            deviceInfo.Write(accessoryCurvePublic);

            var iosDeviceSignature = Sodium.SignDetached(deviceInfo.ToArray(), signKey.secretKey);
            var iosSubTlv = TLV.Serialize(
                new TLV(TLVType.Identifier, deviceId),
                new TLV(TLVType.Signature, iosDeviceSignature));
            var iosEncData = Sodium.Encrypt(iosSubTlv, null,
                Encoding.ASCII.GetBytes("\0\0\0\0PV-Msg03"),
                sessionKey);
            
            res = SendTLVMessage("/pair-verify", TLV.Serialize(
                new TLV(TLVType.State, 3),
                new TLV(TLVType.EncryptedData, iosEncData)));
            
            Assert("Expect state 4", TLV.Find(res, TLVType.State).IntegerValue == 4);
            Assert("Error in state 4", TLV.Find(res, TLVType.Error) == null);

            Console.WriteLine("Pair verify complete");
        }

        private string accessoryPairingId;
        private byte[] accessoryPublic;

        private static readonly byte[] N3072 = SRPAuth.Utilities.ParseByteArray(
@"FFFFFFFF FFFFFFFF C90FDAA2 2168C234 C4C6628B 80DC1CD1 29024E08 8A67CC74
  020BBEA6 3B139B22 514A0879 8E3404DD EF9519B3 CD3A431B 302B0A6D F25F1437
  4FE1356D 6D51C245 E485B576 625E7EC6 F44C42E9 A637ED6B 0BFF5CB6 F406B7ED
  EE386BFB 5A899FA5 AE9F2411 7C4B1FE6 49286651 ECE45B3D C2007CB8 A163BF05
  98DA4836 1C55D39A 69163FA8 FD24CF5F 83655D23 DCA3AD96 1C62F356 208552BB
  9ED52907 7096966D 670C354E 4ABC9804 F1746C08 CA18217C 32905E46 2E36CE3B
  E39E772C 180E8603 9B2783A2 EC07A28F B5C55DF0 6F4C52C9 DE2BCBF6 95581718
  3995497C EA956AE5 15D22618 98FA0510 15728E5A 8AAAC42D AD33170D 04507A33
  A85521AB DF1CBA64 ECFB8504 58DBEF0A 8AEA7157 5D060C7D B3970F85 A6E1E4C7
  ABF5AE8C DB0933D7 1E8C94E0 4A25619D CEE3D226 1AD2EE6B F12FFA06 D98A0864
  D8760273 3EC86A64 521F2B18 177B200C BBE11757 7A615D6C 770988C0 BAD946E2
  08E24FA0 74E5AB31 43DB5BFC E0FD108E 4B82D120 A93AD2CA FFFFFFFF FFFFFFFF");
        private static readonly byte[] g3072 = { 5 };
    }
}