using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HomeKitAccessory.Data;

namespace HomeKitAccessory.Net.PairSetupStates
{
    class PairVerifyState2 : PairSetupState
    {
        private Sodium.Key sessionKey;
        private Sodium.Key controlReadKey;
        private Sodium.Key controlWriteKey;
        private Sodium.Curve25519PublicKey deviceCurvePublic;
        private Sodium.Curve25519PublicKey accessoryCurvePublic;

        public PairVerifyState2(
            Server server,
            Sodium.Key sessionKey,
            Sodium.Key controlReadKey,
            Sodium.Key controlWriteKey,
            Sodium.Curve25519PublicKey deviceSessionPublic,
            Sodium.Curve25519PublicKey accessorySessionPublic)
            : base(server)
        {
            this.sessionKey = sessionKey;
            this.controlReadKey = controlReadKey;
            this.controlWriteKey = controlWriteKey;
            this.deviceCurvePublic = deviceSessionPublic;
            this.accessoryCurvePublic = accessorySessionPublic;
        }

        public override TLVCollection HandlePairVerifyRequest(TLVCollection request, out PairSetupState newState)
        {
            Console.WriteLine("pair verify message received in state 2");
            var state = request.State;
            if (state != 3)
                throw new InvalidOperationException("Unexpected state " + state);
            var encryptedData = request.Find(TLVType.EncryptedData).DataValue;
            var plainData = Sodium.Decrypt(
                encryptedData, null,
                "PV-Msg03", sessionKey);
            if (plainData == null)
            {
                Console.WriteLine("Data did not decrypt");
                newState = new Initial(server);
                return new TLVCollection() {
                    new TLV(TLVType.State, 4),
                    new TLV(TLVType.Error, TLVError.Authentication)
                };
            }

            var deviceData = TLVCollection.Deserialize(plainData);
            var devicePairingId = deviceData.Find(TLVType.Identifier).StringValue;
            Console.WriteLine("Device is " + devicePairingId);

            var devicePublic = server.PairingDatabase.FindKey(devicePairingId);
            if (devicePublic == null)
            {
                Console.WriteLine("Unknown device");
                newState = new Initial(server);
                return new TLVCollection() {
                    new TLV(TLVType.State, 4),
                    new TLV(TLVType.Error, TLVError.Authentication)
                };
            }

            var deviceSignature = deviceData.Find(TLVType.Signature).DataValue;
            var deviceInfo = new MemoryStream();
            deviceInfo.Write(deviceCurvePublic.Data);
            deviceInfo.Write(Encoding.ASCII.GetBytes(devicePairingId));
            deviceInfo.Write(accessoryCurvePublic.Data);

            if (!Sodium.VerifyDetached(deviceSignature, deviceInfo.ToArray(), devicePublic))
            {
                Console.WriteLine("Signature validation failed");
                newState = new Initial(server);
                return new TLVCollection() {
                    new TLV(TLVType.State, 4),
                    new TLV(TLVType.Error, TLVError.Authentication)
                };
            }

            newState = new Verified(
                server,
                controlReadKey,
                controlWriteKey);

            Console.WriteLine("Pair verify complete");

            return new TLVCollection()
            {
                new TLV(TLVType.State, 4)
            };
        }
    }
}