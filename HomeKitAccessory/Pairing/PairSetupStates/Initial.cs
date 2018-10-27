using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HomeKitAccessory.Data;
using NLog;

namespace HomeKitAccessory.Pairing.PairSetupStates
{
    class Initial : PairSetupState
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Initial(Server server)
            : base(server) {}
        
        public override TLVCollection HandlePairSetupRequest(TLVCollection request, out PairSetupState newState)
        {
            logger.Debug("Pair setup message received in initial state");
            var state = request.State;
            if (state != 1)
                throw new InvalidOperationException("Invalid request state");
            var method = (int)request.Find(TLVType.Method).IntegerValue;
            if (method != 0 && method != 1)
                throw new InvalidOperationException(string.Format("Invalid method {0}", method));
            var srpServer = new SRPAuth.SRPServer(server.UserStore, SHA512.Create());
            var userVerifier = srpServer.ClientHello("Pair-Setup");
            logger.Debug("Sending state 2 response");
            newState = new PairSetupState2(server, srpServer);
            return new TLVCollection() {
                new TLV(TLVType.State, 2),
                new TLV(TLVType.PublicKey, userVerifier.ServerPublic),
                new TLV(TLVType.Salt, userVerifier.Salt)
            };
        }

        public override TLVCollection HandlePairVerifyRequest(TLVCollection request, out PairSetupState newState)
        {
            logger.Debug("initial pair verify request received");

            var state = request.State;
            if (state != 1)
                throw new InvalidOperationException("Invalid request state");
            var deviceSessionPublicKey = new Sodium.Curve25519PublicKey(request.Find(TLVType.PublicKey).DataValue);

            var accessorySession = new Sodium.Curve25519Keypair();
            var sharedSecret = accessorySession.SecretKey.ComputeSharedSecret(deviceSessionPublicKey);
            logger.Debug("Generated accessory Curve25519 keypair and shared secret");

            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(accessorySession.PublicKey.Data);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(server.PairingId));
            accessoryInfo.Write(deviceSessionPublicKey.Data);

            var accessorySignature = Sodium.SignDetached(
                accessoryInfo.ToArray(),
                server.PairingDatabase.SignKeyPair.SecretKey);

            logger.Debug("Generated signature {0}", BitConverter.ToString(accessorySignature));

            var subTlv = new TLVCollection() {
                new TLV(TLVType.Identifier, server.PairingId),
                new TLV(TLVType.Signature, accessorySignature)
            }.Serialize();
            
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

            logger.Debug("Session keys generated");

            var encryptedData = Sodium.Encrypt(
                subTlv, null,
                "PV-Msg02", sessionKey);

            newState = new PairVerifyState2(
                server, sessionKey,
                controlReadKey, controlWriteKey,
                deviceSessionPublicKey, accessorySession.PublicKey);

            return new TLVCollection() {
                new TLV(TLVType.State, 2),
                new TLV(TLVType.PublicKey, accessorySession.PublicKey.Data),
                new TLV(TLVType.EncryptedData, encryptedData)
            };
        }
    }
}