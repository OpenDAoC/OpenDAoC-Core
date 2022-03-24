using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;

namespace DOL.GS.Scripts
{
    public class Ozur : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Ozur()
            : base()
        {
        }

        public virtual int OzurDifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        
        public override bool AddToWorld()
        {
            Name = "Council Ozur";
            Model = 918;
            Size = 70;
            Level = 77;
            // Giant
            BodyType = 5;
            ScalingFactor = 45;
            
            OzurBrain sBrain = new OzurBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }

        public override int MaxHealth
        {
            get
            {
                return 20000 * OzurDifficulty / 100;
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
            return 1000 * OzurDifficulty / 100;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85 * OzurDifficulty / 100;
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
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,ServerProperties.Properties.EPIC_ORBS);
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
    public class OzurBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static int _GettingFirstPlayerStage = 50;
        private static int _GettingSecondPlayerStage = 100;
        private static int _GettingNonZerkedStage = _GettingFirstPlayerStage - 1;
        private const int m_value = 20;
        private const int min_value = 0;
        public OzurBrain()
            : base()
        {
            AggroLevel = 200;
            AggroRange = 1000;
        }
        
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
            }
        }
        
        private void Resists(bool isNotZerked)
        {
            if (isNotZerked)
            {
                Body.AbilityBonus[(int)eProperty.Resist_Body] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Heat] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Cold] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Matter] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Energy] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Spirit] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Slash] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Crush] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Thrust] = m_value;
            }
            else
            {
                Body.AbilityBonus[(int)eProperty.Resist_Body] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Heat] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Cold] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Matter] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Energy] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Spirit] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Slash] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Crush] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Thrust] = min_value;
            }
        }
        
        private void Weak(bool weak)
        {
            if(weak)
            {
                Body.AbilityBonus[(int)eProperty.Resist_Body] = min_value - 20;
                Body.AbilityBonus[(int)eProperty.Resist_Heat] = min_value - 20;
                Body.AbilityBonus[(int)eProperty.Resist_Cold] = min_value - 20;
                Body.AbilityBonus[(int)eProperty.Resist_Matter] = min_value - 20;
                Body.AbilityBonus[(int)eProperty.Resist_Energy] = min_value - 20;
                Body.AbilityBonus[(int)eProperty.Resist_Spirit] = min_value - 20;
                Body.AbilityBonus[(int)eProperty.Resist_Slash] = min_value - 20;
                Body.AbilityBonus[(int)eProperty.Resist_Crush] = min_value - 20;
                Body.AbilityBonus[(int)eProperty.Resist_Thrust] = min_value - 20;
            }
        }
        
        public override void Think()
        {
            if (Body.TargetObject != null && Body.InCombat && Body.Health != Body.MaxHealth && HasAggro)
            {
                int countPlayer = 0;
                foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
                {
                    countPlayer++;
                }
                
                if (countPlayer <= _GettingNonZerkedStage)
                {
                    Resists(true);
                }

                if (countPlayer >= _GettingFirstPlayerStage && countPlayer < _GettingSecondPlayerStage)
                {
                    Body.ScalingFactor += 10;
                    Body.Strength += 50;
                    Resists(false);
                }

                if (countPlayer >= _GettingSecondPlayerStage)
                {
                    Body.ScalingFactor += 25;
                    Body.Strength += 150;
                    Weak(true);
                }
            }
            base.Think();
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);
        }
    }
}