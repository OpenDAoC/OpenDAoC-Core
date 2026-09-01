using System.Collections.Generic;
using DOL.AI;
using DOL.AI.Brain;
using DOL.GS.ServerProperties;
using NUnit.Framework;

namespace DOL.GS.Tests.Integration
{
    [TestFixture]
    public class IT_DragonBrain
    {
        private const ushort REGION_ID = 910;
        private const string MESSENGER_NAME = "test dragon messenger";
        private const string ADD_NAME = "test dragon add";

        private static readonly (int X, int Y) PULL_SITE = (8000, 8000);
        private static readonly (int X, int Y) GLARE_SITE = (8000, 23000);
        private static readonly (int X, int Y) BREATH_SITE = (8000, 38000);
        private static readonly (int X, int Y) STUN_SITE = (8000, 53000);
        private static readonly (int X, int Y) THROW_SITE = (23000, 8000);
        private static readonly (int X, int Y) MESSENGER_SITE = (23000, 23000);
        private static readonly (int X, int Y) LEASH_SITE = (23000, 53000);
        private static readonly (int X, int Y) ROAM_SITE = (38000, 8000);
        private static readonly (int X, int Y) SCALING_SITE = (38000, 23000);

        public static readonly DragonConfig Config = new()
        {
            NpcTemplateId = 0,
            FactionId = 0,
            MeleeDamageType = eDamageType.Crush,
            SpawnPoint = new Point3D(0, 0, 0),
            SpawnHeading = 0,
            SpellKeyPrefix = "ItTestDragon",

            SpellDamageType = eDamageType.Heat,
            ResistDebuffSpellType = eSpellType.HeatResistDebuff,
            ResistDebuffDescription = "Decreases a target's given resistance to Heat magic by 50%",
            GlareSpellName = "Test Dragon's Glare",
            BreathSpellName = "Test Dragon's Breath",
            SpellClientEffect = 5700,
            StunClientEffect = 5703,
            ResistDebuffClientEffect = 777,
            BreathClientEffect = 5700,
            RoamGlareSpellId = 990001,
            GlareSpellId = 990002,
            BreathSpellId = 990003,
            StunSpellId = 990004,
            ResistDebuffSpellId = 990005,

            GlareTexts = ["{0} shouts, 'Your flesh will make a splendid meal, {1}.'"],
            BreathTexts = ["You feel a rush of air flow past you as {0} inhales deeply!"],
            GlareTelegraphText = "{0} stares at {1} and prepares a massive attack.",
            BreathTelegraphText = "{0} draws in a deep breath, fixing its gaze upon {1}!",
            BreathAnchorText = "{0} fixes its deadly gaze upon YOU!",
            MessengerWaveTelegraphText = "{0} roars a summons, and the ground trembles near its lair!",
            RoamStartText = "The skies darken as {0} takes wing.",
            StunTelegraphText = "{0} looks mindfully around.",
            ThrowText = "{0} begins flapping its wings violently.",
            EnemyKilledTaunt = "{0} roars in triumph as another {1} falls.",
            DeathAnnounces = ["{0} staggers and topples to the ground."],

            RoamPath =
            [
                (38500, 8000, 0),
                (39000, 8000, 0),
                (39500, 8000, 0)
            ],
            ThrowDestinations =
            [
                new() { X = 53000, Y = 53000, Z = 0, Heading = 0 },
                new() { X = 53400, Y = 53000, Z = 0, Heading = 1024 },
                new() { X = 53000, Y = 53400, Z = 0, Heading = 2048 }
            ],
            MessengerSpawnPoint = new Point3D(23200, 23000, 0),
            MessengerPaths =
            [
                [(23600, 23000, 0), (24000, 23000, 0)],
                [(23200, 23400, 0), (23200, 23800, 0)],
                [(22800, 23000, 0), (22400, 23000, 0)],
                [(23200, 22600, 0), (23200, 22200, 0)]
            ],
            AddReturnPaths =
            [
                [(24000, 23000, 0), (23200, 23000, 0)],
                [(23200, 23800, 0), (23200, 23000, 0)],
                [(22400, 23000, 0), (23200, 23000, 0)],
                [(23200, 22200, 0), (23200, 23000, 0)]
            ],

            MessengerName = MESSENGER_NAME,
            MessengerModel = 2386,
            MessengerSize = 80,
            AddVariants =
            [
                new() { Name = ADD_NAME, Model = 2386, MinSize = 50, MaxSize = 50 }
            ],

            CreateMessenger = pathIndex => new ItTestMessenger { PathIndex = pathIndex },
            CreateAdd = pathIndex => new ItTestAdd { PathIndex = pathIndex }
        };

