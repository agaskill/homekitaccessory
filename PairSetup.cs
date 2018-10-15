using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HomeKitAccessory
{
    abstract class PairSetupState
    {
        public abstract List<TLV> HandleRequest(List<TLV> request, out PairSetupState newState);

        protected int GetState(List<TLV> request)
        {
            return (int)request.Find(x => x.Tag == TLVType.State).IntegerValue;
        }
    }

    class PairSetupState0 : PairSetupState
    {
        private const string setupCode = "724-87-534";

        public override List<TLV> HandleRequest(List<TLV> request, out PairSetupState newState)
        {
            var state = GetState(request);
            if (state != 1)
                throw new InvalidOperationException("Invalid request state");
            var method = (int)request.Find(x => x.Tag == TLVType.Method).IntegerValue;
            if (method != 0 && method != 1)
                throw new InvalidOperationException(string.Format("Invalid method {0}", method));
            var server = new SRPAuth.SRPServer(new PairSetupUserStore(setupCode), SHA512.Create());
            Console.WriteLine(setupCode);
            var userVerifier = server.ClientHello("Pair-Setup");
            Console.WriteLine("Sending state 2 response");
            newState = new PairSetupState2(server);
            return new List<TLV>() {
                new TLV(TLVType.State, 2),
                new TLV(TLVType.PublicKey, userVerifier.ServerPublic),
                new TLV(TLVType.Salt, userVerifier.Salt)
            };
        }
    }

    class PairSetupState2 : PairSetupState
    {
        private SRPAuth.SRPServer sRPServer;

        public PairSetupState2(SRPAuth.SRPServer sRPServer)
        {
            this.sRPServer = sRPServer;
        }

        public override List<TLV> HandleRequest(List<TLV> request, out PairSetupState newState)
        {
            Console.WriteLine("Handling pair setup request in state 2");

            var state = GetState(request);
            if (state != 3)
                throw new InvalidOperationException("Invalid request state " + state);

            var devicePublic = request.Find(x => x.Tag == TLVType.PublicKey).DataValue;
            var deviceProof = request.Find(x => x.Tag == TLVType.Proof).DataValue;

            //TODO: Verify client proof

            var premasterSecret = sRPServer.ClientExchange(devicePublic);
            var srpKey = sRPServer.ComputeStandardKey(premasterSecret);
            var serverProof = sRPServer.ComputeServerProof(deviceProof, srpKey);

            newState = new PairSetupState4(srpKey);

            Console.WriteLine("Sending state 4 response");

            return new List<TLV>() {
                new TLV(TLVType.State, 4),
                new TLV(TLVType.Proof, serverProof)
            };
        }
    }

    class PairSetupState4 : PairSetupState
    {
        private byte[] srpKey;

        private const string pairingId = "54:FB:D8:A0:5A:C6";

        public PairSetupState4(byte[] srpKey)
        {
            this.srpKey = srpKey;
        }

        public override List<TLV> HandleRequest(List<TLV> request, out PairSetupState newState)
        {
            Console.WriteLine("Handling pair setup request in state 4");
            var state = GetState(request);
            if (state != 5)
                throw new InvalidOperationException("Invalid request state " + state);
            var deviceEncryptedData = request.Find(x => x.Tag == TLVType.EncryptedData).DataValue;

            var sessionKey = HKDF.SHA512(srpKey,
                "Pair-Setup-Encrypt-Salt",
                "Pair-Setup-Encrypt-Info",
                32);

            var devicePlainData = Sodium.Decrypt(
                deviceEncryptedData,
                null,
                Encoding.ASCII.GetBytes("\0\0\0\0PS-Msg05"),
                sessionKey);

            Console.WriteLine("Decrypted device data");

            var deviceSubTLV = TLV.Deserialize(devicePlainData);
            var deviceId = deviceSubTLV.Find(x => x.Tag == TLVType.Identifier).DataValue;
            var deviceLTPK = deviceSubTLV.Find(x => x.Tag == TLVType.PublicKey).DataValue;
            var deviceSignature = deviceSubTLV.Find(x => x.Tag == TLVType.Signature).DataValue;

            var iosDeviceX = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Controller-Sign-Salt",
                "Pair-Setup-Controller-Sign-Info",
                32);

            var iosDeviceInfo = new MemoryStream();
            iosDeviceInfo.Write(iosDeviceX);
            iosDeviceInfo.Write(deviceId);
            iosDeviceInfo.Write(deviceLTPK);

            if (!Sodium.VerifyDetached(deviceSignature, iosDeviceInfo.ToArray(), deviceLTPK))
            {
                Console.WriteLine("Signature verification failed");
                newState = null;
                return new List<TLV>() {
                    new TLV(TLVType.State, 6),
                    new TLV(TLVType.Error, TLVError.Authentication)
                };
            }

            Console.WriteLine("device signature verified");

            var accessoryLongTerm = Sodium.SignKeypair();

            SavePairingInfo(Encoding.ASCII.GetString(deviceId), deviceLTPK, accessoryLongTerm.secretKey);

            var accessoryX = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Accessory-Sign-Salt",
                "Pair-Setup-Accessory-Sign-Info",
                32);
            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(accessoryX);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(pairingId));
            accessoryInfo.Write(accessoryLongTerm.publicKey);
            var signature = Sodium.SignDetached(accessoryInfo.ToArray(), accessoryLongTerm.secretKey);

            var subtlv = TLV.Serialize(new List<TLV>() {
                new TLV(TLVType.Identifier, pairingId),
                new TLV(TLVType.PublicKey, accessoryLongTerm.publicKey),
                new TLV(TLVType.Signature, signature)
            });

            var encdata = Sodium.Encrypt(subtlv, null,
                Encoding.ASCII.GetBytes("\0\0\0\0PS-Msg06"),
                sessionKey);

            Console.WriteLine("Responding with encrypted accessory info");

            newState = new PairSetupState0();

            return new List<TLV>() {
                new TLV(TLVType.State, 6),
                new TLV(TLVType.EncryptedData, encdata)
            };
        }

        public void SavePairingInfo(string deviceId, byte[] deviceLTPK, byte[] accessoryLTSK)
        {
            var pairingInfo = Newtonsoft.Json.JsonConvert.SerializeObject(new {
                deviceId,
                deviceLTPK
            });
            File.WriteAllText("pairinginfo.json", pairingInfo);
        }
    }
}