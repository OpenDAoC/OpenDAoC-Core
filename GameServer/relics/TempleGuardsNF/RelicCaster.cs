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
        private static readonly Dictionary<eRealm, Spell> _relicSpells = new Dictionary<eRealm, Spell>();

        protected const byte RelicCasterLevel = 65;
        protected const int RelicCasterRespawnInterval = 900000; // 15min

        static RelicCaster()
        {
            InitRelicSpells();
        }

        public override bool AddToWorld()
        {
            switch (Realm)
            {
                case eRealm.Albion:
                    Name = "Relic Wizard";
                    Model = 61; // Avalonian
                    break;
                case eRealm.Midgard:
                    Name = "Relic Runemaster";
                    Model = 169; // Kobold
                    break;
                case eRealm.Hibernia:
                    Name = "Relic Eldritch";
                    Model = 334; // Elf
                    break;
            }

            Level = RelicCasterLevel;
            Flags = 0; // Remove Peace flag on new mob
            RespawnInterval = RelicCasterRespawnInterval;

            SetOwnBrain(new RelicCasterBrain());

            SetupByRealm(Realm);

            AddAbility(SkillBase.GetAbility(GS.Abilities.ConfusionImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.CCImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.RootImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.MezzImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.StunImmunity));

            return base.AddToWorld();
        }

        private void SetupByRealm(eRealm realm)
        {
            switch (realm)
            {
                case eRealm.Albion:
                    this.LoadEquipmentTemplateFromDatabase("relic_temple_caster_alb");
                    break;
                case eRealm.Midgard:
                    this.LoadEquipmentTemplateFromDatabase("relic_temple_caster_mid");
                    break;
                case eRealm.Hibernia:
                    this.LoadEquipmentTemplateFromDatabase("relic_temple_caster_hib");
                    break;
            }

            if (_relicSpells.TryGetValue(Realm, out Spell spell))
            {
                Spells = new List<Spell> { spell };
            }
        }

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

        public override void Die(GameObject killer)
        {
            int count = TempleRelicPadsLoader.GetEnemiesNearby(this);
            TempleRelicPadsLoader.SendTempleMessage($"{Name} has been killed with {count} enemies in the area.");
            base.Die(killer);
        }
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