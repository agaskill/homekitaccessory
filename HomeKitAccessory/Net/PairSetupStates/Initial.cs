using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HomeKitAccessory.Data;

namespace HomeKitAccessory.Net.PairSetupStates
{
    class Initial : PairSetupState
    {
        public Initial(Server server)
            : base(server) {}
        
        public override TLVCollection HandlePairSetupRequest(TLVCollection request, out PairSetupState newState)
        {
            var state = request.State;
            if (state != 1)
                throw new InvalidOperationException("Invalid request state");
            var method = (int)request.Find(TLVType.Method).IntegerValue;
            if (method != 0 && method != 1)
                throw new InvalidOperationException(string.Format("Invalid method {0}", method));
            var srpServer = new SRPAuth.SRPServer(new PairSetupUserStore(server.SetupCode), SHA512.Create());
            var userVerifier = srpServer.ClientHello("Pair-Setup");
            Console.WriteLine("Sending state 2 response");
            newState = new PairSetupState2(server, srpServer);
            return new TLVCollection() {
                new TLV(TLVType.State, 2),
                new TLV(TLVType.PublicKey, userVerifier.ServerPublic),
                new TLV(TLVType.Salt, userVerifier.Salt)
            };
        }

        public override TLVCollection HandlePairVerifyRequest(TLVCollection request, out PairSetupState newState)
        {
            Console.WriteLine("initial pair verify request received");

            var state = request.State;
            if (state != 1)
                throw new InvalidOperationException("Invalid request state");
            var deviceSessionPublicKey = new Sodium.Curve25519PublicKey(request.Find(TLVType.PublicKey).DataValue);
            Console.WriteLine("Device's Curve25519 public key is " + BitConverter.ToString(deviceSessionPublicKey.Data));

            var accessorySession = new Sodium.Curve25519Keypair();
            var sharedSecret = accessorySession.SecretKey.ComputeSharedSecret(deviceSessionPublicKey);
            Console.WriteLine("Generated accessory Curve25519 keypair {0}", accessorySession);
            Console.WriteLine("Curve25519 shared secret is " + BitConverter.ToString(sharedSecret.Data));

            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(accessorySession.PublicKey.Data);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(server.PairingId));
            accessoryInfo.Write(deviceSessionPublicKey.Data);

            Console.WriteLine("Signing accessory info with accessory secret key "
                + BitConverter.ToString(server.PairingDatabase.SignKeyPair.SecretKey.Data));

            var accessorySignature = Sodium.SignDetached(
                accessoryInfo.ToArray(),
                server.PairingDatabase.SignKeyPair.SecretKey);

            Console.WriteLine("Generated signature " + BitConverter.ToString(accessorySignature));

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

            Console.WriteLine("Encrypting info sub-tlv with derived session key");

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