/*
 * author: Kelt
 * Name: Uaimh Lairmaster
 * Server: Atlas Freeshard
 */

using System;
using System.Collections;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.Behaviour.Actions;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts.DOL.AI.Brain;
using DOL.GS.Styles;
using FiniteStateMachine;

namespace DOL.GS.Scripts
{
    public class UaimhLairmaster : GameNPC
    {
        protected String m_FleeingAnnounce;
        public static bool IsFleeing = true;

        public UaimhLairmaster() : base()
        {
            m_FleeingAnnounce = "{0} starts fleeing!";
        }

        public override bool AddToWorld()
        {
            Model = 844;
            Name = "Uaimh Lairmaster";
            Size = 60;
            Level = 81;
            Gender = eGender.Neutral;

            BodyType = 6; // Humanoid
            RoamingRange = 0;
            base.AddToWorld();
            base.SetOwnBrain(new UaimhLairmasterBrain());
            return true;
        }

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
        public override void EnemyHealed(GameLiving enemy, GameObject healSource, eHealthChangeType changeType,
            int healAmount)
        {
            base.EnemyHealed(enemy, healSource, changeType, healAmount);
            Brain.Notify(GameLivingEvent.EnemyHealed, this,
                new EnemyHealedEventArgs(enemy, healSource, changeType, healAmount));
        }

        #region Tether

        /// <summary>
        /// Return to spawn point, Uaimh Lairmaster can't be attacked while it's
        /// on it's way.
        /// </summary>
        public override void WalkToSpawn()
        {
            UaimhLairmasterBrain brain = new UaimhLairmasterBrain();
            StopAttack();
            StopFollowing();
            brain.AggroTable.Clear();
            EvadeChance = 100;
            WalkToSpawn(MaxSpeed);
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (EvadeChance == 100)
                return;

            base.OnAttackedByEnemy(ad);
        }

        #region Broadcast Message

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

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);

            if (e == GameObjectEvent.TakeDamage)
            {
                if (CheckHealth()) return;
            }

            if (e == GameNPCEvent.ArriveAtTarget)
                EvadeChance = 0;
            
        }

        #endregion

        #region Health Check

        /// <summary>
        /// Actions to be taken into consideration when health drops.
        /// </summary>
        /// <returns>Whether any action was taken.</returns>
        public bool CheckHealth()
        {
            if (HealthPercent <= 60 && IsFleeing)
            {
                BroadcastMessage(String.Format(m_FleeingAnnounce, Name));
                WalkToSpawn();
                IsFleeing = false;
                return true;
            }

            return false;
        }

        #endregion
    }

    namespace DOL.AI.Brain
    {
        public class UaimhLairmasterBrain : StandardMobBrain
        {
            protected byte MAX_Size = 100;
            protected byte MIN_Size = 60;

            protected String m_AggroAnnounce;
            public static bool IsAggroEnemies = true;

            private static readonly log4net.ILog log =
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            public UaimhLairmasterBrain() : base()
            {
                m_AggroAnnounce = "{0} feels threatened and appears more menacing!";
            }

            public override void Think()
            {
                if (Body.InCombat && Body.IsAlive && HasAggro)
                {
                    if (Body.TargetObject != null)
                    {
                        if (IsAggroEnemies)
                        {
                            //Starts Growing
                            GrowSize();
                        }
                    }
                }
                else
                {
                    //Starts Shrinking
                    ShrinkSize();
                }

                base.Think();
            }

            #region Broadcast Message

            /// <summary>
            /// Broadcast relevant messages to the raid.
            /// </summary>
            /// <param name="message">The message to be broadcast.</param>
            public void BroadcastMessage(String message)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
                {
                    player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
                }
            }

            #endregion

            public void GrowSize()
            {
                BroadcastMessage(String.Format(m_AggroAnnounce, Body.Name));
                Body.Size = MAX_Size;
                IsAggroEnemies = false;
            }

            public void ShrinkSize()
            {
                Body.Size = MIN_Size;
                IsAggroEnemies = true;
            }
        }
    }
}