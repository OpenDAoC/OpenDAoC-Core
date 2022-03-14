/*
<author>Kelt</author>
 */
using DOL.AI;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using System;
using System.Reflection;


namespace DOL.GS.Scripts
{

    public abstract class GameEpicMistress : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Set Mistress of Runes difficulty in percent of its max abilities
        /// 100 = full strength
        /// </summary>
        public virtual int MistressDifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }

        /// <summary>
        /// Announcements for Aoe Spear and Nearsight.
        /// </summary>
        protected String[] m_AoEAnnounce;
        protected String m_NearsightAnnounce;
        protected String m_DeathAnnounce;

        /// <summary>
        /// Creates a new instance of GameEpicMistress.
        /// </summary>
        public GameEpicMistress()
            : base()
        {
            m_AoEAnnounce = new String[] { "{0} casts a magical flaming spear on {1}!",
                "{0} drops a flaming spear from above!",
                "{0} uses all her might to create a flaming spear.",
                "{0} casts a dangerous spell!" };
            m_NearsightAnnounce = "{1} can no longer see properly and everyone in the vicinity!";
            m_DeathAnnounce = "{0} has been killed and loses her power.";
            MaxDistance = 2500;
            TetherRange = 2500;
            SetOwnBrain(new MistressBrain());
        }


        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000 * MistressDifficulty / 100;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85 * MistressDifficulty / 100;
        }

        public override short MaxSpeedBase
        {
            get { return (short)(191 + (Level * 2)); }
            set { m_maxSpeedBase = value; }
        }
        public override int MaxHealth
        {
            get
            {
                return 15000 * MistressDifficulty / 100;
            }
        }

        public override short Strength
        {
            get
            {
                return (short)(base.Strength * MistressDifficulty / 100);
            }
        }

        public override int AttackRange
        {
            get { return 180; }
            set { }
        }

        public override bool HasAbility(string keyName)
        {
            if (IsReturningHome && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }

        public double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * 2.0 * MistressDifficulty / 100;
        }

        public override int RespawnInterval
        {
            get
            {
                //25min Respawn
                int result = (25 * 600) * 100;
                return result;
            }
        }

        /// <summary>
        /// Return to spawn point, Mistress of Runes can't be attacked while it's
        /// on it's way.
        /// </summary>
        public override void WalkToSpawn()
        {
            EvadeChance = 100;
            WalkToSpawn(MaxSpeed);
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (EvadeChance == 100)
                return;

            base.OnAttackedByEnemy(ad);
        }

        /// <summary>
        /// Handle event notifications.
        /// </summary>
        /// <param name="e">The event that occured.</param>
        /// <param name="sender">The sender of the event.</param>
        public override void Notify(DOLEvent e, object sender)
        {
            base.Notify(e, sender);
            // When Mistress of Runes arrives at its spawn point, make it vulnerable again.

            if (e == GameNPCEvent.ArriveAtTarget)
                EvadeChance = 0;
        }

        /// <summary>
        /// Invoked when Mistress of Runes dies.
        /// </summary>
        /// <param name="killer">The living that got the killing blow.</param>
        public override void Die(GameObject killer)
        {
            if (killer == null)
                log.Error("Mistress of Runes Killed: killer is null!");
            else
                log.Debug("Mistress of Runes Killed: killer is " + killer.Name + ", attackers:");
            base.StopCurrentSpellcast();
            base.Die(killer);

            BroadcastMessage(String.Format(m_DeathAnnounce, Name, killer.Name));
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

        /// <summary>
        /// Take action upon someone healing the enemy.
        /// </summary>
        /// <param name="enemy">The living that was healed.</param>
        /// <param name="healSource">The source of the heal.</param>
        /// <param name="changeType">The way the living was healed.</param>
        /// <param name="healAmount">The amount that was healed.</param>
        public override void EnemyHealed(GameLiving enemy, GameObject healSource, eHealthChangeType changeType, int healAmount)
        {
            base.EnemyHealed(enemy, healSource, changeType, healAmount);
            Brain.Notify(GameLivingEvent.EnemyHealed, this,
                new EnemyHealedEventArgs(enemy, healSource, changeType, healAmount));
        }

        #endregion



        #region Custom Methods

        /// <summary>
        /// Broadcast relevant messages to the raid.
        /// </summary>
        /// <param name="message">The message to be broadcast.</param>
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        #endregion
     
        #region AoE Spell / Check

        protected Spell m_AoESpell;
        private GameLiving m_AoETarget;       
        private const int m_AoEChance = 5;

        /// <summary>
        /// The AoE Spear spell. Override this property in your Mistress of Runes implementation
        /// and assign the spell to m_AoESpell.
        /// </summary>
        protected abstract Spell AoESpear
        {
            get;
        }

        /// <summary>
        /// Chance to cast AoE Spear when a potential target has been detected.
        /// </summary>
        protected int AoEChance
        {
            get { return m_AoEChance; }
        }


        /// <summary>
        /// The target for the next AoE cast.
        /// </summary>
        private GameLiving AoETarget
        {
            get { return m_AoETarget; }
            set { m_AoETarget = value; PrepareToAoE(); }
        }

        /// <summary>
        /// Check whether or not to cast AoE.
        /// </summary>
        public bool CheckAoESpear(GameLiving target)
        {
            if (target == null || AoETarget != null) return false;
            bool success = Util.Chance(AoEChance);
            if (success)
                AoETarget = target;
            return success;
        }

        /// <summary>
        /// Announce the AoE Spear and start the 4 second timer.
        /// </summary>
        private void PrepareToAoE()
        {
            if (AoETarget == null) return;
            TurnTo(AoETarget);
            int messageNo = Util.Random(1, m_AoEAnnounce.Length) - 1;
            BroadcastMessage(String.Format(m_AoEAnnounce[messageNo], Name, AoETarget.Name));
            new RegionTimer(this, new RegionTimerCallback(CastAoE), 4000);
        }

        /// <summary>
        /// Cast AoE on the raid (AoE damage).
        /// </summary>
        /// <param name="timer">The timer that started this cast.</param>
        /// <returns></returns>
        private int CastAoE(RegionTimer timer)
        {
            // Turn around to the target and cast AoE Spear, then go back to the original
            // target, if one exists.

            GameObject oldTarget = TargetObject;
            TargetObject = AoETarget;
            Z = SpawnPoint.Z; // this is a fix to correct Z errors that sometimes happen during Mistress fights
            TurnTo(AoETarget);
            CastSpell(AoESpear, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            AoETarget = null;
            if (oldTarget != null) TargetObject = oldTarget;
            return 0;
        }

        #endregion

        #region Nearsight Spell / Check

        protected Spell m_NearsightSpell;
        private GameLiving m_NearsightTarget;
        private const int m_NearsightChance = 2;

        /// <summary>
        /// The Nearsight spell. Override this property in your Mistress of Runes implementation
        /// and assign the spell to m_NearsightSpell.
        /// </summary>
        protected abstract Spell Nearsight
        {
            get;
        }

        /// <summary>
        /// Chance to cast Nearsight when a potential target has been detected.
        /// </summary>
        protected int NearsightChance
        {
            get { return m_NearsightChance; }
        }

        /// <summary>
        /// The target for the next Nearsight cast.
        /// </summary>
        private GameLiving NearsightTarget
        {
            get { return m_NearsightTarget; }
            set { m_NearsightTarget = value; PrepareToNearsight(); }
        }

        /// <summary>
        /// Check whether or not to Nearsight at this target.
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns>Whether or not the spell was cast.</returns>
        public bool CheckNearsight(GameLiving target)
        {
            if (target == null || NearsightTarget != null) return false;
            bool success = Util.Chance(NearsightChance);
            if (success)
                NearsightTarget = target;
            return success;
        }

        /// <summary>
        /// Announce the Nearsight and start the 1 second timer.
        /// </summary>
        private void PrepareToNearsight()
        {
            if (NearsightTarget == null) return;
            TurnTo(NearsightTarget);
            BroadcastMessage(String.Format(m_NearsightAnnounce, Name, NearsightTarget.Name));
            new RegionTimer(this, new RegionTimerCallback(CastNearsight), 1000);
        }

        /// <summary>
        /// Cast Nearsight on the target.
        /// </summary>
        /// <param name="timer">The timer that started this cast.</param>
        /// <returns></returns>
        private int CastNearsight(RegionTimer timer)
        {
            // Turn around to the target and cast Nearsight, then go back to the original
            // target, if one exists.

            GameObject oldTarget = TargetObject;
            TargetObject = NearsightTarget;
            Z = SpawnPoint.Z; // this is a fix to correct Z errors that sometimes happen during Mistress fights
            TurnTo(NearsightTarget);
            CastSpell(Nearsight, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            NearsightTarget = null;
            if (oldTarget != null) TargetObject = oldTarget;
            return 0;
        }

        #endregion      
    }
}


