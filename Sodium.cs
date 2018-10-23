namespace HomeKitAccessory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    public static class Sodium
    {
        [DllImport("libsodium")]
        private static extern int sodium_init();

        static Sodium()
        {
            sodium_init();
        }

        public class Key
        {
            public byte[] Data {get; set;}

            public Key(byte[] data)
            {
                Data = data;
            }
        }

        public class Ed25519PublicKey : Key
        {
            public Ed25519PublicKey(byte[] data) : base(data) {}
        }

        public class Ed25519SecretKey : Key
        {
            public Ed25519SecretKey(byte[] data) : base(data) {}

            public Ed25519PublicKey ComputePublic()
            {
                var publicKey = new byte[32];
                var rc = crypto_sign_ed25519_sk_to_pk(publicKey, Data);
                if (rc != 0)
                    throw new InvalidOperationException("Failed to get public key from secret key");
                return new Ed25519PublicKey(publicKey);
            }
        }

        public class Curve25519PublicKey : Key
        {
            public Curve25519PublicKey(byte[] data) : base(data) {}
        }

        [DllImport("libsodium")]
        private static extern void randombytes_buf(
            [Out] byte[] buf,
            IntPtr size);

        public static byte[] GenRandom(int length)
        {
            var buff = new byte[length];
            randombytes_buf(buff, (IntPtr)length);
            return buff;
        }

        public static void FillRandom(byte[] buff)
        {
            randombytes_buf(buff, (IntPtr)buff.Length);
        }

        public class Curve25519SecretKey : Key
        {
            public Curve25519SecretKey(byte[] data) : base(data) {}

            public Curve25519SecretKey() : base(GenRandom(32)) {}

            public Curve25519PublicKey ComputePublic()
            {
                var pk = new byte[32];
                crypto_scalarmult_curve25519_base(pk, Data);
                return new Curve25519PublicKey(pk);
            }

            public Curve25519SharedSecret ComputeSharedSecret(Curve25519PublicKey otherPublic)
            {
                var sharedSecret = new byte[32];
                if (crypto_scalarmult_curve25519(sharedSecret, Data, otherPublic.Data) != 0)
                    throw new ArgumentException("public key and private key can not make a shared secret key");
                return new Curve25519SharedSecret(sharedSecret);
            }
        }

        public class Curve25519SharedSecret : Key
        {
            public Curve25519SharedSecret(byte[] data) : base(data) {}
        }

        public class Curve25519Keypair
        {
            public Curve25519PublicKey PublicKey {get;set;}
            public Curve25519SecretKey SecretKey {get;set;}

            public Curve25519Keypair()
            {
                SecretKey = new Curve25519SecretKey();
                PublicKey = SecretKey.ComputePublic();
            }

            public override string ToString()
            {
                return "public: " + BitConverter.ToString(PublicKey.Data) + "; secret: " + BitConverter.ToString(SecretKey.Data);
            }
        }

        [DllImport("libsodium")]
        private static extern int crypto_scalarmult_curve25519(
            [Out] byte[] k,
            [In] byte[] sk,
            [In] byte[] pk);

        [DllImport("libsodium")]
        private static extern int crypto_scalarmult_curve25519_base(
            [Out] byte[] pk,
            [In] byte[] sk);

        public class Salt
        {
            public byte[] Data {get;set;}

            public static explicit operator Salt(byte[] data)
            {
                return new Salt { Data  = data };
            }
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
        
        public static byte[] Encrypt(byte[] msg, byte[] ad, string nonce, Key key)
        {
            var nonceBytes = Encoding.UTF8.GetBytes(nonce);
            if (nonceBytes.Length < 12) {
                var tmp = new byte[12];
                Array.Copy(nonceBytes, 0, tmp, 12 - nonceBytes.Length, nonceBytes.Length);
                nonceBytes = tmp;
            }
            return Encrypt(msg, ad, nonceBytes, key);
        }

        public static byte[] Encrypt(byte[] msg, byte[] ad, byte[] nonce, Key key)
        {
            if (nonce.Length != 12)
                throw new ArgumentException(nameof(nonce));
            if (key.Data.Length != 32)
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
                (ulong)(ad?.LongLength ?? 0),
                null,
                nonce,
                key.Data);
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

        public static byte[] Decrypt(byte[] ciphertext, byte[] ad, string nonce, Key key)
        {
            var nonceBytes = Encoding.UTF8.GetBytes(nonce);
            if (nonceBytes.Length < 12) {
                var tmp = new byte[12];
                Array.Copy(nonceBytes, 0, tmp, 12 - nonceBytes.Length, nonceBytes.Length);
                nonceBytes = tmp;
            }
            return Decrypt(ciphertext, ad, nonceBytes, key);
        }

        public static byte[] Decrypt(byte[] ciphertext, byte[] ad, byte[] nonce, Key key)
        {
            if (nonce.Length != 12)
                throw new ArgumentException(nameof(nonce));
            if (key.Data.Length != 32)
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
                (ulong)(ad?.LongLength ?? 0),
                nonce,
                key.Data);
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
                PublicKey = new Ed25519PublicKey(pk),
                SecretKey = new Ed25519SecretKey(sk)
            };
        }

        public class Ed25519Keypair
        {
            public Ed25519PublicKey PublicKey {get;set;}
            public Ed25519SecretKey SecretKey {get;set;}
            
            public override string ToString()
            {
                return "public: " + BitConverter.ToString(PublicKey.Data) + "; secret: " + BitConverter.ToString(SecretKey.Data);
            }
        }

        [DllImport("libsodium")]
        private static extern int crypto_sign_ed25519_detached(
            [In ,Out] byte[] sig,
            out ulong siglen,
            [In] byte[] m,
            ulong mlen,
            [In] byte[] sk);
        
        public static byte[] SignDetached(byte[] msg, Ed25519SecretKey secretKey)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (secretKey == null)
                throw new ArgumentNullException(nameof(secretKey));
            if (secretKey.Data.Length != 64)
                throw new ArgumentException("Secret key length is invalid", nameof(secretKey));

            var sig = new byte[64];
            var rc = crypto_sign_ed25519_detached(sig, out ulong siglen, msg, (ulong)msg.LongLength, secretKey.Data);
            return sig;
        }

        [DllImport("libsodium")]
        private static extern int crypto_sign_ed25519_verify_detached(
            [In] byte[] sig,
            [In] byte[] m,
            ulong mlen,
            [In] byte[] pk);

        public static bool VerifyDetached(byte[] sig, byte[] msg, Ed25519PublicKey pk)
        {
            if (sig == null)
                throw new ArgumentNullException(nameof(sig));
            if (sig.Length != 64)
                throw new ArgumentException("Signature length invalid", nameof(sig));
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (pk == null)
                throw new ArgumentNullException(nameof(pk));
            if (pk.Data.Length != 32)
                throw new ArgumentException("private key length invalid", nameof(pk));
            var rc = crypto_sign_ed25519_verify_detached(sig, msg, (ulong)msg.LongLength, pk.Data);
            return rc == 0;
        }

        [DllImport("libsodium")]
        private static extern int crypto_sign_ed25519_sk_to_pk(
            [In, Out] byte[] pk,
            [In] byte[] sk);
    }
}