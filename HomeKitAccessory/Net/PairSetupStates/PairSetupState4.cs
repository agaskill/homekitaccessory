using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HomeKitAccessory.Data;

namespace HomeKitAccessory.Net.PairSetupStates
{
    class PairSetupState4 : PairSetupState
    {
        private byte[] srpKey;

        public PairSetupState4(Server server, byte[] srpKey)
            : base(server)
        {
            this.srpKey = srpKey;
        }

        public override TLVCollection HandlePairSetupRequest(TLVCollection request, out PairSetupState newState)
        {
            Console.WriteLine("Handling pair setup request in state 4");
            var state = request.State;
            if (state != 5)
                throw new InvalidOperationException("Invalid request state " + state);
            var deviceEncryptedData = request.Find(TLVType.EncryptedData).DataValue;

            var sessionKey = new Sodium.Key(HKDF.SHA512(srpKey,
                "Pair-Setup-Encrypt-Salt",
                "Pair-Setup-Encrypt-Info",
                32));

            var devicePlainData = Sodium.Decrypt(
                deviceEncryptedData,
                null, "PS-Msg05",
                sessionKey);

            Console.WriteLine("Decrypted device data");

            var deviceSubTLV = TLVCollection.Deserialize(devicePlainData);
            var deviceId = deviceSubTLV.Find(TLVType.Identifier).DataValue;
            var deviceLTPK = new Sodium.Ed25519PublicKey(deviceSubTLV.Find(TLVType.PublicKey).DataValue);
            var deviceSignature = deviceSubTLV.Find(TLVType.Signature).DataValue;

            var iosDeviceX = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Controller-Sign-Salt",
                "Pair-Setup-Controller-Sign-Info",
                32);

            var iosDeviceInfo = new MemoryStream();
            iosDeviceInfo.Write(iosDeviceX);
            iosDeviceInfo.Write(deviceId);
            iosDeviceInfo.Write(deviceLTPK.Data);

            if (!Sodium.VerifyDetached(deviceSignature, iosDeviceInfo.ToArray(), deviceLTPK))
            {
                Console.WriteLine("Signature verification failed");
                newState = null;
                return new TLVCollection() {
                    new TLV(TLVType.State, 6),
                    new TLV(TLVType.Error, TLVError.Authentication)
                };
            }

            Console.WriteLine("device signature verified");

            server.AddPairing(Encoding.ASCII.GetString(deviceId), deviceLTPK);

            var accessoryLongTerm = server.PairingDatabase.SignKeyPair;

            var accessoryX = HKDF.SHA512(
                srpKey,
                "Pair-Setup-Accessory-Sign-Salt",
                "Pair-Setup-Accessory-Sign-Info",
                32);
            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(accessoryX);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(server.PairingId));
            accessoryInfo.Write(accessoryLongTerm.PublicKey.Data);
            var signature = Sodium.SignDetached(accessoryInfo.ToArray(), accessoryLongTerm.SecretKey);

            var subtlv = new TLVCollection() {
                new TLV(TLVType.Identifier, server.PairingId),
                new TLV(TLVType.PublicKey, accessoryLongTerm.PublicKey.Data),
                new TLV(TLVType.Signature, signature)
            }.Serialize();

            var encdata = Sodium.Encrypt(subtlv, null,
                "PS-Msg06", sessionKey);

            Console.WriteLine("Responding with encrypted accessory info");

            newState = new Initial(server);

            return new TLVCollection() {
                new TLV(TLVType.State, 6),
                new TLV(TLVType.EncryptedData, encdata)
            };
        }
    }
}