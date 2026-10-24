using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Reflection;
using DOL.GS.ServerProperties;
using DOL.Logging;

namespace DOL.GS.PacketHandler
{
    /// <summary>
    /// RSA key transport for 1.130 clients (game.dll).
    ///
    /// A 1.130 client generates a random 256-byte RC4 key, RSA-encrypts it under the server's
    /// public key using a Mythic-customised OAEP, and sends it chunked. The server holds the
    /// matching private key, decrypts, recovers the 256-byte key and feeds it to the per-packet
    /// stream cipher (PacketEncoding1110). Pure managed .NET (BigInteger + SHA-256), so it runs on
    /// Linux with no native dependency.
    ///
    /// Reverse-engineered + verified against real captured key-transports:
    /// - On-wire packet: [u16 BE size][ then [00][len][RSA block] xN ]; each RSA block is the
    ///   modulus size (occasionally one byte short when the top byte is 0; left-pad before decrypt).
    /// - Each block: raw RSA (c^d mod n) then Mythic OAEP decode:
    ///     hash/MGF = SHA-256, label = "mythic" (lHash = SHA-256("mythic")), leading byte 0x01
    ///     (RFC 8017 uses 0x00; this is the one deviation).
    /// - Concatenated plaintext (270 bytes): [12-byte header][256-byte RC4 key][2-byte trailer].
    /// </summary>
    public static class Rsa1130
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static RSA _privateKey;
        private static BigInteger _n, _d;   // raw RSA params for no-padding decrypt
        private static int _k;              // modulus size in bytes

        private const int HLen = 32;                    // SHA-256
        private static readonly byte[] Label = System.Text.Encoding.ASCII.GetBytes("mythic");
        private static readonly byte[] LHash = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes("mythic"));

        /// <summary>Offset + length of the RC4 key inside the recovered key-transport plaintext.</summary>
        public const int Rc4KeyOffset = 12;
        public const int Rc4KeyLength = 256;

        public static bool Enabled => _privateKey != null && Properties.CLIENT_ENABLE_1130_CRYPTO;

        /// <summary>
        /// Whether a client of this version uses 1.130 crypto on this server. Receiving and sending both
        /// check this, so a client is encrypted both ways or not at all.
        /// </summary>
        public static bool AppliesTo(GameClient.eClientVersion version) => AppliesTo(Enabled, version);

        public static bool AppliesTo(bool enabled, GameClient.eClientVersion version) =>
            enabled && version is GameClient.eClientVersion.Version1130;

        public static int ModulusBytes => _k;

        /// <summary>Loads the game RSA private key from PEM text. `Init` reads it from `client_rsa_private_key_file`.</summary>
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

        /// <summary>Init hook for GameServer.Start(). Loads the game RSA private key if 1.130 crypto is on.</summary>
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

        /// <summary>
        /// Decrypt a full key-transport packet payload (the GSPacketIn body, framed as
        /// [00][len][RSA block] chunks after the server's 3-byte header has been stripped), and
        /// return the 256-byte RC4 key, or null on failure.
        /// </summary>
        /// <param name="body">The chunk stream: repeated [00][len][ciphertext].</param>
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

        /// <summary>RC4 key schedule: 256-byte S-box from the key, as in PacketEncoding1110.</summary>
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

        /// <summary>Reassemble + decrypt the [00][len][block] chunks -> concatenated plaintext.</summary>
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

        /// <summary>Raw RSA: m = c^d mod n, returned left-padded to the modulus size (k bytes).</summary>
        private static byte[] RawRsaDecrypt(ReadOnlySpan<byte> ct)
        {
            BigInteger c = BigIntUnsigned(ct);
            BigInteger m = BigInteger.ModPow(c, _d, _n);
            return ToFixed(m, _k);
        }

        /// <summary>
        /// Mythic OAEP decode of a k-byte block: SHA-256, label "mythic", leading byte 0x01.
        /// Returns the message, or null if padding is invalid.
        /// </summary>
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

        /// <summary>MGF1 with SHA-256.</summary>
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
