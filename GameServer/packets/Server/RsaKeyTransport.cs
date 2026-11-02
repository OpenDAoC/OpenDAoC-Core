using System;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Reflection;
using DOL.GS.ServerProperties;
using DOL.Logging;

namespace DOL.GS.PacketHandler
{
    // RSA key transport for 1.127 and 1.130 clients. The client generates a 256-byte RC4 key, encrypts it under the server's
    // public key and sends it in chunks: [00][len][RSA block] per chunk, each block the modulus size (one byte short
    // when its top byte is 0). Each block is raw RSA then OAEP with SHA-256, label "mythic" and a leading 0x01 where
    // RFC 8017 has 0x00. The plaintext is [12-byte header][256-byte RC4 key][2-byte trailer]. After the key transport,
    // the client encrypts its TCP packets, and expects every server packet (TCP and UDP) encrypted, with that RC4 key.
    public sealed class RsaKeyTransport : IClientEncryption
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public static readonly RsaKeyTransport Instance = new();

        private static BigInteger _n, _d;   // raw RSA params for no-padding decrypt
        private static int _k;              // modulus size in bytes, 0 until a key is loaded

        private const int HLen = 32;                    // SHA-256
        private static readonly byte[] LHash = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes("mythic"));

        // Offset and length of the RC4 key in the key transport plaintext.
        public const int Rc4KeyOffset = 12;
        public const int Rc4KeyLength = 256;

        // `Init` loads the key only if `client_enable_encryption_rsa` is on at startup, so changing the property later
        // has no effect until a restart.
        public static bool Enabled => _k > 0;

        // Checked once per connection, at the version reply (`CryptKeyRequestHandler`).
        public static bool AppliesTo(GameClient.eClientVersion version) => AppliesTo(Enabled, version);

        public static bool AppliesTo(bool enabled, GameClient.eClientVersion version) =>
            enabled && version is GameClient.eClientVersion.Version1127 or GameClient.eClientVersion.Version1130;

        private RsaKeyTransport() { }

        public bool DecryptInbound(GameClient client, byte[] buffer, int offset, int length)
        {
            IPacketEncoding encoding = client.PacketProcessor.Encoding;

            // The version reply sets RSAEncrypted. If the next packet decodes as an RSA key transport (OAEP padding
            // validation rejects any other content), the session is encrypted. If not, the client may be patched to run
            // unencrypted: carry on in plaintext and let the checksum decide.
            if (encoding.EncryptionState is eEncryptionState.RSAEncrypted)
            {
                // The chunks are everything after the 2-byte size field. The client's size field under-counts them by
                // 10 bytes, so take the framed length minus the size field.
                byte[] sbox = RecoverRc4Key(buffer.AsSpan(offset + 2, length - 2));

                if (sbox != null)
                {
                    encoding.SBox = sbox;
                    encoding.EncryptionState = eEncryptionState.PseudoRC4Encrypted;

                    if (log.IsInfoEnabled)
                        log.Info($"[rsa-rc4] RC4 established for {client.TcpEndpointAddress}");

                    return false;
                }

                encoding.EncryptionState = eEncryptionState.NotEncrypted;

                if (log.IsInfoEnabled)
                    log.Info($"[rsa-rc4] no key transport from {client.TcpEndpointAddress}; continuing unencrypted");

                return true;
            }

            if (encoding.EncryptionState is eEncryptionState.PseudoRC4Encrypted)
                encoding.DecryptPacket(buffer, offset, false);

            return true;
        }

        public void EncryptOutbound(GameClient client, byte[] buffer, int offset, bool udp)
        {
            IPacketEncoding encoding = client.PacketProcessor.Encoding;

            if (encoding.EncryptionState is eEncryptionState.PseudoRC4Encrypted)
                encoding.EncryptPacket(buffer, offset, udp);
        }

        public void LogChecksumFailure(GameClient client)
        {
            // A connection that fell back to plaintext: if this was its first packet, it was neither a key transport for
            // this server's key nor plaintext.
            if (client.PacketProcessor.Encoding.EncryptionState is eEncryptionState.NotEncrypted && log.IsWarnEnabled)
                log.Warn($"{client.TcpEndpointAddress} sent no key transport for this server's RSA key. If this was its first " +
                    "packet, its game.dll most likely embeds a different public key (unmodified, or set up for another server).");
        }

        public static void LoadPrivateKey(string pem)
        {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            RSAParameters p = rsa.ExportParameters(true);
            _n = BigIntUnsigned(p.Modulus);
            _d = BigIntUnsigned(p.D);
            _k = p.Modulus.Length;
        }

        public static bool Init()
        {
            if (!Properties.CLIENT_ENABLE_ENCRYPTION_RSA)
            {
                if (log.IsInfoEnabled)
                    log.Info("RSA client encryption disabled (client_enable_encryption_rsa=false)");

                return true;
            }

            string root = GameServer.Instance?.Configuration?.RootDirectory ?? ".";
            string path = Path.Combine(root, Properties.CLIENT_RSA_PRIVATE_KEY_FILE);

            try
            {
                LoadPrivateKey(File.ReadAllText(path));

                if (log.IsInfoEnabled)
                    log.Info($"RSA private key loaded from '{path}' ({_k * 8}-bit modulus)");

                return true;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Failed to load RSA private key from '{path}'. " +
                              "Disable client_enable_encryption_rsa or provide the key.", e);

                return false;
            }
        }

        // `body` is the packet after its size field: [00][len][RSA block] chunks. Returns the scheduled S-box, or null if
        // this isn't a key transport for this server's key.
        public static byte[] RecoverRc4Key(ReadOnlySpan<byte> body)
        {
            if (!Enabled)
                return null;

            byte[] plaintext = DecryptKeyTransport(body);
            if (plaintext == null)
                return null;

            if (plaintext.Length < Rc4KeyOffset + Rc4KeyLength)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"[rsa-rc4] recovered plaintext too short ({plaintext.Length} bytes) for RC4 key");

                return null;
            }

            byte[] key = new byte[Rc4KeyLength];
            Array.Copy(plaintext, Rc4KeyOffset, key, 0, Rc4KeyLength);

            // `PacketEncoding168` uses its S-box as given, so schedule the key here.
            return PacketEncoding1110.GenerateSBox(key);
        }

        private static byte[] DecryptKeyTransport(ReadOnlySpan<byte> body)
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
                        log.Debug($"[rsa-rc4] chunk {chunks} OAEP decode failed");

                    return null;
                }
                ms.Write(part, 0, part.Length);
                chunks++;
            }

            if (chunks == 0)
            {
                if (log.IsDebugEnabled)
                    log.Debug("[rsa-rc4] no RSA chunks found in key-transport body");

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

        // Returns the message, or null if the padding is invalid. Every check runs before rejecting, so a block that
        // fails early takes as long as one that fails late.
        private static byte[] MythicOaepDecode(byte[] block)
        {
            if (block.Length != _k)
                return null;

            ReadOnlySpan<byte> maskedSeed = block.AsSpan(1, HLen);
            ReadOnlySpan<byte> maskedDb = block.AsSpan(1 + HLen);
            byte[] seed = Xor(maskedSeed, Mgf1(maskedDb, HLen));
            byte[] db = Xor(maskedDb, Mgf1(seed, maskedDb.Length));

            // db = lHash || PS (0x00 bytes) || 0x01 || message
            int separator = 0;

            for (int i = HLen; i < db.Length; i++)
            {
                if (separator == 0 && db[i] != 0x00)
                    separator = i;
            }

            bool valid = block[0] == 0x01;
            valid &= CryptographicOperations.FixedTimeEquals(db.AsSpan(0, HLen), LHash);
            valid &= separator != 0 && db[separator] == 0x01;

            return valid ? db[(separator + 1)..] : null;
        }

        private static byte[] Mgf1(ReadOnlySpan<byte> seed, int length)
        {
            byte[] output = new byte[length];
            byte[] input = new byte[seed.Length + 4];
            seed.CopyTo(input);
            Span<byte> digest = stackalloc byte[HLen];

            for (int counter = 0, position = 0; position < length; counter++, position += HLen)
            {
                BinaryPrimitives.WriteInt32BigEndian(input.AsSpan(seed.Length), counter);
                SHA256.HashData(input, digest);
                digest[..Math.Min(HLen, length - position)].CopyTo(output.AsSpan(position));
            }

            return output;
        }

        private static byte[] Xor(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
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
