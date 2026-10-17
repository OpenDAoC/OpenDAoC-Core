using DOL.Database;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.Language;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class UT_BountyRentCommandHandler
    {
        [SetUp]
        public void SetUp()
        {
            LanguageMgr.LoadTestDouble(new TestLanguageMgr());
        }

        [TearDown]
        public void TearDown()
        {
            LanguageMgr.LoadTestDouble(new LanguageMgr());
        }

        [Test]
        public void InvalidAmount_IsRejectedBeforeAccessingPaymentState(
            [Values("personal", "guild")] string paymentType,
            [Values("-1", "-100", "-9223372036854775808", "0", "abc", "", "9223372036854775808", "-9223372036854775809")] string amount)
        {
            // No player or house is attached: invalid input must return before
            // accessing balances, rent permissions, or database persistence.
            var client = new GameClient(null) { Account = new DbAccount { Language = "EN" } };
            var packets = new RecordingPacketLib(client);
            client.Out = packets;

            new BountyRentCommandHandler().OnCommand(client, new[] { "&bountyrent", paymentType, amount });

            Assert.That(packets.MessageCount, Is.EqualTo(1));
            Assert.That(packets.LastMessage, Is.EqualTo("EN CorrectFormat"));
        }

        private sealed class RecordingPacketLib : PacketLib168
        {
            public int MessageCount { get; private set; }
            public string LastMessage { get; private set; }

            public RecordingPacketLib(GameClient client) : base(client) { }

            public override void SendMessage(string message, eChatType type, eChatLoc location)
            {
                MessageCount++;
                LastMessage = message;
            }
        }

        private sealed class TestLanguageMgr : LanguageMgr
        {
            protected override bool TryGetRawTranslation(out DbLanguageSystem translationObject, string language, string translationId)
            {
                translationObject = null;
                return false;
            }
        }
    }
}
