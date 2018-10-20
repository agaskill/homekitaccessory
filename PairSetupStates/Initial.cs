using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HomeKitAccessory.PairSetupStates
{
    class Initial : PairSetupState
    {
        public Initial(Server server)
            : base(server) {}
        
        public override List<TLV> HandlePairSetupRequest(List<TLV> request, out PairSetupState newState)
        {
            var state = GetState(request);
            if (state != 1)
                throw new InvalidOperationException("Invalid request state");
            var method = (int)request.Find(x => x.Tag == TLVType.Method).IntegerValue;
            if (method != 0 && method != 1)
                throw new InvalidOperationException(string.Format("Invalid method {0}", method));
            var srpServer = new SRPAuth.SRPServer(new PairSetupUserStore(server.SetupCode), SHA512.Create());
            var userVerifier = srpServer.ClientHello("Pair-Setup");
            Console.WriteLine("Sending state 2 response");
            newState = new PairSetupState2(server, srpServer);
            return new List<TLV>() {
                new TLV(TLVType.State, 2),
                new TLV(TLVType.PublicKey, userVerifier.ServerPublic),
                new TLV(TLVType.Salt, userVerifier.Salt)
            };
        }

        public override List<TLV> HandlePairVerifyRequest(List<TLV> request, out PairSetupState newState)
        {
            Console.WriteLine("initial pair verify request received");

            var state = GetState(request);
            if (state != 1)
                throw new InvalidOperationException("Invalid request state");
            var devicePublic = request.Find(x => x.Tag == TLVType.PublicKey).DataValue;
            Console.WriteLine("Device's Curve25519 public key is " + BitConverter.ToString(devicePublic));

            var keyPair = Sodium.BoxKeypair();
            var sharedSecret = Sodium.SharedSecret(devicePublic, keyPair.secretKey);
            Console.WriteLine("Generated accessory Curve25519 keypair {0}", keyPair);
            Console.WriteLine("Curve25519 shared secret is " + BitConverter.ToString(sharedSecret));

            var accessoryInfo = new MemoryStream();
            accessoryInfo.Write(keyPair.publicKey);
            accessoryInfo.Write(Encoding.ASCII.GetBytes(server.PairingId));
            accessoryInfo.Write(devicePublic);

            Console.WriteLine("Signing accessory info with accessory secret key "
                + BitConverter.ToString(server.PairingDatabase.SignKeyPair.secretKey));

            var accessorySignature = Sodium.SignDetached(
                accessoryInfo.ToArray(),
                server.PairingDatabase.SignKeyPair.secretKey);

            Console.WriteLine("Generated signature " + BitConverter.ToString(accessorySignature));

            var subTlv = TLV.Serialize(
                new TLV(TLVType.Identifier, server.PairingId),
                new TLV(TLVType.Signature, accessorySignature));
            
            var sessionKey = HKDF.SHA512(
                sharedSecret,
                "Pair-Verify-Encrypt-Salt",
                "Pair-Verify-Encrypt-Info",
                32);

            Console.WriteLine("Encrypting info sub-tlv with derived session key");

            var encryptedData = Sodium.Encrypt(
                subTlv, null,
                Encoding.ASCII.GetBytes("\0\0\0\0PV-Msg02"),
                sessionKey);

            newState = new PairVerifyState2(server, sessionKey, sharedSecret, devicePublic, keyPair.publicKey);

            return new List<TLV>() {
                new TLV(TLVType.State, 2),
                new TLV(TLVType.PublicKey, keyPair.publicKey),
                new TLV(TLVType.EncryptedData, encryptedData)
            };
        }
    }
}