        private Region _region;
        private readonly List<GamePlayer> _players = new();
        private readonly List<GameNPC> _npcs = new();

        [SetUp]
        public void SetUp()
        {
            _region = TestWorld.CreateRegion(REGION_ID);
        }

        [TearDown]
        public void TearDown()
        {
            TestWorld.ClearActiveEncounters();

            foreach (GameNPC npc in _npcs)
            {
                npc.Brain?.Stop();
                npc.RemoveFromWorld();
            }

            foreach (GameNPC minion in WorldMgr.GetNPCsByNameFromRegion(MESSENGER_NAME, REGION_ID, eRealm.None))
                minion.RemoveFromWorld();

            foreach (GameNPC minion in WorldMgr.GetNPCsByNameFromRegion(ADD_NAME, REGION_ID, eRealm.None))
                minion.RemoveFromWorld();

            foreach (GamePlayer player in _players)
                TestWorld.RemovePlayer(player);

            _npcs.Clear();
            _players.Clear();
            TestWorld.SetGameHour(12);
        }

        [Test]
        public void Think_ShouldSnapshotEncounter_WhenPulled()
        {
            GamePlayer puller = NewPlayer(PULL_SITE.X + 1000, PULL_SITE.Y);
            GamePlayer bystander = NewPlayer(PULL_SITE.X + 1400, PULL_SITE.Y);
            DragonBrain brain = NewDragon(PULL_SITE);

            brain.AddToAggroList(puller, 100);
            brain.Think();

            Assert.That(brain.RaidEncounter.Active, Is.True);
            Assert.That(brain.RaidEncounter.Size, Is.EqualTo(2));
            Assert.That(brain.RaidEncounter.IsOnRoster(puller), Is.True);
            Assert.That(brain.RaidEncounter.IsOnRoster(bystander), Is.True);

            GamePlayer latecomer = NewPlayer(PULL_SITE.X + 1800, PULL_SITE.Y);

            Assert.That(brain.RaidEncounter.IsOnRoster(latecomer), Is.False);
            Assert.That(brain.RaidEncounter.Size, Is.EqualTo(2));
        }

        [Test]
        public void Think_ShouldTelegraphThenCastGlare_WhenVictimIsInRange()
        {
            GamePlayer victim = NewPlayer(GLARE_SITE.X + 1200, GLARE_SITE.Y);
            DragonBrain brain = NewDragon(GLARE_SITE);

            brain.AddToAggroList(victim, 100);
            brain.Think();
            brain.Think();

            Assert.That(TestWorld.Messages(victim), Does.Contain(string.Format(Config.GlareTelegraphText, brain.Body.Name, victim.Name)));

            TestWorld.Advance(6100);

            Assert.That(TestWorld.Messages(victim), Does.Contain(string.Format(Config.GlareTexts[0], brain.Body.Name, victim.CharacterClass.Name)));
        }

