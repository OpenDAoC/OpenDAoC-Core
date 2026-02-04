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
            // Wizard
            _relicSpells[eRealm.Albion] = new Spell(new DbSpell
            {
                Name = "Relic Flames",
                CastTime = 3,
                ClientEffect = 368, // Wizard flame effect
                Damage = 450,
                Range = 1500,
                Radius = 1000,
                Target = "ENEMY",
                Type = "DirectDamage",
                DamageType = (int)eDamageType.Heat
            }, 0);

            // Runemaster
            _relicSpells[eRealm.Midgard] = new Spell(new DbSpell
            {
                Name = "Relic Spears",
                CastTime = 2,
                ClientEffect = 2958, // Runemaster Spear effect
                Damage = 450,
                Range = 1500,
                Radius = 1000,
                Target = "ENEMY",
                Type = "DirectDamage",
                DamageType = (int)eDamageType.Energy
            }, 0);

            // Eldritch
            _relicSpells[eRealm.Hibernia] = new Spell(new DbSpell
            {
                Name = "Relic Void",
                CastTime = 2,
                ClientEffect = 4568, // Eldritch black void balls effect
                Damage = 450,
                Range = 1500,
                Radius = 1000,
                Target = "ENEMY",
                Type = "DirectDamage",
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
        [GameServerStartedEvent]
        public static void OnServerStarted(DOLEvent e, object sender, EventArgs args)
        {
            CreateCaster();
            log.Info("[RelicCaster] System erfolgreich initialisiert.");
        }
        public static void CreateCaster()
        {
            var spawnPoints = new[]
            {
                // Albion (Castle Excalibur)
                new { Name = "Relic Wizard", Model = new ushort[] { 61, 65 }, Realm = eRealm.Albion, Region = (ushort)163, X = 672934, Y = 590012, Z = 8738, Heading = (ushort)1344, Equip = "relic_temple_caster_alb" },
                new { Name = "Relic Wizard", Model = new ushort[] { 61, 65 }, Realm = eRealm.Albion, Region = (ushort)163, X = 674760, Y = 590021, Z = 8738, Heading = (ushort)2730, Equip = "relic_temple_caster_alb" },
                new { Name = "Relic Wizard", Model = new ushort[] { 61, 65 }, Realm = eRealm.Albion, Region = (ushort)163, X = 673846, Y = 591603, Z = 8738, Heading = (ushort)4095, Equip = "relic_temple_caster_alb" },
                // Albion (Castle Myrddin)
                new { Name = "Relic Wizard", Model = new ushort[] { 61, 65 }, Realm = eRealm.Albion, Region = (ushort)163, X = 579091, Y = 676623, Z = 8730, Heading = (ushort)2726, Equip = "relic_temple_caster_alb" },
                new { Name = "Relic Wizard", Model = new ushort[] { 61, 65 }, Realm = eRealm.Albion, Region = (ushort)163, X = 578175, Y = 678208, Z = 8730, Heading = (ushort)4095, Equip = "relic_temple_caster_alb" },
                new { Name = "Relic Wizard", Model = new ushort[] { 61, 65 }, Realm = eRealm.Albion, Region = (ushort)163, X = 577261, Y = 676622, Z = 8730, Heading = (ushort)1378, Equip = "relic_temple_caster_alb" },
                // Midgard (Mjollner Faste)
                new { Name = "Relic Runemaster", Model = new ushort[] { 169, 177 }, Realm = eRealm.Midgard, Region = (ushort)163, X = 611826, Y = 302515, Z = 8490, Heading = (ushort)2728, Equip = "relic_temple_caster_mid" },
                new { Name = "Relic Runemaster", Model = new ushort[] { 169, 177 }, Realm = eRealm.Midgard, Region = (ushort)163, X = 610914, Y = 304094, Z = 8490, Heading = (ushort)6, Equip = "relic_temple_caster_mid" },
                new { Name = "Relic Runemaster", Model = new ushort[] { 169, 177 }, Realm = eRealm.Midgard, Region = (ushort)163, X = 610001, Y = 302513, Z = 8490, Heading = (ushort)1368, Equip = "relic_temple_caster_mid" },
                // Midgard (Grallarhorn Faste)
                new { Name = "Relic Runemaster", Model = new ushort[] { 169, 177 }, Realm = eRealm.Midgard, Region = (ushort)163, X = 713095, Y = 404789, Z = 8778, Heading = (ushort)4093, Equip = "relic_temple_caster_mid" },
                new { Name = "Relic Runemaster", Model = new ushort[] { 169, 177 }, Realm = eRealm.Midgard, Region = (ushort)163, X = 712176, Y = 403220, Z = 8778, Heading = (ushort)1360, Equip = "relic_temple_caster_mid" },
                new { Name = "Relic Runemaster", Model = new ushort[] { 169, 177 }, Realm = eRealm.Midgard, Region = (ushort)163, X = 713997, Y = 403216, Z = 8778, Heading = (ushort)2733, Equip = "relic_temple_caster_mid" },
                // Hibernia (Dun Lamfhota)
                new { Name = "Relic Eldritch", Model = new ushort[] { 334, 342 }, Realm = eRealm.Hibernia, Region = (ushort)163, X = 372728, Y = 592154, Z = 8730, Heading = (ushort)4086, Equip = "relic_temple_caster_hib" },
                new { Name = "Relic Eldritch", Model = new ushort[] { 334, 342 }, Realm = eRealm.Hibernia, Region = (ushort)163, X = 371830, Y = 590577, Z = 8730, Heading = (ushort)1360, Equip = "relic_temple_caster_hib" },
                new { Name = "Relic Eldritch", Model = new ushort[] { 334, 342 }, Realm = eRealm.Hibernia, Region = (ushort)163, X = 373643, Y = 590576, Z = 8730, Heading = (ushort)2713, Equip = "relic_temple_caster_hib" },
                // Hibernia (Dun Dagda)
                new { Name = "Relic Eldritch", Model = new ushort[] { 334, 342 }, Realm = eRealm.Hibernia, Region = (ushort)163, X = 471126, Y = 677236, Z = 8106, Heading = (ushort)2719, Equip = "relic_temple_caster_hib" },
                new { Name = "Relic Eldritch", Model = new ushort[] { 334, 342 }, Realm = eRealm.Hibernia, Region = (ushort)163, X = 470201, Y = 678817, Z = 8106, Heading = (ushort)4079, Equip = "relic_temple_caster_hib" },
                new { Name = "Relic Eldritch", Model = new ushort[] { 334, 342 }, Realm = eRealm.Hibernia, Region = (ushort)163, X = 469298, Y = 677234, Z = 8106, Heading = (ushort)1364, Equip = "relic_temple_caster_hib" }
            };

            foreach (var sp in spawnPoints)
            {
                string customID = $"RelicCaster_{sp.Realm}_{sp.X}_{sp.Y}";
                if (WorldMgr.GetNPCsFromRegion(sp.Region).Any(n => n.InternalID == customID))
                    continue;
                // randomly select given models
                ushort randomModel = sp.Model[Util.Random(0, sp.Model.Length - 1)];

                RelicCaster caster = new RelicCaster
                {
                    InternalID = customID,
                    Name = sp.Name,
                    Model = randomModel,
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
            // Caster Guards should not me mez, confuse, snare or stun
            AddAbility(SkillBase.GetAbility(GS.Abilities.ConfusionImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.CCImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.RootImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.MezzImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.StunImmunity));
            return base.AddToWorld();
        }
        #endregion

        #region Combat

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

        /*public override void Think()
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
        }*/
    }
}