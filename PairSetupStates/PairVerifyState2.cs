using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HomeKitAccessory.PairSetupStates
{
    class PairVerifyState2 : PairSetupState
    {
        private byte[] sessionKey;
        private byte[] sharedSecret;
        private byte[] deviceSessionPublic;
        private byte[] accessorySessionPublic;

        public PairVerifyState2(Server server,
            byte[] sessionKey,
            byte[] sharedSecret,
            byte[] deviceSessionPublic,
            byte[] accessorySessionPublic)
            : base(server)
        {
            this.sessionKey = sessionKey;
            this.sharedSecret = sharedSecret;
            this.deviceSessionPublic = deviceSessionPublic;
            this.accessorySessionPublic = accessorySessionPublic;
        }

        public override List<TLV> HandlePairVerifyRequest(List<TLV> request, out PairSetupState newState)
        {
            Console.WriteLine("pair verify message received in state 2");
            var state = GetState(request);
            if (state != 3)
                throw new InvalidOperationException("Unexpected state " + state);
            var encryptedData = request.Find(x => x.Tag == TLVType.EncryptedData).DataValue;
            var plainData = Sodium.Decrypt(encryptedData, null,
                Encoding.ASCII.GetBytes("\0\0\0\0PV-Msg03"), sessionKey);
            if (plainData == null)
            {
                Console.WriteLine("Data did not decrypt");
                newState = new Initial(server);
                return new List<TLV>() {
                    new TLV(TLVType.State, 4),
                    new TLV(TLVType.Error, TLVError.Authentication)
                };
            }

            var deviceData = TLV.Deserialize(plainData);
            var devicePairingId = deviceData.Find(x => x.Tag == TLVType.Identifier).StringValue;
            Console.WriteLine("Device is " + devicePairingId);

            var devicePublic = server.PairingDatabase.FindKey(devicePairingId);
            if (devicePublic == null)
            {
                Console.WriteLine("Unknown device");
                newState = new Initial(server);
                return new List<TLV>() {
                    new TLV(TLVType.State, 4),
                    new TLV(TLVType.Error, TLVError.Authentication)
                };
            }

            var deviceSignature = deviceData.Find(x => x.Tag == TLVType.Signature).DataValue;
            var deviceInfo = new MemoryStream();
            deviceInfo.Write(deviceSessionPublic);
            deviceInfo.Write(Encoding.ASCII.GetBytes(devicePairingId));
            deviceInfo.Write(accessorySessionPublic);

            if (!Sodium.VerifyDetached(deviceSignature, deviceInfo.ToArray(), devicePublic))
            {
                Console.WriteLine("Signature validation failed");
                newState = new Initial(server);
                return new List<TLV>() {
                    new TLV(TLVType.State, 4),
                    new TLV(TLVType.Error, TLVError.Authentication)
                };
            }

            newState = new Verified(
                server,
                HKDF.SHA512(
                    sharedSecret,
                    "Control-Salt",
                    "Control-Read-Encryption-Key",
                    32),
                HKDF.SHA512(
                    sharedSecret,
                    "Control-Salt",
                    "Control-Write-Encryption-Key",
                    32));

            return new List<TLV>()
            {
                new TLV(TLVType.State, 4)
            };
        }
    }
}