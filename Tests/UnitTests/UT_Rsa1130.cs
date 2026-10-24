using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Logging;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    // After the version reply, a 1.130 client's next packet is either an RSA key transport (the session is encrypted)
    // or, for a client patched to run unencrypted, an ordinary plaintext packet. `RecoverRc4Key` tells them apart.
    [TestFixture]
    public class UT_Rsa1130
    {
        private const int HLEN = 32; // SHA-256
        private static readonly byte[] LHash = SHA256.HashData("mythic"u8.ToArray());

        private RSA _serverKey;

        [OneTimeSetUp]
        public void LoadServerKey()
        {
            LoggerManager.InitializeWithExplicitLibrary(null, LogLibrary.None);
            _serverKey = RSA.Create(1024);
            Rsa1130.LoadPrivateKey(_serverKey.ExportPkcs8PrivateKeyPem());
        }

        [Test]
        public void RecoverRc4Key_KeyTransportForServerKey_ReturnsScheduledKey()
        {
            byte[] key = RandomNumberGenerator.GetBytes(Rsa1130.Rc4KeyLength);

            byte[] sbox = Rsa1130.RecoverRc4Key(CreateKeyTransport(_serverKey, key));

            Assert.That(sbox, Is.EqualTo(ScheduleKey(key)));
        }

        [Test]
        public void RecoverRc4Key_KeyTransportForAnotherKey_ReturnsNull()
        {
            using RSA otherKey = RSA.Create(1024);
            byte[] key = RandomNumberGenerator.GetBytes(Rsa1130.Rc4KeyLength);

            Assert.That(Rsa1130.RecoverRc4Key(CreateKeyTransport(otherKey, key)), Is.Null);
        }

        // Body = the packet after its 2-byte size field: [sequence][session][parameter][code][payload][checksum].
        // A leading 0x00 and a small second byte also look like the start of an RSA chunk.
        [TestCase(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA9, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC })]
        [TestCase(new byte[] { 0x00, 0x08, 0x00, 0x05, 0x00, 0x00, 0x00, 0xA7, 0x41, 0x42, 0x43, 0x44, 0x01, 0x02 })]
        [TestCase(new byte[] { 0x01, 0x02, 0x00, 0x05, 0x00, 0x00, 0x00, 0xF4, 0x00, 0x00 })]
        public void RecoverRc4Key_PlaintextPacket_ReturnsNull(byte[] body)
        {
            Assert.That(Rsa1130.RecoverRc4Key(body), Is.Null);
        }

        // TCP, UDP-over-TCP and UDP sends all check `EncryptsOutbound`: this connection's state, not just the version.
        [TestCase(GameClient.eClientVersion.Version1130, true, eEncryptionState.PseudoRC4Encrypted, true)]
        [TestCase(GameClient.eClientVersion.Version1130, true, eEncryptionState.RSAEncrypted, false)]
        [TestCase(GameClient.eClientVersion.Version1130, true, eEncryptionState.NotEncrypted, false)]
        [TestCase(GameClient.eClientVersion.Version1130, false, eEncryptionState.PseudoRC4Encrypted, false)]
        [TestCase(GameClient.eClientVersion.Version1127, true, eEncryptionState.PseudoRC4Encrypted, false)]
        public void EncryptsOutbound_DependsOnThisConnectionsState(GameClient.eClientVersion version, bool cryptoOn, eEncryptionState state, bool expected)
        {
            GameClient client = CreateClient(version, cryptoOn);
            client.PacketProcessor.Encoding.EncryptionState = state;

            Assert.That(client.PacketProcessor.EncryptsOutbound, Is.EqualTo(expected));
        }

        [Test]
        public void FirstPacketAfterVersionReply_KeyTransport_EncryptsThisConnection()
        {
            GameClient client = CreateClient(GameClient.eClientVersion.Version1130, true);
            client.PacketProcessor.Encoding.EncryptionState = eEncryptionState.RSAEncrypted; // Set by the version reply.
            byte[] body = CreateKeyTransport(_serverKey, RandomNumberGenerator.GetBytes(Rsa1130.Rc4KeyLength));
            byte[] packet = new byte[2 + body.Length];
            packet[0] = (byte) ((body.Length - 10) >> 8); // The client's size field under-counts the chunks by 10.
            packet[1] = (byte) (body.Length - 10);
            body.CopyTo(packet, 2);

            Receive(client, packet);

            Assert.That(client.PacketProcessor.Encoding.EncryptionState, Is.EqualTo(eEncryptionState.PseudoRC4Encrypted));
            Assert.That(client.PacketProcessor.EncryptsOutbound, Is.True);
        }

        [Test]
        public void FirstPacketAfterVersionReply_Plaintext_LeavesThisConnectionUnencrypted()
        {
            GameClient client = CreateClient(GameClient.eClientVersion.Version1130, true);
            client.PacketProcessor.Encoding.EncryptionState = eEncryptionState.RSAEncrypted; // Set by the version reply.
            byte[] packet = [0x00, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x11, 0x22, 0x33, 0x44, 0x00, 0x00];
            ushort checksum = PacketProcessor.CalculateChecksum(packet, 0, packet.Length - 2);
            packet[^2] = (byte) (checksum >> 8);
            packet[^1] = (byte) checksum;

            Receive(client, packet);

            Assert.That(client.PacketProcessor.Encoding.EncryptionState, Is.EqualTo(eEncryptionState.NotEncrypted));
            Assert.That(client.PacketProcessor.EncryptsOutbound, Is.False);
        }

        [TearDown]
        public void TurnCryptoOff()
        {
            Properties.CLIENT_ENABLE_1130_CRYPTO = false;
        }

        private static GameClient CreateClient(GameClient.eClientVersion version, bool cryptoOn)
        {
            Properties.CLIENT_ENABLE_1130_CRYPTO = cryptoOn;
            GameClient client = new(null) { Version = version };
            client.PacketProcessor = new PacketProcessor(client);
            return client;
        }

        // What `GameClient.OnReceive` does once the version is known.
        private static void Receive(GameClient client, byte[] packet)
        {
            byte[] receiveBuffer = (byte[]) typeof(DOL.Network.BaseClient)
                .GetProperty("ReceiveBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(client);
            packet.CopyTo(receiveBuffer, 0);
            typeof(GameClient).GetMethod("ProcessInboundTcpBytes", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(client, [packet.Length]);
        }

        // [00][len][RSA block] per chunk; the plaintext is [12-byte header][256-byte RC4 key][2-byte trailer].
        private static byte[] CreateKeyTransport(RSA rsa, byte[] key)
        {
            byte[] plaintext = new byte[Rsa1130.Rc4KeyOffset + Rsa1130.Rc4KeyLength + 2];
            Array.Copy(key, 0, plaintext, Rsa1130.Rc4KeyOffset, key.Length);

            RSAParameters p = rsa.ExportParameters(false);
            BigInteger n = new(p.Modulus, true, true);
            BigInteger e = new(p.Exponent, true, true);
            int k = p.Modulus.Length;
            int maxMessage = k - 2 * HLEN - 2;
            using MemoryStream body = new();

            for (int offset = 0; offset < plaintext.Length; offset += maxMessage)
            {
                int length = Math.Min(maxMessage, plaintext.Length - offset);
                byte[] block = MythicOaepEncode(plaintext.AsSpan(offset, length), k);
                byte[] ciphertext = BigInteger.ModPow(new BigInteger(block, true, true), e, n).ToByteArray(true, true);
                body.WriteByte(0x00);
                body.WriteByte((byte) k);
                body.Write(new byte[k - ciphertext.Length]);
                body.Write(ciphertext);
            }

            return body.ToArray();
        }

        // RFC 8017 OAEP with SHA-256, label "mythic", and a leading 0x01 where RFC 8017 has 0x00.
        private static byte[] MythicOaepEncode(ReadOnlySpan<byte> message, int k)
        {
            byte[] db = new byte[k - 1 - HLEN];
            LHash.CopyTo(db, 0);
            db[db.Length - message.Length - 1] = 0x01;
            message.CopyTo(db.AsSpan(db.Length - message.Length));

            byte[] seed = RandomNumberGenerator.GetBytes(HLEN);
            byte[] maskedDb = Xor(db, Mgf1(seed, db.Length));
            byte[] maskedSeed = Xor(seed, Mgf1(maskedDb, HLEN));

            byte[] block = new byte[k];
            block[0] = 0x01;
            maskedSeed.CopyTo(block, 1);
            maskedDb.CopyTo(block, 1 + HLEN);
            return block;
        }

        private static byte[] Mgf1(byte[] seed, int length)
        {
            byte[] output = new byte[length];
            byte[] input = new byte[seed.Length + 4];
            seed.CopyTo(input, 0);

            for (int counter = 0, position = 0; position < length; counter++, position += HLEN)
            {
                input[seed.Length] = (byte) (counter >> 24);
                input[seed.Length + 1] = (byte) (counter >> 16);
                input[seed.Length + 2] = (byte) (counter >> 8);
                input[seed.Length + 3] = (byte) counter;
                byte[] digest = SHA256.HashData(input);
                Array.Copy(digest, 0, output, position, Math.Min(HLEN, length - position));
            }

            return output;
        }

        private static byte[] Xor(byte[] a, byte[] b)
        {
            byte[] result = new byte[a.Length];

            for (int i = 0; i < a.Length; i++)
                result[i] = (byte) (a[i] ^ b[i]);

            return result;
        }

        // Standard RC4 key schedule over the full 256-byte key.
        private static byte[] ScheduleKey(byte[] key)
        {
            byte[] s = new byte[256];

            for (int i = 0; i < s.Length; i++)
                s[i] = (byte) i;

            for (int i = 0, j = 0; i < s.Length; i++)
            {
                j = (j + s[i] + key[i % key.Length]) & 0xFF;
                (s[i], s[j]) = (s[j], s[i]);
            }

            return s;
        }
    }
}
