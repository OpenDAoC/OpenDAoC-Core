using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Movement;
using DOL.GS.ServerProperties;

// Eight identical copies of Host patrol the Caer Sidi tower; a randomly chosen one is the real boss.
// Killing the real one kills every copy. Mobs with PackageID "HostBaf" in the same region assist when any copy is pulled.
// The encounter is driven by the "Host Initializator" NPC. Do not remove it in-game.
namespace DOL.GS
{
    public class HostInitializator : GameNPC
    {
        public const int HOST_COUNT = 8;

        private Host[] _hosts = [];
        private long _nextSpawnTime;

        public void TickEncounter()
        {
            if (_hosts.Length > 0)
            {
                foreach (Host host in _hosts)
                {
                    if (host.IsAlive)
                        return;
                }

                _hosts = [];
                _nextSpawnTime = GameLoop.GameLoopTime + Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000L;
                return;
            }

            if (GameServiceUtils.ShouldTick(_nextSpawnTime))
                SpawnHosts();
        }

        private void SpawnHosts()
        {
            Host[] hosts = new Host[HOST_COUNT];
            int realHostIndex = Util.Random(HOST_COUNT - 1);

            for (int i = 0; i < hosts.Length; i++)
            {
                Host host = new()
                {
                    X = X,
                    Y = Y,
                    Z = Z,
                    Heading = Heading,
                    CurrentRegion = CurrentRegion,
                    IsRealHost = i == realHostIndex
                };

                host.AddToWorld();
                hosts[i] = host;
            }

            // Siblings are used by the real Host to kill every copy when it dies.
            foreach (Host host in hosts)
                host.Siblings = hosts;

            _hosts = hosts;
        }

        public override bool AddToWorld()
        {
            SetOwnBrain(new HostInitializatorBrain());
            return base.AddToWorld();
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs = WorldMgr.GetNPCsByNameFromRegion("Host Initializator", 60, eRealm.None);

            if (npcs.Length > 0)
                return;

            log.Warn("Host Initializator not found, creating it...");

            HostInitializator initializator = new()
            {
                Name = "Host Initializator",
                GuildName = "DO NOT REMOVE!",
                Model = 665,
                Level = 50,
                Size = 50,
                Realm = eRealm.None,
                RespawnInterval = 5000,
                Flags = eFlags.CANTTARGET | eFlags.FLYING | eFlags.DONTSHOWNAME | eFlags.PEACE,
                Faction = FactionMgr.GetFactionByID(64),
                CurrentRegionID = 60, // Caer Sidi.
                X = 26995,
                Y = 29733,
                Z = 17871
            };

            initializator.AddToWorld();
            initializator.SaveIntoDatabase();
        }
    }

    public class Host : GameEpicBoss
    {
        public const string BAF_PACKAGE_ID = "HostBaf";

        private const short PATROL_SPEED = 100;

        // Patrol route through the tower, starting at the spawn location on the 3rd floor:
        // around the 3rd floor, down the stairs to the 2nd, around it, down to the 1st, around it, then all the way back up.
        private static readonly (int X, int Y, int Z)[] _patrolPoints =
        [
            (26995, 29733, 17871),
            (26749, 29730, 17871),
            (26180, 30241, 17871),
            (25743, 30447, 17861),
            (25154, 30151, 17861),
            (24901, 29673, 17861),
            (25376, 29310, 17861),
            (25360, 29635, 17866),
            (25608, 29967, 17702),
            (25984, 29902, 17534),
            (26121, 29617, 17405),
            (25889, 29309, 17251),
            (25453, 29390, 17051),
            (25372, 29775, 16897),
            (25946, 29958, 16638),
            (26116, 29523, 16495),
            (26106, 29305, 16495),
            (25061, 29335, 16495),
            (25046, 30229, 16495),
            (25686, 30428, 16495),
            (26832, 29793, 16495),
            (25718, 29012, 16495),
            (25358, 29563, 16495),
            (25426, 29842, 16406),
            (25842, 29983, 16223),
            (26129, 29643, 16039),
            (25714, 29267, 15796),
            (25345, 29587, 15588),
            (25711, 29995, 15357),
            (26123, 29645, 15122),
            (25796, 28979, 15120),
            (24729, 29725, 15119),
            (25695, 30592, 15119),
            (26792, 29721, 15119),
            (26102, 29302, 15120),
            (26085, 29802, 15192),
            (25487, 29903, 15457),
            (25370, 29483, 15625),
            (25873, 29309, 15872),
            (26103, 29695, 16058),
            (25693, 29975, 16284),
            (25352, 29538, 16495),
            (25775, 29107, 16495),
            (26114, 29597, 16495),
            (25730, 29985, 16722),
            (25368, 29610, 16957),
            (25705, 29283, 17169),
            (26109, 29587, 17393),
            (25759, 30023, 17632),
            (25359, 29578, 17871),
            (25809, 29142, 17871),
            (26344, 29391, 17871)
        ];

        public bool IsRealHost { get; set; }
        public Host[] Siblings { get; set; } = [];

        public override int MeleeAttackRange => 350;

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40; // dmg reduction for melee dmg
                case eDamageType.Crush: return 40; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 50; // dmg reduction for rest resists
            }
        }

        public override int MaxHealth => 30000;

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 250;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }

        public override void Die(GameObject killer)
        {
            if (IsRealHost)
            {
                foreach (Host sibling in Siblings)
                {
                    if (sibling != this && sibling.IsAlive)
                        sibling.Die(killer);
                }
            }

            base.Die(killer);
        }

        public override bool AddToWorld()
        {
            Name = "Host";
            Model = 26;
            Size = 60;
            Level = 79;
            MaxSpeedBase = 300;
            Flags = eFlags.GHOST;
            MeleeDamageType = eDamageType.Crush;
            BodyType = 6;
            Realm = eRealm.None;
            Faction = FactionMgr.GetFactionByID(64);
            PackageID = IsRealHost ? "HostReal" : "HostCopy";
            RespawnInterval = -1;
            TetherRange = 0;
            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);

            Strength = 5;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 220;
            Intelligence = 220;
            Empathy = 200;

            SetOwnBrain(new HostBrain());
            return base.AddToWorld();
        }
    }
}

namespace DOL.AI.Brain
{
    public class HostInitializatorBrain : StandardMobBrain
    {
        public HostInitializatorBrain() : base()
        {
            ThinkInterval = 10000;
        }

        public override void Think()
        {
            (Body as HostInitializator)?.TickEncounter();
        }
    }

    public class HostBrain : StandardMobBrain
    {
        private bool _pulledFriends;

        public HostBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 2500;
        }

        public override void Think()
        {
            if (HasAggro && Body.TargetObject != null)
            {
                if (!_pulledFriends)
                {
                    _pulledFriends = true;
                    PullRegionFriends();
                }
            }
            else if (!Body.InCombat)
            {
                _pulledFriends = false;
                Body.Health = Body.MaxHealth;
            }

            base.Think();
        }

        // The copies and their linked mobs can be anywhere in the tower, so pull the entire region rather than a radius.
        private void PullRegionFriends()
        {
            foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc == Body || !npc.IsAlive)
                    continue;

                if (npc.Brain is not StandardMobBrain friend || friend.HasAggro)
                    continue;

                if (friend is HostBrain || npc.PackageID == Host.BAF_PACKAGE_ID)
                    AddAggroListTo(friend);
            }
        }
    }
}
