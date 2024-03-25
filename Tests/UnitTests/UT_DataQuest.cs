using System.Collections.Concurrent;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS;
using DOL.GS.PlayerClass;
using DOL.GS.Quests;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace DOL.Tests.Unit.Gameserver
{
    [TestFixture]
    class UT_DataQuest
    {
        FakeServer fakeServer = new FakeServer();

        [SetUp]
        public void Init()
        {
            GameServer.LoadTestDouble(fakeServer);
        }

        #region Accessor
        [Test]
        public void MoneyReward_Init_Zero()
        {
            var dataQuest = NewDataQuest();

            var actual = dataQuest.MoneyReward();
            var expected = 0;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void ExperiencePercent_Init_Zero()
        {
            var dataQuest = NewDataQuest();
            var player = NewFakePlayer();
            player.Level = 1;

            var actual = dataQuest.ExperiencePercent(player);
            var expected = 0;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void MoneyReward_RewardMoneyIs2InAString_2()
        {
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.RewardMoney = "2";
            var dataQuest = NewDataQuest(dbDataQuest);

            var actual = dataQuest.MoneyReward();
            var expected = 2;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void ExperiencePercent_PlayerIsLevelOneAndRewardXPIs2InAString_4()
        {
            var player = NewFakePlayer();
            player.Level = 1;
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.RewardXP = "2";
            var dataQuest = NewDataQuest(dbDataQuest);

            var actual = dataQuest.ExperiencePercent(player);
            var expected = 4;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void RewardMoney_SetTo2Pipe3Pipe4OnStep2_3()
        {
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.RewardMoney = "2|3|4";
            var dataQuest = NewDataQuest(dbDataQuest);

            dataQuest.Step = 2;

            var actual = dataQuest.SpyRewardMoney;
            var expected = 3;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void RewardXP_SetTo2Pipe3Pipe4OnStepOne_3()
        {
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.RewardXP = "2|3|4";
            var dataQuest = NewDataQuest(dbDataQuest);

            dataQuest.Step = 2;

            var actual = dataQuest.SpyRewardXP;
            var expected = 3;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void RewardCLXP_SetTo4_4()
        {
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.RewardCLXP = "4";
            var dataQuest = NewDataQuest(dbDataQuest);

            dataQuest.Step = 1;

            var actual = dataQuest.SpyRewardCLXP;
            var expected = 4;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void RewardRP_SetTo5_5()
        {
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.RewardRP = "5";
            var dataQuest = NewDataQuest(dbDataQuest);

            dataQuest.Step = 1;

            var actual = dataQuest.SpyRewardRP;
            var expected = 5;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void RewardBP_SetTo6_6()
        {
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.RewardBP = "6";
            var dataQuest = NewDataQuest(dbDataQuest);

            dataQuest.Step = 1;

            var actual = dataQuest.SpyRewardBP;
            var expected = 6;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void StepType_Init_StepTypeUnknown()
        {
            var dataQuest = NewDataQuest();

            dataQuest.Step = 1;

            var actual = dataQuest.StepType;
            var expected = DataQuest.eStepType.Unknown;
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Step_Init_Zero()
        {
            var dataQuest = NewDataQuest();

            var actual = dataQuest.Step;
            var expected = 0;
            ClassicAssert.AreEqual(expected, actual);
        }
        #endregion Accessor

        #region CheckQuestQualification
        [Test]
        public void CheckQuestQualification_Default_True()
        {
            var player = NewFakePlayer();
            var dataQuest = NewDataQuest();

            bool isQualified = dataQuest.CheckQuestQualification(player);

            ClassicAssert.IsTrue(isQualified);
        }

        [Test]
        public void CheckQuestQualification_PlayerIsLevelOneAndMinLevelIsTwo_False()
        {
            var player = NewFakePlayer();
            player.Level = 1;
            var dataQuest = NewDataQuest();
            dataQuest.SpyDBDataQuest.MinLevel = 2;

            bool isQualified = dataQuest.CheckQuestQualification(player);

            ClassicAssert.IsFalse(isQualified);
        }

        [Test]
        public void CheckQuestQualification_PlayerIsPaladinAndAllowedClassIsCleric_False()
        {
            var player = NewFakePlayer();
            player.fakeCharacterClass = new ClassPaladin();
            var dbDataQuest = new DbDataQuest();
            var clericClassID = (int)eCharacterClass.Cleric;
            dbDataQuest.AllowedClasses = clericClassID.ToString();
            var dataQuest = NewDataQuest(dbDataQuest);

            bool isQualified = dataQuest.CheckQuestQualification(player);

            ClassicAssert.IsFalse(isQualified);
        }

        [Test]
        public void CheckQuestQualification_PlayerDidQuestThreeTimesAndMaxCountIsThree_False()
        {
            var player = NewFakePlayer();
            var dataQuest = NewDataQuest();
            dataQuest.SpyDBDataQuest.MaxCount = 3;
            dataQuest.SpyDBDataQuest.StartType = (byte)DataQuest.eStartType.Collection;
            dataQuest.SpyCharQuest.Count = 3;
            var fakeDatabase = new FakeDatabase();
            fakeServer.SetDatabase(fakeDatabase);
            fakeDatabase.SelectObjectReturns = new List<DataObject>() { dataQuest.SpyCharQuest };

            bool isQualified = dataQuest.CheckQuestQualification(player);

            ClassicAssert.IsFalse(isQualified);
        }

        [Test]
        public void CheckQuestQualification_PlayerStartedQuestAlready_False()
        {
            var player = NewFakePlayer();
            var dataQuest = NewDataQuest();

            player.fakeQuestList.TryAdd(dataQuest, (byte) player.fakeQuestList.Count);
            bool isQualified = dataQuest.CheckQuestQualification(player);

            ClassicAssert.IsFalse(isQualified);
        }

        [Test]
        public void CheckQuestQualification_PlayerFinishedQuestAlreadyAndMaxCountReached_False()
        {
            var player = NewFakePlayer();
            var dataQuest = NewDataQuest();
            dataQuest.SpyDBDataQuest.MaxCount = 1;
            dataQuest.SpyCharQuest.Count = 1;

            player.AddFinishedQuest(dataQuest);
            bool isQualified = dataQuest.CheckQuestQualification(player);

            ClassicAssert.IsFalse(isQualified);
        }

        [Test]
        public void CheckQuestQualification_DependentQuestNotDone_False()
        {
            var player = NewFakePlayer();
            var dbDataQuest = new DbDataQuest();
            dbDataQuest.QuestDependency = "SomeQuestName";
            var dataQuest = NewDataQuest(dbDataQuest);

            bool isQualified = dataQuest.CheckQuestQualification(player);

            ClassicAssert.IsFalse(isQualified);
        }
#endregion CheckQuestQualification

        [Test]
        public void IsDoingQuest_SameQuest_True()
        {
            var dataQuest = NewDataQuest();
            dataQuest.Step = 1;

            var actual = dataQuest.IsDoingQuest(dataQuest);

            ClassicAssert.IsTrue(actual);
        }

        [Test]
        public void IsDoingQuest_SameQuestAndStepIsZero_False()
        {
            var dataQuest = NewDataQuest();
            dataQuest.Step = 0;

            var actual = dataQuest.IsDoingQuest(dataQuest);

            ClassicAssert.IsFalse(actual);
        }

        [Test]
        public void IsDoingQuest_QuestWithDifferentID_False()
        {
            var dataQuest = NewDataQuest();
            dataQuest.SpyDBDataQuest.ID = 1;
            var otherDataQuest = NewDataQuest();
            otherDataQuest.SpyDBDataQuest.ID = 2;
            dataQuest.Step = 1;

            var actual = dataQuest.IsDoingQuest(otherDataQuest);

            ClassicAssert.IsFalse(actual);
        }

        [Test]
        public void TargetName_Default_EmptyString()
        {
            var dataQuest = NewDataQuest();
            dataQuest.Step = 1;

            var actual = dataQuest.TargetName;
            var expected = "";
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TargetName_SomeName_SomeName()
        {
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.TargetName = "SomeName";
            var dataQuest = NewDataQuest(dbDataQuest);
            dataQuest.Step = 1;

            var actual = dataQuest.TargetName;
            var expected = "SomeName";
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TargetName_FooZeroAndBarZeroOnStep2_Bar()
        {
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.TargetName = "Foo;0|Bar;0";
            var dataQuest = NewDataQuest(dbDataQuest);
            dataQuest.Step = 2;

            var actual = dataQuest.TargetName;
            var expected = "Bar";
            ClassicAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TargetText_BarPipeBazOnStep2_Baz()
        {
            var dbDataQuest = NewDBDataQuest();
            dbDataQuest.TargetText = "Bar|Baz";
            var dataQuest = NewDataQuest(dbDataQuest);

            dataQuest.Step = 2;

            var actual = dataQuest.SpyTargetText;
            var expected = "Baz";
            ClassicAssert.AreEqual(expected, actual);
        }

        private FakeQuestPlayer NewFakePlayer() => new FakeQuestPlayer();
        private DataQuestSpy NewDataQuest() => NewDataQuest(NewDBDataQuest());
        private DataQuestSpy NewDataQuest(DbDataQuest dbDataQuest) => new DataQuestSpy(null, dbDataQuest, new DbCharacterXDataQuest());
        private DbDataQuest NewDBDataQuest() => new DbDataQuest();

        private class FakeQuestPlayer : FakePlayer
        {
            public ConcurrentDictionary<AbstractQuest, byte> fakeQuestList = new();

            public override ConcurrentDictionary<AbstractQuest, byte> QuestList => fakeQuestList;
        }

        private class DataQuestSpy : DataQuest
        {
            public long SpyRewardMoney => RewardMoney;
            public long SpyRewardXP => RewardXP;
            public long SpyRewardCLXP => RewardCLXP;
            public long SpyRewardRP => RewardRP;
            public long SpyRewardBP => RewardBP;
            public string SpyTargetText => TargetText;

            public DbDataQuest SpyDBDataQuest => m_dataQuest;
            public DbCharacterXDataQuest SpyCharQuest => m_charQuest;

            public DataQuestSpy(GamePlayer player, DbDataQuest dbDataQuest, DbCharacterXDataQuest charXDataQuest) : base(player, dbDataQuest, charXDataQuest) { }
        }
    }
}
