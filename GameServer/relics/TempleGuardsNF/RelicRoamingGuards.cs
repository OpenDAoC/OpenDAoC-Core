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
    // TODO: NPCs go in Idle after long time, and stop follow leader
    // Healh, Styles etc. adjustments
    // Level adjustments
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

            double headingRadiants = _leader.Heading * (Math.PI / 2048.0);
            double cosH = Math.Cos(headingRadiants);
            double sinH = Math.Sin(headingRadiants);

            int targetX = _leader.X + (int)(_offsetX * cosH - _offsetY * sinH);
            int targetY = _leader.Y + (int)(_offsetX * sinH + _offsetY * cosH);

            long dx = Body.X - targetX;
            long dy = Body.Y - targetY;
            long distSq = dx * dx + dy * dy;

            // 1. Formation halten (Drehen nur im Stand)
            // Wir erhöhen den Puffer minimal auf 1000, um Mikrobewegungen am Ziel zu vermeiden
            if (distSq < 1000)
            {
                if (Body.IsMoving) Body.StopMoving();
                if (Math.Abs(Body.Heading - _leader.Heading) > 20) Body.TurnTo(_leader.Heading);
                return;
            }

            // 2. Ziel-Winkel für die Wache berechnen
            double angle = Math.Atan2(targetX - Body.X, targetY - Body.Y);
            ushort targetHeading = (ushort)((int)(angle * 2048.0 / Math.PI) & 0xFFF);

            // 3. Laufrichtung setzen (Mit Toleranz gegen Zucken)
            // Nur drehen, wenn die Abweichung > 5 Grad ist (ca. 60 Einheiten)
            if (Math.Abs(Body.Heading - targetHeading) > 60)
            {
                Body.TurnTo(targetHeading);
            }

            // 4. Geschwindigkeit mit Dämpfungs-Zone
            short moveSpeed = _leader.MaxSpeed;

            // AUFHOLEN: Wenn weiter als ~110 Units weg
            if (distSq > 12100)
            {
                moveSpeed = (short)(moveSpeed * 1.25);
            }
            // ABBREMSEN: Wenn näher als ~55 Units dran
            else if (distSq < 3025)
            {
                moveSpeed = (short)(moveSpeed * 0.85);
            }
            // ZONE DAZWISCHEN: Hier nutzt er 1.0x Speed (kein Zucken)

            // 5. Marschieren
            // Wir erhöhen die Schwelle für den Neubefehl etwas, um Pakete zu sparen
            if (!Body.IsMoving || distSq > 1600)
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
                {    0, -100 },                  // 1. Wache: Direkt hinter dem Leader (Spitze)
                { -80, -200 }, {  80, -200 },  // 2. & 3. Wache: Erstes Paar (leicht versetzt)
                { -200, -300 }, {  200, -300 },  // 4. & 5. Wache: Zweites Paar
                { -320, -400 }, {  320, -400 },  // 6. & 7. Wache: Drittes Paar
                { -80, -350 }, {  80, -350 }   // 8. & 9. Wache: Viertes Paar (Ende des Vs)
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
                    follower = new RelicKeepGuard
                    {
                        // Index i-1, weil der erste Turm-Name (0) dem zweiten NPC (i=1) zugewiesen wird
                        Name = "Relic Defender of " + towerNames[i - 1]
                    };
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