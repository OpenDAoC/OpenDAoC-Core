using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Keeps;
using System.Timers;

namespace DOL.GS
{
    public class RelicPatrolGuard : GameNPC
    {
        public override short MaxSpeed => 150;
        public int FormationX { get; set; }
        public int FormationY { get; set; }
        public bool IsLeader { get; set; }
        public RelicPatrolGuard MyLeader { get; set; }

        [GameServerStartedEvent]
        public static void OnServerStartup(DOLEvent e, object sender, EventArgs args)
        {
            //string[] midTowers = { "Fensalir Faste Watchtower", "Arvakr Faste Watchtower", "Hlidskialf Faste Watchtower", "Glenlock Faste Watchtower", "Nottmor Faste Watchtower", "Bledmeer Faste Watchtower", "Blendrake Faste Outpost", "Nottmor Faste Outpost" };
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Midgard, 163, 597415, 304597, 8088, "patrol_lamfhota");
            //string[] midTowers = { "Fensalir Faste Guardtower", "Arvakr Faste Guardtower", "Hlidskialf Faste Guardtower", "Glenlock Faste Guardtower", "Nottmor Faste Guardtower", "Bledmeer Faste Guardtower", "Hlidskialf Faste Outpost", "Glenlock Faste Outpost" };
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Midgard, 163, 597415, 304597, 8088, "patrol_lamfhota");
            string[] hibTowers = { "Dun Ailinne Watchtower", "Dun Scathaig Watchtower", "Dun da Behnn Watchtower", "Dun nGed Watchtower", "Dun Crimthain Watchtower", "Dun Crauchon Watchtower", "Dun Bolg Outpost", "Dun Crimthain Outpost" };
            RelicPatrolManager.SpawnPatrolGroup(eRealm.Hibernia, 163, 374452, 590104, 8571, "patrol_lamfhota", hibTowers);
            //string[] hibTowers = { "Dun Ailinne Guardtower", "Dun Scathaig Guardtower", "Dun da Behnn Guardtower", "Dun nGed Guardtower", "Dun Crimthain Guardtower", "Dun Crauchon Guardtower", "Dun da Behnn Outpost", "Dun nGed Outpost" };
            //RelicPatrolManager.SpawnPatrolGroup(eRealm.Hibernia, 163, 374452, 590104, 8571, "patrol_lamfhota");
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
                this.LoadEquipmentTemplateFromDatabase("relic_patrol_" + Realm.ToString().ToLower().Substring(0, 3));
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
        // Diese Variablen speichern wir IM Brain, damit JEDER NPC (auch KeepGuards) sie nutzen kann
        private GameNPC _leader;
        private int _offsetX;
        private int _offsetY;

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
            if (Body == null || !Body.IsAlive || _leader == null) return;
            if (Body.InCombat) { base.Think(); return; }

            // 1. Zielposition berechnen (Relativ zum Leader)
            double headingRadiants = _leader.Heading * (Math.PI / 2048.0);
            double cosH = Math.Cos(headingRadiants);
            double sinH = Math.Sin(headingRadiants);

            int targetX = _leader.X + (int)(_offsetX * cosH - _offsetY * sinH);
            int targetY = _leader.Y + (int)(_offsetX * sinH + _offsetY * cosH);

            // 2. Distanz berechnen
            long dx = Body.X - targetX;
            long dy = Body.Y - targetY;
            long distSq = dx * dx + dy * dy;

            // 3. Bewegung
            if (distSq < 900)
            {
                Body.TurnTo(_leader.Heading);
                return;
            }

            double angle = Math.Atan2(targetX - Body.X, targetY - Body.Y);
            ushort targetHeading = (ushort)((int)(angle * 2048.0 / Math.PI) & 0xFFF);
            Body.TurnTo(targetHeading);

            short moveSpeed = _leader.MaxSpeed;
            if (distSq > 40000) moveSpeed = (short)(moveSpeed * 1.25);
            else if (distSq < 2500) moveSpeed = (short)(moveSpeed * 0.9);

            if (!Body.IsMoving || distSq > 2500)
            {
                Body.WalkTo(new Point3D(targetX, targetY, _leader.Z), moveSpeed);
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
        public static void SpawnPatrolGroup(eRealm realm, ushort region, int x, int y, int z, string pathID, string[] towerNames)
        {
            // 1. Leader erstellen
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

            // Structure of roaming guards based on leader position
            int[,] offsets = {
                { 0, -80 }, { -100, -150 }, { 100, -150 },
                { -200, -220 }, { 200, -220 }, { -300, -290 },
                { 300, -290 }, { -70, -270 }, { 70, -270 }
            };

            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                GameNPC follower;

                // First Guard is normal RelicGuard
                if (i == 0)
                {
                    follower = new RelicGuard();
                }
                // 2. Alle anderen sind RelicKeepGuards, sofern wir Turm-Namen haben
                else if (towerNames != null && (i - 1) < towerNames.Length)
                {
                    follower = new RelicKeepGuard();
                    // Index i-1, weil der erste Turm-Name (0) dem zweiten NPC (i=1) zugewiesen wird
                    follower.Name = "Relic Defender of " + towerNames[i - 1];
                }
                // 3. Fallback, falls mehr NPCs als Turm-Namen vorhanden sind
                else
                {
                    follower = new RelicPatrolGuard();
                }

                // Standard-Zuweisungen
                follower.Realm = realm;
                follower.CurrentRegionID = region;
                follower.X = x + offsets[i, 0];
                follower.Y = y + offsets[i, 1];
                follower.Z = z;

                follower.AddToWorld();

                // Formation erzwingen
                follower.SetOwnBrain(new RelicPatrolBrain(leader, offsets[i, 0], offsets[i, 1]));
            }
        }
    }
}