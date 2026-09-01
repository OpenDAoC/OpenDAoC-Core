using DOL.AI.Brain;
using NUnit.Framework;

namespace DOL.GS.Tests.Integration
{
    [TestFixture]
    public class IT_Harness
    {
        private Region _region;

        [SetUp]
        public void SetUp()
        {
            _region = TestWorld.CreateRegion(900);
        }

        [TearDown]
        public void TearDown()
        {
            TestWorld.ClearActiveEncounters();
        }

        [Test]
        public void CreatePlayer_ShouldBeInWorld_AndVisibleToRadiusQueries()
        {
            GamePlayer player = TestWorld.CreatePlayer(_region, 1000, 1000);
            GameNPC npc = TestWorld.AddNpc(new GameNPC { Name = "harness npc", Level = 50 }, _region, 1100, 1000);

            Assert.That(player.ObjectState, Is.EqualTo(GameObject.eObjectState.Active));
            Assert.That(player.InternalID, Is.Not.Null.And.Not.Empty);
            Assert.That(player.IsAlive, Is.True);
            Assert.That(player.Client.IsPlaying, Is.True);
            Assert.That(npc.GetPlayersInRadius(500), Does.Contain(player));
            Assert.That(player.GetNPCsInRadius(500), Does.Contain(npc));
            Assert.That(ClientService.Instance.GetPlayers(), Does.Contain(player));
        }

        [Test]
        public void Tick_ShouldFireTimers_WhenTimeAdvances()
        {
            GameNPC npc = TestWorld.AddNpc(new GameNPC { Name = "timer npc", Level = 50 }, _region, 5000, 5000);
            int fired = 0;
            _ = new ECSGameTimer(npc, _ => { fired++; return 0; }, 1000);

            TestWorld.Advance(900);
            Assert.That(fired, Is.EqualTo(0));
            TestWorld.Advance(200);
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void SetGameHour_ShouldBeReflectedInRegionGameTime()
        {
            TestWorld.SetGameHour(21);
            Assert.That(_region.GameTime / 1000 / 60 / 60, Is.EqualTo(21));
        }

        [Test]
        public void Aggro_ShouldSnapshotEncounter_WithNearbyPlayersOnRoster()
        {
            GamePlayer puller = TestWorld.CreatePlayer(_region, 10000, 10000);
            GamePlayer bystander = TestWorld.CreatePlayer(_region, 10200, 10000);
            StandardMobBrain brain = new();
            GameNPC npc = new() { Name = "encounter npc", Level = 50 };
            npc.SetOwnBrain(brain);
            brain.RaidEncounter = new(brain);
            TestWorld.AddNpc(npc, _region, 10100, 10000);

            brain.AddToAggroList(puller, 100);
            brain.Think();

            Assert.That(brain.RaidEncounter.Active, Is.True);
            Assert.That(brain.RaidEncounter.Size, Is.EqualTo(2));
            Assert.That(brain.RaidEncounter.IsOnRoster(puller), Is.True);
            Assert.That(brain.RaidEncounter.IsOnRoster(bystander), Is.True);

            GamePlayer latecomer = TestWorld.CreatePlayer(_region, 10300, 10000);
            Assert.That(brain.RaidEncounter.IsOnRoster(latecomer), Is.False);
            Assert.That(GameServer.ServerRules.IsAllowedToAttack(latecomer, npc, true), Is.False);
            Assert.That(GameServer.ServerRules.IsAllowedToHelp(latecomer, puller, true), Is.False);
            Assert.That(GameServer.ServerRules.IsAllowedToHelp(bystander, puller, true), Is.True);
            Assert.That(TestWorld.Messages(latecomer), Does.Not.Contain("You are not part of this encounter!"));
            GameServer.ServerRules.IsAllowedToAttack(latecomer, npc, false);
            Assert.That(TestWorld.Messages(latecomer), Does.Contain("You are not part of this encounter!"));
        }
    }
}
