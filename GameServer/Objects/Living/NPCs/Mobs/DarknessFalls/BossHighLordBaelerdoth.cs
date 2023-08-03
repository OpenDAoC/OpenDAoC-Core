﻿using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using log4net;

namespace DOL.GS
{
    public class BossHighLordBaelerdoth : GameEpicBoss
    {
        private static new readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("High Lord Baelerdoth initialized..");
        }
        public BossHighLordBaelerdoth()
            : base()
        {
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 40; // dmg reduction for melee dmg
                case EDamageType.Crush: return 40; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 100000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162129);
            LoadTemplate(npcTemplate);

            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;

            // demon
            BodyType = 2;
            RespawnInterval = ServerProperties.ServerProperties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

            BaelerdothBrain sBrain = new BaelerdothBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100  * ServerProperties.ServerProperties.EPICS_DMG_MULTIPLIER;
        }
        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override void Die(GameObject killer)
        {
            base.Die(killer);
        }
        public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (HealthPercent < 25)
            {
                CastSpell(AbsDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
        #region pbaoe abs debuff

        /// <summary>
        /// The Bomb spell.
        /// and assign the spell to m_breathSpell.
        /// </summary>
        ///
        /// 
        protected Spell m_absDebuffSpell;

        /// <summary>
        /// The Bomb spell.
        /// </summary>
        protected Spell AbsDebuff
        {
            get
            {
                if (m_absDebuffSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Duration = 20;
                    spell.ClientEffect = 9606;
                    spell.Icon = 9606;
                    spell.Damage = 0;
                    spell.Value = 15;
                    spell.Name = "Aura of Baelerdoth";
                    spell.Range = 1500;
                    spell.Radius = 350;
                    spell.SpellID = 99998;
                    spell.Target = "Enemy";
                    spell.Type = "ArmorAbsorptionDebuff";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) EDamageType.Spirit;
                    m_absDebuffSpell = new Spell(spell, 50);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_absDebuffSpell);
                }

                return m_absDebuffSpell;
            }
        }

        #endregion
    }
}

namespace DOL.AI.Brain
{
    public class BaelerdothBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public BaelerdothBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            base.Think();
        }
    }
}