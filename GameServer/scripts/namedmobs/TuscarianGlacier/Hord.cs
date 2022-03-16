using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;

namespace DOL.GS.Scripts
{
    public class Hord : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Hord()
            : base()
        {
        }

        public virtual int HordDifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        
        public override bool AddToWorld()
        {
            Name = "Council Hord";
            Model = 918;
            Size = 70;
            Level = 77;
            // Giant
            BodyType = 5;
            ScalingFactor = 45;
            
            HordBrain sBrain = new HordBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }

        public override int MaxHealth
        {
            get
            {
                return 20000 * HordDifficulty / 100;
            }
        }

        public override int AttackRange
        {
            get
            {
                return 450;
            }
            set
            {
            }
        }
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000 * HordDifficulty / 100;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85 * HordDifficulty / 100;
        }
        public override void Die(GameObject killer)
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");
            
            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,5000);
                }
            }
            base.Die(killer);
        }
        
        #region Damage & Heal Events

        /// <summary>
        /// Take some amount of damage inflicted by another GameObject.
        /// </summary>
        /// <param name="source">The object inflicting the damage.</param>
        /// <param name="damageType">The type of damage.</param>
        /// <param name="damageAmount">The amount of damage inflicted.</param>
        /// <param name="criticalAmount">The critical amount of damage inflicted</param>
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            Brain.Notify(GameObjectEvent.TakeDamage, this,
                new TakeDamageEventArgs(source, damageType, damageAmount, criticalAmount));
        }
        #endregion
        
    }
}
namespace DOL.AI.Brain
{
    public class HordBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected String m_HealAnnounce;
        
        public HordBrain()
            : base()
        {
            m_HealAnnounce = "{0} heals his wounds.";
            AggroLevel = 200;
            AggroRange = 1500; //so players cant just pass him without aggroing
        }
        
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
            }
        }
        
        public override void Think()
        {
            /*if (Body.TargetObject != null && Body.InCombat && Body.Health != Body.MaxHealth && HasAggro)
            {
                if (Util.Chance(1))
                {
                    new RegionTimer(Body, new RegionTimerCallback(CastHeal), 1000);
                }
            }*/
            base.Think();
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);

            if (e == GameNPCEvent.TakeDamage)
            {
                if (Body.TargetObject != null && Body.InCombat && Body.Health != Body.MaxHealth && HasAggro)
                {
                    if (Util.Chance(3))
                    {
                        new RegionTimer(Body, new RegionTimerCallback(CastHeal), 1000);
                    }
                }
            }
        }

        /// <summary>
        /// Cast Heal on itself
        /// </summary>
        /// <param name="timer">The timer that started this cast.</param>
        /// <returns></returns>
        private int CastHeal(RegionTimer timer)
        {
            BroadcastMessage(String.Format(m_HealAnnounce, Body.Name));
            Body.CastSpell(Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }
        
        protected Spell m_healSpell;
        /// <summary>
        /// The Heal spell.
        /// </summary>
        protected Spell Heal
        {
            get
            {
                if (m_healSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.Power = 0;
                    spell.ClientEffect = 3011;
                    spell.Icon = 3011;
                    spell.TooltipId = 3011;
                    spell.Damage = 0;
                    spell.Name = "Minor Emendation";
                    spell.Range = 0;
                    spell.SpellID = 3011;
                    spell.Duration = 0;
                    spell.Value = 500;
                    spell.SpellGroup = 130;
                    spell.Target = "Self";
                    spell.Type = "Heal";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_healSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_healSpell);
                }
                return m_healSpell;
            }
        }
    }
}