        [Test]
        public void Think_ShouldStartBreathOncePerBand_WhenHealthDropsIntoANewBand()
        {
            GamePlayer anchor = NewPlayer(BREATH_SITE.X + 1200, BREATH_SITE.Y);
            DragonBrain brain = NewDragon(BREATH_SITE);
            GameNPC body = brain.Body;

            brain.AddToAggroList(anchor, 100);
            brain.Think();
            brain.Think();

            Assert.That(brain._breathIndex, Is.EqualTo(0));

            body.Health = (int) (body.MaxHealth * 0.9);
            brain.Think();

            Assert.That(brain._breathIndex, Is.EqualTo(1));
            Assert.That(TestWorld.Messages(anchor), Does.Contain(string.Format(Config.BreathAnchorText, body.Name)));
            Assert.That(TestWorld.Messages(anchor), Does.Contain(string.Format(Config.BreathTelegraphText, body.Name, anchor.Name)));

            body.Health = (int) (body.MaxHealth * 0.9);
            brain.Think();

            Assert.That(brain._breathIndex, Is.EqualTo(1));

            TestWorld.Advance(8000);
            body.Health = (int) (body.MaxHealth * 0.8);
            brain.Think();

            Assert.That(brain._breathIndex, Is.EqualTo(2));
        }

        [Test]
        public void Think_ShouldTelegraphStun_OnlyWhenHealthIsAtOrBelowThreeQuarters()
        {
            GamePlayer target = NewPlayer(STUN_SITE.X + 1200, STUN_SITE.Y);
            DragonBrain brain = NewDragon(STUN_SITE);
            GameNPC body = brain.Body;
            brain._breathIndex = 9;

            brain.AddToAggroList(target, 100);
            brain.Think();
            brain.Think();

            string telegraph = string.Format(Config.StunTelegraphText, body.Name);

            Assert.That(TestWorld.Messages(target), Does.Not.Contain(telegraph));

            body.Health = (int) (body.MaxHealth * 0.75);
            brain.Think();

            Assert.That(TestWorld.Messages(target), Does.Contain(telegraph));
        }

        [Test]
        public void Think_ShouldThrowVictimsToADestination_AndDropThemFromTheAggroList()
        {
            GamePlayer main = NewPlayer(THROW_SITE.X + 1000, THROW_SITE.Y);
            List<GamePlayer> others =
            [
                NewPlayer(THROW_SITE.X + 1200, THROW_SITE.Y),
                NewPlayer(THROW_SITE.X + 1400, THROW_SITE.Y),
                NewPlayer(THROW_SITE.X + 1600, THROW_SITE.Y),
                NewPlayer(THROW_SITE.X + 1800, THROW_SITE.Y)
            ];
            DragonBrain brain = NewDragon(THROW_SITE);
            brain._breathIndex = 9;
            brain.AddToAggroList(main, 100000);

            foreach (GamePlayer other in others)
                brain.AddToAggroList(other, 100);

            brain.Think();

            Assert.That(brain.Body.TargetObject, Is.EqualTo(main));

            brain.Think();

            List<GamePlayer> thrown = new();

            foreach (GamePlayer other in others)
            {
                foreach (DragonConfig.ThrowDestination destination in Config.ThrowDestinations)
                {
                    if (other.X == destination.X && other.Y == destination.Y && other.Z == destination.Z)
                    {
                        thrown.Add(other);
                        break;
                    }
                }
            }

            Assert.That(thrown.Count, Is.InRange(2, others.Count));

            foreach (GamePlayer victim in thrown)
            {
                Assert.That(TestWorld.Messages(victim), Does.Contain(string.Format(Config.ThrowText, brain.Body.Name)));
                Assert.That(brain.IsInAggroList(victim), Is.False);
            }

            Assert.That(main.X, Is.EqualTo(THROW_SITE.X + 1000));
            Assert.That(brain.IsInAggroList(main), Is.True);
        }

