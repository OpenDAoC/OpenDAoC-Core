using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Keeps;

namespace DOL.GS
{
    // New Frontiers Relic Temple Healer
    // You have to set the realm to the mob and reload the mob
    public class RelicHealer : GameNPC
    {
        private static readonly Dictionary<eRealm, Spell> _relicHeals = new Dictionary<eRealm, Spell>();

        protected const byte RelicHealerLevel = 65;
        protected const int RelicHealerRespawnInterval = 900000; // 15min

        static RelicHealer()
        {
            InitRelicHeals();
        }

        public override bool AddToWorld()
        {
            switch (Realm)
            {
                case eRealm.Albion:
                    Name = "Cleric";
                    Model = 35; // Briton (female)
                    break;
                case eRealm.Midgard:
                    Name = "Healer";
                    Model = 193; // Dwarf (female)
                    break;
                case eRealm.Hibernia:
                    Name = "Druid";
                    Model = 310; // Celt (female)
                    break;
            }

            Level = RelicHealerLevel;
            Flags = 0;
            RespawnInterval = RelicHealerRespawnInterval;
            Mana = MaxMana;

            SetOwnBrain(new RelicHealerBrain());
            SetupByRealm(Realm);

            // Immunitäten eines Relikt-Wächters
            AddAbility(SkillBase.GetAbility(GS.Abilities.ConfusionImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.CCImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.RootImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.MezzImmunity));
            AddAbility(SkillBase.GetAbility(GS.Abilities.StunImmunity));

            return base.AddToWorld();
        }

        private void SetupByRealm(eRealm realm)
        {
            string template = "relic_temple_healer_";
            switch (realm)
            {
                case eRealm.Albion: template += "alb"; break;
                case eRealm.Midgard: template += "mid"; break;
                case eRealm.Hibernia: template += "hib"; break;
            }
            this.LoadEquipmentTemplateFromDatabase(template);
        }

        private static void InitRelicHeals()
        {
            _relicHeals[eRealm.Albion] = CreateRelicHeal(1340, "Relic Prayer");
            _relicHeals[eRealm.Midgard] = CreateRelicHeal(3011, "Relic Restoration");
            _relicHeals[eRealm.Hibernia] = CreateRelicHeal(3030, "Relic Nature's Care");
        }

        private static Spell CreateRelicHeal(int effect, string name)
        {
            return new Spell(new DbSpell
            {
                Name = name,
                CastTime = 2,
                ClientEffect = effect,
                Value = 200,
                Range = 2000,
                Target = "REALM",
                Type = "Heal",
                Uninterruptible = true
            }, 0);
        }

        public Spell GetRelicHeal()
        {
            _relicHeals.TryGetValue(Realm, out Spell spell);
            return spell;
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
    public class RelicHealerBrain : StandardMobBrain
    {
        public RelicHealerBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 1500;
        }

        public override void Think()
        {
            if (Body == null || !Body.IsAlive) return;

            if (!Body.IsCasting)
            {
                if (CheckAreaForRelicHeals())
                    return;
            }
            else
            {
                if (Body.CurrentSpeed > 0) Body.StopMoving();
                return;
            }

            base.Think();
        }

        private bool CheckAreaForRelicHeals()
        {
            RelicHealer healer = Body as RelicHealer;
            if (healer == null) return false;
            GameLiving target = null;

            // Check for NPCs in 1500 radius
            foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
            {
                if (npc.IsAlive && npc.HealthPercent < 80 && npc.Realm == Body.Realm)
                {
                    target = npc;
                    break;
                }
            }

            if (target != null)
            {
                healer.TargetObject = target;
                return healer.CastSpell(healer.GetRelicHeal(), SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }

            return false;
        }
        public override bool CanAggroTarget(GameLiving target)
        {
            if (Body == null || target == null) return false;
            return GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
        }
    }
}