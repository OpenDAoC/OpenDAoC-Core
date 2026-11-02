using System;
using DOL.GS.PacketHandler;
using DOL.Logging;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_PacketEncoding168
    {
        private const int OFFSET = 7; // A UDP datagram can carry several frames; this one isn't the first.
        private const int PAYLOAD_SIZE = 23;
        private const ushort COUNTER = 0x1234;
        private const byte CODE = 0xA9;

        // `GameClient` and `RsaKeyTransport` log.
        [OneTimeSetUp]
        public void InitializeLogging()
        {
            LoggerManager.InitializeWithExplicitLibrary(null, LogLibrary.None);
        }

        [Test]
        public void EncryptPacket_UdpFrame_ClientDecryptRestoresCounterCodeAndPayload()
        {
            byte[] sbox = CreateSBox();
            byte[] expected = CreateDatagram();
            byte[] buf = (byte[]) expected.Clone();

            CreateEncoding(sbox).EncryptPacket(buf, OFFSET, true);

            Assert.That(buf[OFFSET], Is.EqualTo(expected[OFFSET]), "length stays in clear");
            Assert.That(buf[OFFSET + 1], Is.EqualTo(expected[OFFSET + 1]), "length stays in clear");
            Assert.That(buf.AsSpan(OFFSET + 2, PAYLOAD_SIZE + 3).SequenceEqual(expected.AsSpan(OFFSET + 2, PAYLOAD_SIZE + 3)), Is.False);
            Assert.That(buf.AsSpan(0, OFFSET).SequenceEqual(expected.AsSpan(0, OFFSET)), Is.True, "bytes before the frame are untouched");

            ClientDecryptUdpFrame(sbox, buf, OFFSET);

            Assert.That(buf, Is.EqualTo(expected));
        }

        [Test]
        public void ClientDecrypt_UnencryptedUdpFrame_GarblesCodeAndPayload()
        {
            byte[] sbox = CreateSBox();
            byte[] expected = CreateDatagram();
            byte[] buf = (byte[]) expected.Clone();

            ClientDecryptUdpFrame(sbox, buf, OFFSET);

            Assert.That(buf, Is.Not.EqualTo(expected));
        }

        [Test]
        public void RsaKeyTransportAppliesTo_Versions1127And1130WithEncryptionEnabled()
        {
            foreach (GameClient.eClientVersion version in Enum.GetValues<GameClient.eClientVersion>())
            {
                Assert.That(RsaKeyTransport.AppliesTo(false, version), Is.False, $"{version}, crypto disabled");
                Assert.That(RsaKeyTransport.AppliesTo(true, version), Is.EqualTo(version is GameClient.eClientVersion.Version1127 or GameClient.eClientVersion.Version1130), $"{version}, crypto enabled");
            }
        }

        // Only the version reply turns it on.
        [TestCase(GameClient.eClientVersion.Version1127)]
        [TestCase(GameClient.eClientVersion.Version1130)]
        public void Encryption_BeforeVersionReply_IsPlaintext(GameClient.eClientVersion version)
        {
            GameClient client = new(null) { Version = version };

            Assert.That(client.Encryption, Is.SameAs(NoClientEncryption.Instance));
        }

        // Known answers recorded from the encoders before the S-box copy moved to the stack. A key with every byte set
        // (not a permutation) so that 1.68 (S-box used as given) and 1.110 (S-box scheduled from the key) differ.
        [TestCase(false, false, "00153A66890453B56D3837DE548EAFFC551A73906186A0FF3D4A")]
        [TestCase(false, true, "00159B32DB95A359C3F49F81B5259B341B05C37F3315DB295B95")]
        [TestCase(true, false, "0015A1B50F924729E99D3AFC47AC184C500D1F1DC6E11EF63D4A")]
        [TestCase(true, true, "0015D230AF25471791DE9DD15722B999759D50B4EC84AB931B93")]
        public void EncryptPacket_KnownAnswer(bool encoding1110, bool udpPacket, string expected)
        {
            byte[] buf = CreateOutgoingPacket();

            CreateEncoding(encoding1110, CreateKey()).EncryptPacket(buf, 0, udpPacket);

            Assert.That(Convert.ToHexString(buf), Is.EqualTo(expected));
        }

        [TestCase(false, "00139C5CA53E73EC81C4111FAA569B0A9307EC41A60B3805229FA4893EC5E8")]
        [TestCase(true, "00138D16EBA40D2AEBDC773520407538B1FECB4929D4852FA7B9309926AAC3")]
        public void DecryptPacket_KnownAnswer(bool encoding1110, string expected)
        {
            byte[] buf = CreateIncomingPacket();

            CreateEncoding(encoding1110, CreateKey()).DecryptPacket(buf, 0, false);

            Assert.That(Convert.ToHexString(buf), Is.EqualTo(expected));
        }

        // Every packet starts from the same S-box, so the same packet encrypts the same way twice.
        [TestCase(false)]
        [TestCase(true)]
        public void EncryptPacket_LeavesSBoxUnchanged(bool encoding1110)
        {
            IPacketEncoding encoding = CreateEncoding(encoding1110, CreateKey());
            byte[] sbox = (byte[]) encoding.SBox.Clone();
            byte[] first = CreateOutgoingPacket();
            byte[] second = CreateOutgoingPacket();

            encoding.EncryptPacket(first, 0, false);
            encoding.DecryptPacket(CreateIncomingPacket(), 0, false);
            encoding.EncryptPacket(second, 0, false);

            Assert.That(encoding.SBox, Is.EqualTo(sbox));
            Assert.That(second, Is.EqualTo(first));
        }

        // Only the first 256 bytes of the S-box are used.
        [Test]
        public void EncryptPacket_LongerSBox_UsesFirst256Bytes()
        {
            byte[] key = CreateKey();
            byte[] longer = new byte[300];
            key.CopyTo(longer, 0);
            byte[] expected = CreateOutgoingPacket();
            byte[] buf = CreateOutgoingPacket();

            CreateEncoding(false, key).EncryptPacket(expected, 0, false);
            CreateEncoding(false, longer).EncryptPacket(buf, 0, false);

            Assert.That(buf, Is.EqualTo(expected));
        }

        [Test]
        public void EncryptPacket_ShorterSBox_Throws()
        {
            IPacketEncoding encoding = CreateEncoding(false, new byte[100]);

            Assert.That(() => encoding.EncryptPacket(CreateOutgoingPacket(), 0, false), Throws.InstanceOf<SystemException>());
        }

        private static IPacketEncoding CreateEncoding(bool encoding1110, byte[] key)
        {
            IPacketEncoding encoding = encoding1110 ? new PacketEncoding1110() : new PacketEncoding168();
            encoding.SBox = (byte[]) key.Clone();
            encoding.EncryptionState = eEncryptionState.PseudoRC4Encrypted;
            return encoding;
        }

        private static byte[] CreateKey()
        {
            byte[] key = new byte[256];

            for (int i = 0; i < key.Length; i++)
                key[i] = (byte) (i * 31 + 7);

            return key;
        }

        // [size][code][payload], size 21.
        private static byte[] CreateOutgoingPacket()
        {
            byte[] buf = new byte[2 + 3 + 21];
            buf[1] = 21;

            for (int i = 2; i < buf.Length; i++)
                buf[i] = (byte) (i * 13 + 5);

            return buf;
        }

        // [size][10 header bytes][payload], size 19.
        private static byte[] CreateIncomingPacket()
        {
            byte[] buf = new byte[2 + 10 + 19];
            buf[1] = 19;

            for (int i = 2; i < buf.Length; i++)
                buf[i] = (byte) (i * 11 + 3);

            return buf;
        }

        private static PacketEncoding168 CreateEncoding(byte[] sbox)
        {
            return new PacketEncoding168
            {
                SBox = (byte[]) sbox.Clone(),
                EncryptionState = eEncryptionState.PseudoRC4Encrypted
            };
        }

        private static byte[] CreateSBox()
        {
            byte[] sbox = new byte[256];

            for (int i = 0; i < sbox.Length; i++)
                sbox[i] = (byte) i;

            Random random = new(1130);

            for (int i = sbox.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (sbox[i], sbox[j]) = (sbox[j], sbox[i]);
            }

            return sbox;
        }

        // Same layout as `GSUDPPacketOut` after `PacketProcessor` writes the counter: [size][counter][code][payload].
        private static byte[] CreateDatagram()
        {
            byte[] buf = new byte[OFFSET + 5 + PAYLOAD_SIZE];

            for (int i = 0; i < OFFSET; i++)
                buf[i] = 0xEE;

            buf[OFFSET] = PAYLOAD_SIZE >> 8;
            buf[OFFSET + 1] = PAYLOAD_SIZE & 0xFF;
            buf[OFFSET + 2] = COUNTER >> 8;
            buf[OFFSET + 3] = COUNTER & 0xFF;
            buf[OFFSET + 4] = CODE;

            for (int i = 0; i < PAYLOAD_SIZE; i++)
                buf[OFFSET + 5 + i] = (byte) (i * 7 + 1);

            return buf;
        }

        // The client's UDP receive: decrypts `size + 3` bytes (counter, code, payload) after the size,
        // with a fresh copy of the S-box, second half first.
        private static void ClientDecryptUdpFrame(byte[] sbox, byte[] buf, int offset)
        {
            byte[] s = (byte[]) sbox.Clone();
            int start = offset + 2;
            int count = ((buf[offset] << 8) | buf[offset + 1]) + 3;
            int half = count >> 1;
            byte i = 0;
            byte j = 0;

            for (int k = half; k < count; k++)
                DecryptByte(s, buf, start + k, ref i, ref j);

            for (int k = 0; k < half; k++)
                DecryptByte(s, buf, start + k, ref i, ref j);

            static void DecryptByte(byte[] s, byte[] buf, int index, ref byte i, ref byte j)
            {
                i++;
                byte tmp = s[i];
                j += tmp;
                s[i] = s[j];
                s[j] = tmp;
                buf[index] ^= s[(byte) (s[i] + tmp)];
                j += buf[index];
            }
        }
    }
}
