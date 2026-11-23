using DOL.GS.Keeps;
using DOL.Logging;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_HookPointInventory
    {
        private HookPointItem _guard;

        [SetUp]
        public void SetUp()
        {
            LoggerManager.InitializeWithExplicitLibrary(null, LogLibrary.None);
            _guard = new HookPointItem("Melee Guard", 67, 2624, "DOL.GS.Keeps.GuardFighter", 0);
            HookPointInventory.RedHPInventory.AddFirstFreeSlot(_guard);
        }

        [TearDown]
        public void TearDown()
        {
            HookPointInventory.RedHPInventory.GetAllItems().Remove(_guard);
        }

        [TestCase(0x01, "Red")]
        [TestCase(0x20, "Red")]
        [TestCase(0x21, "Blue")]
        [TestCase(0x40, "Blue")]
        [TestCase(0x41, "LightGreen")]
        [TestCase(0x61, "Green")]
        [TestCase(0x81, "Yellow")]
        public void GetInventory_ByHookPointId(int hookPointId, string expected)
        {
            HookPointInventory inventory = expected switch
            {
                "Red" => HookPointInventory.RedHPInventory,
                "Blue" => HookPointInventory.BlueHPInventory,
                "LightGreen" => HookPointInventory.LightGreenHPInventory,
                "Green" => HookPointInventory.GreenHPInventory,
                _ => HookPointInventory.YellowHPInventory
            };

            Assert.That(HookPointInventory.GetInventory(hookPointId), Is.SameAs(inventory));
        }

        [Test]
        public void HasItems_OnlyWhereSomethingIsSold()
        {
            Assert.That(HookPointInventory.HasItems(0x01), Is.True);  // guard hookpoint, one guard for sale
            Assert.That(HookPointInventory.HasItems(0x21), Is.False); // NPC hookpoint, nothing for sale
        }
    }
}
