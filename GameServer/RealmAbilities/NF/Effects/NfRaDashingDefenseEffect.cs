using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
    public class NfRaDashingDefenseEffect : StaticEffect, IGameEffect
    {
        //private Int64 m_startTick;
        private EcsGameTimer m_expireTimer;
        //private GamePlayer m_player;
        private Int32 m_effectDuration;

        // <summary>
        // The ability description
        // <//summary>
        protected const String delveString = "Ability that if successful will guard an attack meant for the ability's target. You will block in the target's place.";

        // <summary>
        // Holds guarder
        // <//summary>
        private GamePlayer m_guardSource;

        // <summary>
        // Gets guarder
        // <//summary>
        public GamePlayer GuardSource
        {
            get { return m_guardSource; }
        }

        // <summary>
        // Holds guarded player
        // <//summary>
        private GamePlayer m_guardTarget;

        // <summary>
        // Gets guarded player
        // <//summary>
        public GamePlayer GuardTarget
        {
            get { return m_guardTarget; }
        }

        // <summary>
        // Holds player group
        // <//summary>
        private GroupUtil m_playerGroup;

        // <summary>
        // Creates a new guard effect
        // <//summary>
        public NfRaDashingDefenseEffect()
        {
        }

        public const int GUARD_DISTANCE = 1000;

        // <summary>
        // Start the guarding on player
        // <//summary>
        // <param name="guardSource">The guarder<//param>
        // <param name="guardTarget">The player guarded by guarder<//param>
        public void Start(GamePlayer guardSource, GamePlayer guardTarget, int duration)
        {
            if (guardSource == null || guardTarget == null)
                return;

            m_playerGroup = guardSource.Group;

            if (m_playerGroup != guardTarget.Group)
                return;

            m_guardSource = guardSource;
            m_guardTarget = guardTarget;
            // Set the duration & start the timers
            m_effectDuration = duration;
            StartTimers();

            m_guardSource.EffectList.Add(this);
            m_guardTarget.EffectList.Add(this);

            if (!guardSource.IsWithinRadius(guardTarget, NfRaDashingDefenseEffect.GUARD_DISTANCE))
            {
                guardSource.Out.SendMessage(string.Format("You are now guarding {0}, but you must stand closer.", guardTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                guardTarget.Out.SendMessage(string.Format("{0} is now guarding you, but you must stand closer.", guardSource.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            }
            else
            {
                guardSource.Out.SendMessage(string.Format("You are now guarding {0}.", guardTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                guardTarget.Out.SendMessage(string.Format("{0} is now guarding you.", guardSource.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            }
            guardTarget.TempProperties.SetProperty(RealmAbilities.NfRaDashingDefenseAbility.Dashing, true);
        }

        // <summary>
        // Called when effect must be canceled
        // <//summary>
        public override void Cancel(bool playerCancel)
        {
            //Stop Timers
            StopTimers();
            m_guardSource.EffectList.Remove(this);
            m_guardTarget.EffectList.Remove(this);

            m_guardTarget.TempProperties.RemoveProperty(RealmAbilities.NfRaDashingDefenseAbility.Dashing);

            m_guardSource.Out.SendMessage(string.Format("You are no longer guarding {0}.", m_guardTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            m_guardTarget.Out.SendMessage(string.Format("{0} is no longer guarding you.", m_guardSource.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);

            m_playerGroup = null;
        }

        // <summary>
        // Starts the timers for this effect
        // <//summary>
        private void StartTimers()
		{
			StopTimers();
			m_expireTimer = new EcsGameTimer(GuardSource, new EcsGameTimer.EcsTimerCallback(ExpireCallback), m_effectDuration * 1000);
		}

		/// <summary>
		/// Stops the timers for this effect
		/// </summary>
		private void StopTimers()
		{

			if (m_expireTimer != null)
			{
				m_expireTimer.Stop();
				m_expireTimer = null;
			}
		}

        // <summary>
        // Remaining Time of the effect in milliseconds
        // <//summary>
		private int ExpireCallback(EcsGameTimer timer)
		{
			Cancel(false);

			return 0;
		}

        // <summary>
        // Effect Name
        // <//summary>
        public override string Name
		{
			get
			{
				return "Dashing Defense";
			}
		}

        /// <summary>
		/// Remaining time of the effect in milliseconds
		/// </summary>
		public override Int32 RemainingTime
		{
			get
			{
				EcsGameTimer timer = m_expireTimer;
				if (timer == null || !timer.IsAlive)
					return 0;
				return timer.TimeUntilElapsed;
			}
		}

        /// <summary>
		/// Icon ID
		/// </summary>
		public override UInt16 Icon
		{
			get
			{
				return 3032;
			}
		}

        // <summary>
        // Delve Info
        // <//summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var delveInfoList = new List<string>(4);
                delveInfoList.Add(delveString);
                delveInfoList.Add(" ");
                delveInfoList.Add(GuardSource.GetName(0, true) + " is guarding " + GuardTarget.GetName(0, false));
                return delveInfoList;
            }
        }
    }
}
