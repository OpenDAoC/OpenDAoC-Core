using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Reflection;
using DOL.GS.ServerProperties;
using DOL.Logging;

namespace DOL.GS.PacketHandler
{
    // RSA key transport for 1.130 clients. The client generates a 256-byte RC4 key, encrypts it under the server's
    // public key and sends it in chunks: [00][len][RSA block] per chunk, each block the modulus size (one byte short
    // when its top byte is 0). Each block is raw RSA then OAEP with SHA-256, label "mythic" and a leading 0x01 where
    // RFC 8017 has 0x00. The plaintext is [12-byte header][256-byte RC4 key][2-byte trailer].
    public static class Rsa1130
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static RSA _privateKey;
        private static BigInteger _n, _d;   // raw RSA params for no-padding decrypt
        private static int _k;              // modulus size in bytes

        private const int HLen = 32;                    // SHA-256
        private static readonly byte[] Label = System.Text.Encoding.ASCII.GetBytes("mythic");
        private static readonly byte[] LHash = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes("mythic"));

        // Offset and length of the RC4 key in the key transport plaintext.
        public const int Rc4KeyOffset = 12;
        public const int Rc4KeyLength = 256;

        // `Init` loads the key only if `client_enable_1130_crypto` is on at startup, so changing the property later
        // has no effect until a restart.
        public static bool Enabled => _privateKey != null;

        // Checked once per connection, at the version reply (`CryptKeyRequestHandler`).
        public static bool AppliesTo(GameClient.eClientVersion version) => AppliesTo(Enabled, version);

        public static bool AppliesTo(bool enabled, GameClient.eClientVersion version) =>
            enabled && version is GameClient.eClientVersion.Version1130;

        public static int ModulusBytes => _k;

        public static void LoadPrivateKey(string pem)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            RSAParameters p = rsa.ExportParameters(true);
            _n = BigIntUnsigned(p.Modulus);
            _d = BigIntUnsigned(p.D);
            _k = p.Modulus.Length;
            _privateKey = rsa;
        }

        public static bool Init()
        {
            if (!Properties.CLIENT_ENABLE_1130_CRYPTO)
            {
                if (log.IsInfoEnabled)
                    log.Info("1.130 client crypto disabled (client_enable_1130_crypto=false)");

                return true;
            }

            string root = GameServer.Instance?.Configuration?.RootDirectory ?? ".";
            string path = Path.Combine(root, Properties.CLIENT_RSA_PRIVATE_KEY_FILE);

            try
            {
                LoadPrivateKey(File.ReadAllText(path));

                if (log.IsInfoEnabled)
                    log.Info($"1.130 RSA private key loaded from '{path}' ({_privateKey.KeySize}-bit, {_k}-byte block)");

                return true;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Failed to load 1.130 RSA private key from '{path}'. " +
                              "Disable client_enable_1130_crypto or provide the key.", e);

                return false;
            }
        }

        // `body` is the packet after its size field: [00][len][RSA block] chunks. Returns the scheduled S-box, or null if
        // this isn't a key transport for this server's key.
        public static byte[] RecoverRc4Key(ReadOnlySpan<byte> body)
        {
            if (_privateKey == null)
                return null;

            byte[] plaintext = DecryptKeyTransport(body);
            if (plaintext == null)
                return null;

            if (plaintext.Length < Rc4KeyOffset + Rc4KeyLength)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"[1130-crypto] recovered plaintext too short ({plaintext.Length} bytes) for RC4 key");

                return null;
            }

            byte[] key = new byte[Rc4KeyLength];
            Array.Copy(plaintext, Rc4KeyOffset, key, 0, Rc4KeyLength);

            // Return the SCHEDULED S-box (RC4 KSA applied). PacketEncoding168's SBox setter stores
            // the value as-is and DecryptPacket uses it directly, so it must already be scheduled.
            // (The key schedule is the same as PacketEncoding1110.GenerateSBox.)
            return GenerateSBox(key);
        }

        private static byte[] GenerateSBox(byte[] key)
        {
            var sbox = new byte[256];
            for (int x = 0; x < 256; x++) sbox[x] = (byte)x;
            for (int x = 0, y = 0; x < 256; x++)
            {
                y = (y + sbox[x] + key[x % key.Length]) & 255;
                (sbox[x], sbox[y]) = (sbox[y], sbox[x]);
            }
            return sbox;
        }

        public static byte[] DecryptKeyTransport(ReadOnlySpan<byte> body)
        {
            using var ms = new MemoryStream();
            int p = 0;
            int chunks = 0;
            while (p + 2 <= body.Length && body[p] == 0x00)
            {
                int len = body[p + 1];
                if (len == 0 || p + 2 + len > body.Length)
                    break;
                ReadOnlySpan<byte> ct = body.Slice(p + 2, len);
                p += 2 + len;

                byte[] block = RawRsaDecrypt(ct);          // k-byte OAEP block
                byte[] part = MythicOaepDecode(block);
                if (part == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug($"[1130-crypto] chunk {chunks} OAEP decode failed");

                    return null;
                }
                ms.Write(part, 0, part.Length);
                chunks++;
            }

            if (chunks == 0)
            {
                if (log.IsDebugEnabled)
                    log.Debug("[1130-crypto] no RSA chunks found in key-transport body");

                return null;
            }
            return ms.ToArray();
        }

        // m = c^d mod n, left-padded to the modulus size.
        private static byte[] RawRsaDecrypt(ReadOnlySpan<byte> ct)
        {
            BigInteger c = BigIntUnsigned(ct);
            BigInteger m = BigInteger.ModPow(c, _d, _n);
            return ToFixed(m, _k);
        }

        // Returns the message, or null if the padding is invalid.
        private static byte[] MythicOaepDecode(byte[] block)
        {
            if (block.Length != _k || block[0] != 0x01)
                return null;

            var maskedSeed = new byte[HLen];
            Array.Copy(block, 1, maskedSeed, 0, HLen);
            var maskedDb = new byte[_k - 1 - HLen];
            Array.Copy(block, 1 + HLen, maskedDb, 0, maskedDb.Length);

            byte[] seed = Xor(maskedSeed, Mgf1(maskedDb, HLen));
            byte[] db = Xor(maskedDb, Mgf1(seed, maskedDb.Length));

            // db = lHash || PS(0x00..) || 0x01 || message
            for (int i = 0; i < HLen; i++)
                if (db[i] != LHash[i])
                    return null;

            int j = HLen;
            while (j < db.Length && db[j] == 0x00) j++;
            if (j >= db.Length || db[j] != 0x01)
                return null;

            var msg = new byte[db.Length - (j + 1)];
            Array.Copy(db, j + 1, msg, 0, msg.Length);
            return msg;
        }

        private static byte[] Mgf1(byte[] seed, int length)
        {
            var outb = new byte[length];
            int pos = 0;
            uint counter = 0;
            Span<byte> cnt = stackalloc byte[4];
            while (pos < length)
            {
                cnt[0] = (byte)(counter >> 24);
                cnt[1] = (byte)(counter >> 16);
                cnt[2] = (byte)(counter >> 8);
                cnt[3] = (byte)counter;
                using var sha = SHA256.Create();
                sha.TransformBlock(seed, 0, seed.Length, null, 0);
                sha.TransformFinalBlock(cnt.ToArray(), 0, 4);
                byte[] digest = sha.Hash;
                int n = Math.Min(HLen, length - pos);
                Array.Copy(digest, 0, outb, pos, n);
                pos += n;
                counter++;
            }
            return outb;
        }

        private static byte[] Xor(byte[] a, byte[] b)
        {
            var r = new byte[a.Length];
            for (int i = 0; i < a.Length; i++) r[i] = (byte)(a[i] ^ b[i]);
            return r;
        }

        private static BigInteger BigIntUnsigned(ReadOnlySpan<byte> beBytes)
            => new BigInteger(beBytes, isUnsigned: true, isBigEndian: true);

        private static byte[] ToFixed(BigInteger v, int len)
        {
            byte[] be = v.ToByteArray(isUnsigned: true, isBigEndian: true);
            if (be.Length == len) return be;
            var r = new byte[len];
            Array.Copy(be, 0, r, len - be.Length, be.Length);   // left-pad
            return r;
        }
    }
}
