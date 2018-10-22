namespace HomeKitAccessory
{
    using System;
    using System.Runtime.InteropServices;

    public static class Sodium
    {
        [DllImport("libsodium")]
        private static extern int sodium_init();

        static Sodium()
        {
            sodium_init();
        }

        public class Curve25519Keypair
        {
            public byte[] publicKey;
            public byte[] secretKey;

            public override string ToString()
            {
                return "public: " + BitConverter.ToString(publicKey) + "; secret: " + BitConverter.ToString(secretKey);
            }
        }

        [DllImport("libsodium")]
        private static extern int crypto_box_keypair(
            [In, Out] byte[] pk,
            [In, Out] byte[] sk);

        public static Curve25519Keypair BoxKeypair()
        {
            var pk = new byte[32];
            var sk = new byte[32];
            crypto_box_keypair(pk, sk);
            return new Curve25519Keypair
            {
                publicKey = pk,
                secretKey = sk
            };
        }

        [DllImport("libsodium")]
        private static extern int crypto_box_beforenm(
            [In, Out] byte[] k,
            [In] byte[] pk,
            [In] byte[] sk);

        public static byte[] SharedSecret(byte[] pk, byte[] sk)
        {
            var sharedSecret = new byte[32];
            if (crypto_box_beforenm(sharedSecret, pk, sk) != 0)
                throw new ArgumentException("public key and private key can not make a shared secret key");
            return sharedSecret;
        }

        [DllImport("libsodium")]
        private static extern int crypto_aead_chacha20poly1305_ietf_encrypt(
            [In, Out] byte[] ciphertext,
            out ulong clen,
            [In] byte[] msg,
            ulong mlen,
            [In] byte[] ad,
            ulong adlen,
            [In] byte[] nsec,
            [In] byte[] npub,
            [In] byte[] key);

        public static byte[] Encrypt(byte[] msg, byte[] ad, byte[] nonce, byte[] key)
        {
            if (nonce.Length != 12)
                throw new ArgumentException(nameof(nonce));
            if (key.Length != 32)
                throw new ArgumentException(nameof(key));
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));

            var result = new byte[msg.Length + 16];
            int rc = crypto_aead_chacha20poly1305_ietf_encrypt(
                result,
                out ulong resultlen,
                msg,
                (ulong)msg.LongLength,
                ad,
                (ad == null ? 0UL : (ulong)ad.LongLength),
                null,
                nonce,
                key);
            return result;
        }

        [DllImport("libsodium")]
        private static extern int crypto_aead_chacha20poly1305_ietf_decrypt(
            [In, Out] byte[] msg,
            out ulong msglen,
            [In] byte[] nsec,
            [In] byte[] ciphertext,
            ulong cipherlen,
            [In] byte[] ad,
            ulong adlen,
            [In] byte[] npub,
            [In] byte[] key);

        public static byte[] Decrypt(byte[] ciphertext, byte[] ad, byte[] nonce, byte[] key)
        {
            if (nonce.Length != 12)
                throw new ArgumentException(nameof(nonce));
            if (key.Length != 32)
                throw new ArgumentException(nameof(key));
            if (ciphertext == null)
                throw new ArgumentNullException(nameof(ciphertext));
            
            var result = new byte[ciphertext.Length - 16];
            int rc = crypto_aead_chacha20poly1305_ietf_decrypt(
                result,
                out ulong msglen,
                null, ciphertext,
                (ulong)ciphertext.LongLength,
                ad,
                (ad == null ? 0UL : (ulong)ad.LongLength),
                nonce,
                key);
            if (rc == -1) {
                return null;
            }
            return result;
        }

        [DllImport("libsodium")]
        private static extern int crypto_sign_keypair(
            [Out] byte[] pk,
            [Out] byte[] sk);

        public static Ed25519Keypair SignKeypair()
        {
            var pk = new byte[32];
            var sk = new byte[64];
            crypto_sign_keypair(pk, sk);
            return new Ed25519Keypair
            {
                publicKey = pk,
                secretKey = sk
            };
        }

        public class Ed25519Keypair
        {
            public byte[] publicKey;
            public byte[] secretKey;
            
            public override string ToString()
            {
                return "public: " + BitConverter.ToString(publicKey) + "; secret: " + BitConverter.ToString(secretKey);
            }
        }

        [DllImport("libsodium")]
        private static extern int crypto_sign_ed25519_detached(
            [In ,Out] byte[] sig,
            out ulong siglen,
            [In] byte[] m,
            ulong mlen,
            [In] byte[] sk);
        
        public static byte[] SignDetached(byte[] msg, byte[] secretKey)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (secretKey == null)
                throw new ArgumentNullException(nameof(secretKey));
            if (secretKey.Length != 64)
                throw new ArgumentException("Secret key length is invalid", nameof(secretKey));

            var sig = new byte[64];
            var rc = crypto_sign_ed25519_detached(sig, out ulong siglen, msg, (ulong)msg.LongLength, secretKey);
            return sig;
        }

        [DllImport("libsodium")]
        private static extern int crypto_sign_ed25519_verify_detached(
            [In] byte[] sig,
            [In] byte[] m,
            ulong mlen,
            [In] byte[] pk);

        public static bool VerifyDetached(byte[] sig, byte[] msg, byte[] pk)
        {
            if (sig == null)
                throw new ArgumentNullException(nameof(sig));
            if (sig.Length != 64)
                throw new ArgumentException("Signature length invalid", nameof(sig));
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (pk == null)
                throw new ArgumentNullException(nameof(pk));
            if (pk.Length != 32)
                throw new ArgumentException("private key length invalid", nameof(pk));
            var rc = crypto_sign_ed25519_verify_detached(sig, msg, (ulong)msg.LongLength, pk);
            return rc == 0;
        }

        [DllImport("libsodium")]
        private static extern int crypto_sign_ed25519_sk_to_pk(
            [In, Out] byte[] pk,
            [In] byte[] sk);

        public static byte[] SignSecretKeyToPublicKey(byte[] secretKey)
        {
            if (secretKey == null)
                throw new ArgumentNullException(nameof(secretKey));
            if (secretKey.Length != 64)
                throw new ArgumentException("Invalid secret key length", nameof(secretKey));
            var publicKey = new byte[32];
            var rc = crypto_sign_ed25519_sk_to_pk(publicKey, secretKey);
            if (rc != 0)
                throw new InvalidOperationException("Failed to get public key from secret key");
            return publicKey;
        }
    }
}