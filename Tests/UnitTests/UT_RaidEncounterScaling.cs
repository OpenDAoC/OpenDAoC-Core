using DOL.AI.Brain;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_RaidEncounterScaling
    {
        [SetUp]
        public void SetUp()
        {
            ServerProperties.Properties.RAID_SCALING_ENABLED = true;
            ServerProperties.Properties.RAID_SCALING_BASELINE_SIZE = 8;
            ServerProperties.Properties.RAID_SCALING_MAX_SIZE = 80;
            ServerProperties.Properties.RAID_SCALING_HP_PER_EXTRA_PLAYER = 0.10;
            ServerProperties.Properties.RAID_SCALING_ACTIVE_FRACTION = 0.75;
            ServerProperties.Properties.RAID_SCALING_AF_IDLE_WEIGHT = 0.75;
            ServerProperties.Properties.RAID_SCALING_ACTIVITY_WINDOW_SECONDS = 30;
            ServerProperties.Properties.RAID_SCALING_ITEM_SHARE_SIZE = 5;
            ServerProperties.Properties.RAID_SCALING_QUIT_GRACE_MINUTES = 20;
        }

        static RaidEncounter NewEncounter()
        {
            return new RaidEncounter(new StandardMobBrain());
        }

        [Test]
        public void ScaleSize_ShouldClampToBaseline_WhenSizeBelowBaseline()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 3;
            Assert.That(encounter.ScaleSize, Is.EqualTo(8));
        }

        [Test]
        public void ScaleSize_ShouldReturnSize_WhenSizeWithinRange()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 20;
            Assert.That(encounter.ScaleSize, Is.EqualTo(20));
        }

        [Test]
        public void ScaleSize_ShouldClampToMax_WhenSizeAboveMax()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 200;
            Assert.That(encounter.ScaleSize, Is.EqualTo(80));
        }

        [Test]
        public void HpMultiplier_ShouldBeOne_WhenSizeEqualsBaseline()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 8;
            Assert.That(encounter.HpMultiplier, Is.EqualTo(1.0).Within(1e-9));
        }

        [Test]
        public void HpMultiplier_ShouldBeOne_WhenSizeIsZero()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 0;
            Assert.That(encounter.HpMultiplier, Is.EqualTo(1.0).Within(1e-9));
        }

        [Test]
        public void HpMultiplier_ShouldScaleWithSize_WhenSizeAboveBaseline()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 18;
            Assert.That(encounter.HpMultiplier, Is.EqualTo(2.0).Within(1e-9));
        }

        [Test]
        public void HpMultiplier_ShouldClampToMaxSize_WhenSizeAboveMax()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 200;
            Assert.That(encounter.HpMultiplier, Is.EqualTo(8.2).Within(1e-9));
        }

        [Test]
        public void BonusLootRolls_ShouldBeZero_WhenSizeEqualsBaseline()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 8;
            Assert.That(encounter.BonusLootRolls, Is.EqualTo(0));
        }

        [Test]
        public void BonusLootRolls_ShouldBeZero_WhenSizeBelowNextShare()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 12;
            Assert.That(encounter.BonusLootRolls, Is.EqualTo(0));
        }

        [Test]
        public void BonusLootRolls_ShouldBeOne_WhenSizeReachesNextShare()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 13;
            Assert.That(encounter.BonusLootRolls, Is.EqualTo(1));
        }

        [Test]
        public void BonusLootRolls_ShouldBeThree_WhenSizeReachesThirdShare()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 23;
            Assert.That(encounter.BonusLootRolls, Is.EqualTo(3));
        }

        [Test]
        public void BonusLootRolls_ShouldClampToMaxSize_WhenSizeAboveMax()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 200;
            Assert.That(encounter.BonusLootRolls, Is.EqualTo(14));
        }

        [Test]
        public void BonusLootRolls_ShouldBeZero_WhenItemShareSizeIsZero()
        {
            ServerProperties.Properties.RAID_SCALING_ITEM_SHARE_SIZE = 0;
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 40;
            Assert.That(encounter.BonusLootRolls, Is.EqualTo(0));
        }

        [Test]
        public void ScaleUnitCount_ShouldUseBaselineSize_WhenEncounterInactive()
        {
            RaidEncounter encounter = NewEncounter();
            Assert.That(encounter.ScaleUnitCount(4, 15), Is.EqualTo(2));
        }

        [Test]
        public void ScaleUnitCount_ShouldIgnoreSize_WhenEncounterInactive()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 40;
            Assert.That(encounter.ScaleUnitCount(4, 15), Is.EqualTo(2));
        }

        [Test]
        public void ScaleUnitCount_ShouldReturnMaxCount_WhenPlayersPerUnitIsZero()
        {
            RaidEncounter encounter = NewEncounter();
            Assert.That(encounter.ScaleUnitCount(0, 15), Is.EqualTo(15));
        }

        [Test]
        public void ScaleUnitCount_ShouldNeverGoBelowOne_WhenPlayersPerUnitIsLarge()
        {
            RaidEncounter encounter = NewEncounter();
            Assert.That(encounter.ScaleUnitCount(100, 15), Is.EqualTo(1));
        }

        [Test]
        public void CalculateArmorFactorScalingFactor_ShouldReturnDefault_WhenActiveMeetsExpected()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 8;
            Assert.That(encounter.CalculateArmorFactorScalingFactor(1.6, 6), Is.EqualTo(1.6).Within(1e-9));
        }

        [Test]
        public void CalculateArmorFactorScalingFactor_ShouldReturnDefault_WhenActiveExceedsExpected()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 8;
            Assert.That(encounter.CalculateArmorFactorScalingFactor(1.6, 10), Is.EqualTo(1.6).Within(1e-9));
        }

        [Test]
        public void CalculateArmorFactorScalingFactor_ShouldPeakAtFullDeficit_WhenNoActiveAttackers()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 8;
            Assert.That(encounter.CalculateArmorFactorScalingFactor(1.6, 0), Is.EqualTo(2.8).Within(1e-9));
        }

        [Test]
        public void CalculateArmorFactorScalingFactor_ShouldScaleWithPartialDeficit_WhenSomeActiveAttackers()
        {
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 8;
            Assert.That(encounter.CalculateArmorFactorScalingFactor(1.6, 3), Is.EqualTo(2.2).Within(1e-9));
        }

        [Test]
        public void CalculateArmorFactorScalingFactor_ShouldReturnDefault_WhenActiveFractionIsZero()
        {
            ServerProperties.Properties.RAID_SCALING_ACTIVE_FRACTION = 0;
            RaidEncounter encounter = NewEncounter();
            encounter.Size = 8;
            Assert.That(encounter.CalculateArmorFactorScalingFactor(1.6, 0), Is.EqualTo(1.6).Within(1e-9));
        }

        [Test]
        public void HasActiveEncounters_ShouldBeFalse_WhenNothingHasSnapshotted()
        {
            Assert.That(RaidEncounter.HasActiveEncounters, Is.False);
        }

        [Test]
        public void GetActiveEncounters_ShouldBeEmpty_WhenNothingHasSnapshotted()
        {
            Assert.That(RaidEncounter.GetActiveEncounters(), Is.Empty);
        }
    }
}
