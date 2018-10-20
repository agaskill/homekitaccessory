using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HomeKitAccessory.PairSetupStates
{
    class PairSetupState4 : PairSetupState
    {
        private byte[] srpKey;

        public PairSetupState4(Server server, byte[] srpKey)
            : base(server)
        {
            this.srpKey = srpKey;
        }

        public override List<TLV> HandlePairSetupRequest(List<TLV> request, out PairSetupState newState)
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

            server.PairingDatabase.AddPairing(Encoding.ASCII.GetString(deviceId), deviceLTPK);

            var accessoryLongTerm = server.PairingDatabase.SignKeyPair;

            var accessoryX = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Accessory-Sign-Salt",
                "Pair-Setup-Accessory-Sign-Info",
                32);
            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(accessoryX);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(server.PairingId));
            accessoryInfo.Write(accessoryLongTerm.publicKey);
            var signature = Sodium.SignDetached(accessoryInfo.ToArray(), accessoryLongTerm.secretKey);

            var subtlv = TLV.Serialize(new List<TLV>() {
                new TLV(TLVType.Identifier, server.PairingId),
                new TLV(TLVType.PublicKey, accessoryLongTerm.publicKey),
                new TLV(TLVType.Signature, signature)
            });

            var encdata = Sodium.Encrypt(subtlv, null,
                Encoding.ASCII.GetBytes("\0\0\0\0PS-Msg06"),
                sessionKey);

            Console.WriteLine("Responding with encrypted accessory info");

            newState = new Initial(server);

            return new List<TLV>() {
                new TLV(TLVType.State, 6),
                new TLV(TLVType.EncryptedData, encdata)
            };
        }
    }
}