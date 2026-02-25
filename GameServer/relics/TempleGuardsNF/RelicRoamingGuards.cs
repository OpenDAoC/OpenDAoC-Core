using System;
using DOL.GS;
using DOL.AI.Brain;
using DOL.Events;

namespace DOL.GS
{
    // TODO: NPCs go in Idle after long time, and stop follow leader
    // Healh, Styles etc. adjustments
    // Level adjustments
    public class RelicPatrolGuard : GameNPC
    {
        public override short MaxSpeed => 150;
        public bool IsLeader { get; set; }
        public override bool IsVisibleToPlayers => true;

        [GameServerStartedEvent]
        public static void OnServerStartup(DOLEvent e, object sender, EventArgs args)
        {
            //string[] albTowers1 = { "Caer Renaris Watchtower", "Caer Hurbury Watchtower", "Caer Sursbrooke Watchtower", "Caer Boldiam Watchtower", "Caer Berkstead Watchtower", "Caer Benowyc Spire", "Caer Erasleigh Outpost", "Caer Berkstead Outpost" };
            //string[] albTowers2 = { "Caer Renaris Guardtower", "Caer Hurbury Guardtower", "Caer Sursbrooke Guardtower", "Caer Boldiam Guardtower", "Caer Berkstead Guardtower", "Caer Benowyc Watchtower", "Caer Sursbrooke Outpost", "Caer Boldiam Outpost" };
            //string[] midTowers1 = { "Fensalir Faste Watchtower", "Arvakr Faste Watchtower", "Hlidskialf Faste Watchtower", "Glenlock Faste Watchtower", "Nottmor Faste Watchtower", "Bledmeer Faste Spire", "Blendrake Faste Outpost", "Nottmor Faste Outpost" };
            //string[] midTowers2 = { "Fensalir Faste Guardtower", "Arvakr Faste Guardtower", "Hlidskialf Faste Guardtower", "Glenlock Faste Guardtower", "Nottmor Faste Guardtower", "Bledmeer Faste Guardtower", "Hlidskialf Faste Outpost", "Glenlock Faste Outpost" };
            string[] hibTowers1 = { "Dun Ailinne Watchtower", "Dun Scathaig Watchtower", "Dun da Behnn Watchtower", "Dun nGed Watchtower", "Dun Crimthain Watchtower", "Dun Crauchon Spire", "Dun Bolg Outpost", "Dun Crimthain Outpost" };
            string[] hibTowers2 = { "Dun Ailinne Guardtower", "Dun Scathaig Guardtower", "Dun da Behnn Guardtower", "Dun nGed Guardtower", "Dun Crimthain Guardtower", "Dun Crauchon Guardtower", "Dun da Behnn Outpost", "Dun nGed Outpost" };


            
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Midgard, 163, 597415, 304597, 8088, "patrol_mjollner", midTowers);
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Midgard, 163, 597415, 304597, 8088, "patrol_mjollner_reverse", midTowers);

            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Midgard, 163, 597415, 304597, 8088, "patrol_grallarhorn", midTowers);
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Midgard, 163, 597415, 304597, 8088, "patrol_grallarhorn_reverse", midTowers);
            
            RelicPatrolManager.SpawnPatrolGroup(eRealm.Hibernia, 163, 374418, 590129, 8578, "patrol_lamfhota", hibTowers1);
            RelicPatrolManager.SpawnPatrolGroup(eRealm.Hibernia, 163, 371058, 590136, 8566, "patrol_lamfhota_reverse", hibTowers2);

            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Hibernia, 163, 374452, 590104, 8571, "patrol_dagda", hibTowers);
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Hibernia, 163, 374452, 590104, 8571, "patrol_dagda_reverse", hibTowers);
            
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Albion, 163, 672170, 589589, 8609, "patrol_excalibur", albTowers);
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Albion, 163, 672170, 589589, 8609, "patrol_excalibur_reverse", albTowers);

            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Albion, 163, 374452, 590104, 8571, "patrol_myrddin", albTowers);
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Albion, 163, 374452, 590104, 8571, "patrol_myrddin_reverse", albTowers);
        }

        public override bool AddToWorld()
        {
            if (!IsLeader)
            {
                switch (Realm)
                {
                    case eRealm.Albion: Name = "Relic Patrolman"; Model = 14; break;
                    case eRealm.Midgard: Name = "Relic Patrouiller"; Model = 137; break;
                    case eRealm.Hibernia: Name = "Relic Sentinel"; Model = 318; break;
                }
                string realmSuffix = Realm switch { eRealm.Albion => "alb", eRealm.Midgard => "mid", _ => "hib" };
                LoadEquipmentTemplateFromDatabase("relic_patrol_" + realmSuffix);
            }
            else
            {
                Model = 1;
                Name = "";
            }

            Level = 65;
            return base.AddToWorld();
        }
    }
}

namespace DOL.AI.Brain
{
    public class RelicPatrolBrain : StandardMobBrain
    {
        private readonly GameNPC _leader;
        private readonly int _offsetX;
        private readonly int _offsetY;
        // Cached trigonometry — recalculated only when the leader's heading actually changes.
        private ushort _lastLeaderHeading = ushort.MaxValue;
        private double _cosH;
        private double _sinH;
        // Reused walk-target — avoids allocating a new Point3D every 200 ms per follower.
        private readonly Point3D _walkTarget = new Point3D(0, 0, 0);

