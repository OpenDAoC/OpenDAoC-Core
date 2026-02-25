using System.Collections.Generic;
using DOL.Database;
using DOL.GS;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    // New Frontiers Relic Temple Healer
    // NPC will also attack enemies if they are in range, but priority is to heal allies
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
            AddAbility(SkillBase.GetAbility(GS.Abilities.ConfusionImmunity));

            return base.AddToWorld();
        }

        private void SetupByRealm(eRealm realm)
        {
            switch (realm)
            {
                case eRealm.Albion:   LoadEquipmentTemplateFromDatabase("relic_temple_healer_alb"); break;
                case eRealm.Midgard:  LoadEquipmentTemplateFromDatabase("relic_temple_healer_mid"); break;
                case eRealm.Hibernia: LoadEquipmentTemplateFromDatabase("relic_temple_healer_hib"); break;
            }
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
                Radius = 250, // Small radius, players will get healed too
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
        protected const int CheckHealRange = 1500; // In this range the npc checks for heal targets

        // Cached once after the healer's static dictionary is populated — avoids a
        // dictionary lookup every think cycle.
        private Spell _cachedHealSpell;

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
            if (Body is not RelicHealer healer) return false;
            _cachedHealSpell ??= healer.GetRelicHeal();
            if (_cachedHealSpell == null) return false;
            GameLiving target = null;

            // Check for NPCs in 1500 radius
            foreach (GameNPC npc in Body.GetNPCsInRadius(CheckHealRange))
            {
                if (npc.IsAlive && npc.HealthPercent < 80 && npc.Realm == Body.Realm)
                {
                    target = npc;
                    break;
                }
            }
            // Heal players if no NPCs need healing
            if (target == null)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(CheckHealRange))
                {
                    if (player.IsAlive && player.HealthPercent < 80 && player.Realm == Body.Realm)
                    {
                        target = player;
                        break;
                    }
                }
            }

            if (target != null)
            {
                healer.TargetObject = target;
                return healer.CastSpell(_cachedHealSpell, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
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