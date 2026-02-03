using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.AI.Brain;
using DOL.Logging;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class RelicCaster : GameNPC
    {
        private static new readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // Statisches Dictionary für die Zauber der Reiche
        private static readonly Dictionary<eRealm, Spell> _relicSpells = new Dictionary<eRealm, Spell>();

        protected const byte RelicCasterLevel = 65;
        protected const int RelicCasterRespawnInterval = 900000; // 15 Minuten

        static RelicCaster()
        {
            InitRelicSpells();
        }

        public RelicCaster() : base() { }

        #region Spell Initialization
        private static void InitRelicSpells()
        {
            // Albion
            _relicSpells[eRealm.Albion] = new Spell(new DbSpell
            {
                Name = "Relic Blast",
                CastTime = 2,
                ClientEffect = 77,
                Damage = 450,
                Range = 2000,
                Radius = 1000,
                Target = "ENEMY",
                Type = "DirectDamage",
                DamageType = (int)eDamageType.Heat
            }, 0);

            // Midgard
            _relicSpells[eRealm.Midgard] = new Spell(new DbSpell
            {
                Name = "Relic Ice",
                CastTime = 2,
                ClientEffect = 2570,
                Damage = 450,
                Range = 2000,
                Radius = 1000, // Hinzugefügt
                Target = "ENEMY",
                Type = "DirectDamage", // Geändert
                DamageType = (int)eDamageType.Cold
            }, 0);

            // Hibernia
            _relicSpells[eRealm.Hibernia] = new Spell(new DbSpell
            {
                Name = "Relic Fire",
                CastTime = 2,
                ClientEffect = 4269, // Hier kannst du die 3051er Effekte nutzen
                Damage = 450,
                Range = 2000,
                Radius = 1000, // Hinzugefügt
                Target = "ENEMY",
                Type = "DirectDamage", // Geändert
                DamageType = (int)eDamageType.Cold
            }, 0);
        }

        public void SetRelicSpells()
        {
            if (_relicSpells.TryGetValue(Realm, out Spell spell))
            {
                Spells = new List<Spell> { spell };
            }
        }
        #endregion

        #region Spawn Logic
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            var spawnPoints = new[]
            {
                // Albion (Castle Excalibur)
                new { Name = "Relic Wizard", Model = (ushort)61, Realm = eRealm.Albion, Region = (ushort)163, X = 672934, Y = 590012, Z = 8738, Heading = (ushort)8738, Equip = "relic_temple_caster_alb" },
                new { Name = "Relic Wizard", Model = (ushort)61, Realm = eRealm.Albion, Region = (ushort)163, X = 674760, Y = 590021, Z = 8738, Heading = (ushort)2730, Equip = "relic_temple_caster_alb" },
                new { Name = "Relic Wizard", Model = (ushort)61, Realm = eRealm.Albion, Region = (ushort)163, X = 673846, Y = 591603, Z = 8738, Heading = (ushort)4095, Equip = "relic_temple_caster_alb" },
                // Albion (Castle Myrddin)
                new { Name = "Relic Wizard", Model = (ushort)61, Realm = eRealm.Albion, Region = (ushort)163, X = 579091, Y = 676623, Z = 8730, Heading = (ushort)2726, Equip = "relic_temple_caster_alb" },
                new { Name = "Relic Wizard", Model = (ushort)61, Realm = eRealm.Albion, Region = (ushort)163, X = 578175, Y = 678208, Z = 8730, Heading = (ushort)4095, Equip = "relic_temple_caster_alb" },
                new { Name = "Relic Wizard", Model = (ushort)61, Realm = eRealm.Albion, Region = (ushort)163, X = 577261, Y = 676622, Z = 8730, Heading = (ushort)1378, Equip = "relic_temple_caster_alb" },
                // Midgard (Mjollner Faste)
                new { Name = "Relic Runemaster", Model = (ushort)169, Realm = eRealm.Midgard, Region = (ushort)163, X = 611826, Y = 302515, Z = 8490, Heading = (ushort)2728, Equip = "relic_temple_caster_mid" },
                new { Name = "Relic Runemaster", Model = (ushort)169, Realm = eRealm.Midgard, Region = (ushort)163, X = 610914, Y = 304094, Z = 8490, Heading = (ushort)6, Equip = "relic_temple_caster_mid" },
                new { Name = "Relic Runemaster", Model = (ushort)169, Realm = eRealm.Midgard, Region = (ushort)163, X = 610001, Y = 302513, Z = 8490, Heading = (ushort)1368, Equip = "relic_temple_caster_mid" },
                // Midgard (Grallarhorn Faste)
                new { Name = "Relic Runemaster", Model = (ushort)169, Realm = eRealm.Midgard, Region = (ushort)163, X = 713095, Y = 404789, Z = 8778, Heading = (ushort)4093, Equip = "relic_temple_caster_mid" },
                new { Name = "Relic Runemaster", Model = (ushort)169, Realm = eRealm.Midgard, Region = (ushort)163, X = 712176, Y = 403220, Z = 8778, Heading = (ushort)1360, Equip = "relic_temple_caster_mid" },
                new { Name = "Relic Runemaster", Model = (ushort)169, Realm = eRealm.Midgard, Region = (ushort)163, X = 713997, Y = 403216, Z = 8778, Heading = (ushort)2733, Equip = "relic_temple_caster_mid" },
                // Hibernia (Dun Lamfhota)
                new { Name = "Relic Eldritch", Model = (ushort)334, Realm = eRealm.Hibernia, Region = (ushort)163, X = 372728, Y = 592154, Z = 8730, Heading = (ushort)4086, Equip = "relic_temple_caster_hib" },
                new { Name = "Relic Eldritch", Model = (ushort)334, Realm = eRealm.Hibernia, Region = (ushort)163, X = 371830, Y = 590577, Z = 8730, Heading = (ushort)1360, Equip = "relic_temple_caster_hib" },
                new { Name = "Relic Eldritch", Model = (ushort)334, Realm = eRealm.Hibernia, Region = (ushort)163, X = 373643, Y = 590576, Z = 8730, Heading = (ushort)2713, Equip = "relic_temple_caster_hib" },
                // Hibernia (Dun Dagda)
                new { Name = "Relic Eldritch", Model = (ushort)334, Realm = eRealm.Hibernia, Region = (ushort)163, X = 471126, Y = 677236, Z = 8106, Heading = (ushort)2719, Equip = "relic_temple_caster_hib" },
                new { Name = "Relic Eldritch", Model = (ushort)334, Realm = eRealm.Hibernia, Region = (ushort)163, X = 470201, Y = 678817, Z = 8106, Heading = (ushort)4079, Equip = "relic_temple_caster_hib" },
                new { Name = "Relic Eldritch", Model = (ushort)334, Realm = eRealm.Hibernia, Region = (ushort)163, X = 469298, Y = 677234, Z = 8106, Heading = (ushort)1364, Equip = "relic_temple_caster_hib" }
            };

            foreach (var sp in spawnPoints)
            {
                string customID = $"RelicCaster_{sp.Realm}_{sp.X}_{sp.Y}";
                if (WorldMgr.GetNPCsFromRegion(sp.Region).Any(n => n.InternalID == customID))
                    continue;

                RelicCaster caster = new RelicCaster
                {
                    InternalID = customID,
                    Name = sp.Name,
                    Model = sp.Model,
                    Level = RelicCasterLevel,
                    Realm = sp.Realm,
                    X = sp.X,
                    Y = sp.Y,
                    Z = sp.Z,
                    Heading = sp.Heading,
                    CurrentRegionID = sp.Region,
                    RespawnInterval = RelicCasterRespawnInterval
                };

                if (!string.IsNullOrEmpty(sp.Equip))
                    caster.LoadEquipmentTemplateFromDatabase(sp.Equip);

                caster.AddToWorld();
            }
        }

        public override bool AddToWorld()
        {
            SetOwnBrain(new RelicCasterBrain());
            SetRelicSpells();
            return base.AddToWorld();
        }
        #endregion

        #region Combat
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (IsReturningToSpawnPoint)
            {
                if (source is GamePlayer player)
                    player.Out.SendMessage($"{Name} is currently immune to damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }

        public override void Die(GameObject killer)
        {
            int count = TempleRelicPadsLoader.GetEnemiesNearby(this);
            TempleRelicPadsLoader.SendTempleMessage($"{Name} has been killed with {count} enemies in the area.");
            base.Die(killer);
        }
        #endregion
    }
}

namespace DOL.AI.Brain
{
    public class RelicCasterBrain : StandardMobBrain
    {
        protected const int MaxDistance = 2000;

        public RelicCasterBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 1000;
        }

        public override void Think()
        {
            if (Body == null || !Body.IsAlive || Body.IsReturningToSpawnPoint) return;

            // Überprüfung der Leine (Leash)
            if (Body.GetDistanceTo(Body.SpawnPoint) > MaxDistance)
            {
                ResetCaster();
                return;
            }

            base.Think();
        }

        private void ResetCaster()
        {
            Body.StopAttack();
            ClearAggroList();
            Body.Health = Body.MaxHealth;
            Body.Mana = Body.MaxMana;
            Body.ReturnToSpawnPoint(Body.MaxSpeed);
        }

        public override bool CanAggroTarget(GameLiving target)
        {
            if (Body == null || target == null) return false;
            return GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
        }
    }
}