        [Test]
        public void Think_ShouldSpawnABaselineMessengerWave_WhenHealthIsAtOrBelowHalf()
        {
            GamePlayer player = NewPlayer(MESSENGER_SITE.X + 1200, MESSENGER_SITE.Y);
            DragonBrain brain = NewDragon(MESSENGER_SITE);
            GameNPC body = brain.Body;
            brain._breathIndex = 9;

            brain.AddToAggroList(player, 100);
            brain.Think();
            body.Health = body.MaxHealth / 2;
            brain.Think();

            Assert.That(TestWorld.Messages(player), Does.Contain(string.Format(Config.MessengerWaveTelegraphText, body.Name)));

            TestWorld.Advance(3100);

            Assert.That(brain.RaidEncounter.ScaleUnitCount(Config.PlayersPerMessenger, Config.MessengerWaveMaxCount), Is.EqualTo(Properties.RAID_SCALING_BASELINE_SIZE / Config.PlayersPerMessenger));
            Assert.That(MessengerCount(), Is.EqualTo(Properties.RAID_SCALING_BASELINE_SIZE / Config.PlayersPerMessenger));

            brain.RaidEncounter.Clear();

            Assert.That(MessengerCount(), Is.EqualTo(0));
        }

        [Test]
        public void Think_ShouldScaleTheMessengerWave_WhenTheRosterIsLargerThanTheBaseline()
        {
            GamePlayer player = NewPlayer(MESSENGER_SITE.X + 1200, MESSENGER_SITE.Y);
            DragonBrain brain = NewDragon(MESSENGER_SITE);
            GameNPC body = brain.Body;
            brain._breathIndex = 9;

            brain.AddToAggroList(player, 100);
            brain.Think();
            brain.RaidEncounter.Size = 40;
            body.Health = body.MaxHealth / 2;
            brain.Think();
            TestWorld.Advance(3100);

            int expected = brain.RaidEncounter.ScaleUnitCount(Config.PlayersPerMessenger, Config.MessengerWaveMaxCount);

            Assert.That(expected, Is.EqualTo(10));
            Assert.That(MessengerCount(), Is.EqualTo(expected));
        }

        [Test]
        public void Think_ShouldResetTheEncounter_WhenTheAggroListIsEmptied()
        {
            GamePlayer player = NewPlayer(MESSENGER_SITE.X + 1200, MESSENGER_SITE.Y);
            DragonBrain brain = NewDragon(MESSENGER_SITE);
            GameNPC body = brain.Body;

            brain.AddToAggroList(player, 100);
            brain.Think();
            body.Health = body.MaxHealth / 2;
            brain.Think();
            TestWorld.Advance(3100);

            Assert.That(brain._encounterStarted, Is.True);
            Assert.That(brain._breathIndex, Is.EqualTo(1));
            Assert.That(MessengerCount(), Is.GreaterThan(0));
            Assert.That(body.Health, Is.LessThan(body.MaxHealth));

            brain.ClearAggroList();
            brain.Think();

            Assert.That(brain._encounterStarted, Is.False);
            Assert.That(brain._breathIndex, Is.EqualTo(0));
            Assert.That(MessengerCount(), Is.EqualTo(0));
            Assert.That(body.Health, Is.EqualTo(body.MaxHealth));
        }

        [Test]
        public void Think_ShouldLeashBackToSpawn_WhenDraggedOutOfTetherRange()
        {
            GamePlayer player = NewPlayer(LEASH_SITE.X + 3000, LEASH_SITE.Y);
            DragonBrain brain = NewDragon(LEASH_SITE, 500);
            GameNPC body = brain.Body;

            brain.AddToAggroList(player, 100);
            brain.Think();

            Assert.That(body.TargetObject, Is.EqualTo(player));

            body.Health = body.MaxHealth / 2;
            body.MoveInRegion(body.CurrentRegionID, LEASH_SITE.X + 5000, LEASH_SITE.Y, 0, body.Heading);
            brain.Think();

            Assert.That(body.X, Is.EqualTo(LEASH_SITE.X));
            Assert.That(body.Y, Is.EqualTo(LEASH_SITE.Y));
            Assert.That(body.Health, Is.EqualTo(body.MaxHealth));
            Assert.That(brain.HasAggro, Is.False);

            body.MoveInRegion(body.CurrentRegionID, LEASH_SITE.X + 5000, LEASH_SITE.Y, 0, body.Heading);
            brain.Think();

            Assert.That(body.X, Is.EqualTo(LEASH_SITE.X + 5000));
        }

