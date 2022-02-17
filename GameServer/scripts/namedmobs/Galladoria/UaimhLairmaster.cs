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
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS.Scripts
{
    public class UaimhLairmaster : GameNPC
    {
        public UaimhLairmaster() : base()
        {
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
    }
}

namespace DOL.AI.Brain
{
    public class UaimhLairmasterBrain : StandardMobBrain
    {
        protected byte MAX_Size = 100;
        protected byte MIN_Size = 60;

        protected String m_AggroAnnounce;
        protected String m_FleeingAnnounce;

        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public UaimhLairmasterBrain() : base()
        {
            m_AggroAnnounce = "{0} feels threatened and appears more menacing!";
            m_FleeingAnnounce = "{0} starts fleeing!";
        }

        public static bool IsFleeing = true;
        public static bool IsAggroEnemies = true;

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

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);

            if (e == GameNPCEvent.ArriveAtTarget)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(3000))
                {
                    if (Body.IsVisibleTo(player) && player.IsAlive && player.IsAttackable)
                    {
                        Body.EvadeChance = 0;
                        Body.StartAttack(player);
                    }
                }
            }
        }
        
        #region Custom Methods

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