        public RelicPatrolBrain(GameNPC leader, int x, int y) : base()
        {
            _leader = leader;
            _offsetX = x;
            _offsetY = y;
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 200;
        }

        public override void Think()
        {
            if (Body == null || !Body.IsAlive || _leader == null || !_leader.IsAlive) return;
            if (Body.InCombat) { base.Think(); return; }

            // Recompute trig only when the leader actually turns — avoids redundant
            // Math.Cos/Math.Sin calls every tick for every follower in the patrol group.
            if (_leader.Heading != _lastLeaderHeading)
            {
                _lastLeaderHeading = _leader.Heading;
                double rad = _leader.Heading * (Math.PI / 2048.0);
                _cosH = Math.Cos(rad);
                _sinH = Math.Sin(rad);
            }

            int targetX = _leader.X + (int)(_offsetX * _cosH - _offsetY * _sinH);
            int targetY = _leader.Y + (int)(_offsetX * _sinH + _offsetY * _cosH);

            long dx = Body.X - targetX;
            long dy = Body.Y - targetY;
            long distSq = dx * dx + dy * dy;

            // 1. Hold formation: stop micro-movement when close enough.
            // Buffer of 1000 units prevents jitter at the destination.
            if (distSq < 1000)
            {
                if (Body.IsMoving) Body.StopMoving();
                if (Math.Abs(Body.Heading - _leader.Heading) > 20) Body.TurnTo(_leader.Heading);
                return;
            }

            // 2. Calculate the target heading for this guard.
            double angle = Math.Atan2(targetX - Body.X, targetY - Body.Y);
            ushort targetHeading = (ushort)((int)(angle * 2048.0 / Math.PI) & 0xFFF);

            // 3. Turn only when deviation exceeds ~5 degrees (~60 units) to prevent jitter.
            if (Math.Abs(Body.Heading - targetHeading) > 60)
            {
                Body.TurnTo(targetHeading);
            }

            // 4. Speed with damping zones.
            short moveSpeed = _leader.MaxSpeed;

            // Catch-up: more than ~110 units away.
            if (distSq > 12100)
            {
                moveSpeed = (short)(moveSpeed * 1.25);
            }
            // Slow down: closer than ~55 units.
            else if (distSq < 3025)
            {
                moveSpeed = (short)(moveSpeed * 0.85);
            }

            // 5. March — reuse the cached walk target to avoid per-tick allocation.
            // Only issue a new WalkTo when not already moving or when significantly off-target.
            if (!Body.IsMoving || distSq > 1600)
            {
                _walkTarget.X = targetX;
                _walkTarget.Y = targetY;
                _walkTarget.Z = _leader.Z;
                Body.WalkTo(_walkTarget, moveSpeed);
            }
        }

        public override bool CanAggroTarget(GameLiving target)
        {
            if (Body == null || target == null) return false;
            return GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
        }
    }
}

namespace DOL.GS
{
    public static class RelicPatrolManager
    {
        // Static formation offsets — allocated once, shared across all patrol groups.
        private static readonly int[,] _formationOffsets =
        {
            {    0, -100 }, // 1st guard: directly behind the leader (tip)
            {  -80, -200 }, {   80, -200 }, // 2nd & 3rd: first pair
            { -200, -300 }, {  200, -300 }, // 4th & 5th: second pair
            { -320, -400 }, {  320, -400 }, // 6th & 7th: third pair
            {  -80, -350 }, {   80, -350 }, // 8th & 9th: fourth pair (end of V)
        };

        public static void SpawnPatrolGroup(eRealm realm, ushort region, int x, int y, int z, string pathID, string[] towerNames)
        {
            // 1. Create the invisible path-following leader.
            RelicPatrolGuard leader = new RelicPatrolGuard
            {
                Realm = realm,
                Flags = GameNPC.eFlags.PEACE | GameNPC.eFlags.CANTTARGET | GameNPC.eFlags.DONTSHOWNAME,
                CurrentRegionID = region,
                X = x,
                Y = y,
                Z = z,
                IsLeader = true,
                PathID = pathID
            };
            leader.AddToWorld();

            // Formation setup based on leader position.
            for (int i = 0; i < _formationOffsets.GetLength(0); i++)
            {
                GameNPC follower;

                // First follower is a plain RelicGuard.
                if (i == 0)
                {
                    follower = new RelicGuard();
                }
                // Remaining slots map to named keep guards if tower names were supplied.
                else if (towerNames != null && (i - 1) < towerNames.Length)
                {
                    follower = new RelicKeepGuard
                    {
                        Name = "Relic Defender of " + towerNames[i - 1]
                    };
                }
                // Fallback when more NPCs than tower names are provided.
                else
                {
                    follower = new RelicPatrolGuard();
                }

                follower.Realm = realm;
                follower.CurrentRegionID = region;
                follower.X = x + _formationOffsets[i, 0];
                follower.Y = y + _formationOffsets[i, 1];
                follower.Z = z;

                follower.AddToWorld();
                follower.SetOwnBrain(new RelicPatrolBrain(leader, _formationOffsets[i, 0], _formationOffsets[i, 1]));
            }
        }
    }
}