        [Test]
        public void Think_ShouldRoamOncePerGameDay_WhenNightFallsWithoutAggro()
        {
            GamePlayer watcher = NewPlayer(ROAM_SITE.X + 3000, ROAM_SITE.Y);
            DragonBrain brain = NewDragon(ROAM_SITE);
            GameNPC body = brain.Body;

            TestWorld.SetGameHour(21);
            brain.Think();

            Assert.That(brain._isRoaming, Is.True);
            Assert.That(brain._roamDoneToday, Is.True);
            Assert.That(body.Flags.HasFlag(GameNPC.eFlags.FLYING), Is.True);
            Assert.That(brain.FSM.GetCurrentState().StateType, Is.EqualTo(eFSMStateType.ROAMING));
            Assert.That(TestWorld.Messages(watcher), Does.Contain(string.Format(Config.RoamStartText, body.Name)));

            brain.Stop();

            Assert.That(brain._isRoaming, Is.False);
            Assert.That(body.Flags.HasFlag(GameNPC.eFlags.FLYING), Is.False);

            brain.Think();

            Assert.That(brain._isRoaming, Is.False);
            Assert.That(brain._roamDoneToday, Is.True);
        }

        [Test]
        public void MaxHealth_ShouldScaleWithTheRoster_WhenTheEncounterGrowsPastTheBaseline()
        {
            GamePlayer player = NewPlayer(SCALING_SITE.X + 1200, SCALING_SITE.Y);
            DragonBrain brain = NewDragon(SCALING_SITE);
            GameNPC body = brain.Body;

            brain.AddToAggroList(player, 100);
            brain.Think();

            RaidEncounter encounter = brain.RaidEncounter;

            Assert.That(encounter.Active, Is.True);
            Assert.That(encounter.HpMultiplier, Is.EqualTo(1).Within(0.0001));

            int baseline = body.MaxHealth;
            encounter.Size = Properties.RAID_SCALING_BASELINE_SIZE + 10;

            Assert.That(encounter.HpMultiplier, Is.EqualTo(1 + Properties.RAID_SCALING_HP_PER_EXTRA_PLAYER * 10).Within(0.0001));
            Assert.That(body.MaxHealth, Is.EqualTo((int) (baseline * encounter.HpMultiplier)).Within(2));
        }

        private GamePlayer NewPlayer(int x, int y)
        {
            GamePlayer player = TestWorld.CreatePlayer(_region, x, y);
            player.AbilityBonus[eProperty.MaxHealth] = 200000;
            player.Health = player.MaxHealth;
            _players.Add(player);
            return player;
        }

        private DragonBrain NewDragon((int X, int Y) site, int tetherRange = 0)
        {
            DragonBrain brain = new(Config);
            ItTestDragonBody body = new() { Name = "test dragon", Level = 70, TetherRange = tetherRange };
            body.SetOwnBrain(brain);
            brain.RaidEncounter = new(brain);
            TestWorld.AddNpc(body, _region, site.X, site.Y);
            body.SpawnPoint = new Point3D(site.X, site.Y, 0);
            _npcs.Add(body);
            return brain;
        }

        private static int MessengerCount()
        {
            return WorldMgr.GetNPCsByNameFromRegion(MESSENGER_NAME, REGION_ID, eRealm.None).Length;
        }
    }

    public class ItTestDragonBody : GameNPC
    {
        public override bool IsVisibleToPlayers => true;
    }

    public class ItTestMessenger : DragonMessenger
    {
        public ItTestMessenger() : base(IT_DragonBrain.Config) { }
    }

    public class ItTestAdd : DragonAdd
    {
        public ItTestAdd() : base(IT_DragonBrain.Config) { }
    }
}
