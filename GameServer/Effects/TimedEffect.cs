using System.Collections.Generic;
using DOL.Language;

namespace DOL.GS.Effects
{
	/// <summary>
	/// base for all effects that are timed and should stop on itself
	/// </summary>
	public class TimedEffect : StaticEffect
	{
		private readonly object m_LockObject = new object();

		protected int m_duration;

		/// <summary>
		/// The timer that will cancel the effect
		/// </summary>
		protected EcsGameTimer m_expireTimer;

		/// <summary>
		/// create timed effect that will last the given timespan in milliseconds
		/// </summary>
		/// <param name="timespan"></param>
		public TimedEffect(int timespan)
		{
			m_duration = timespan;
		}

		/// <summary>
		/// Start the timed effect on target
		/// </summary>
		/// <param name="target">The effect target</param>
		public override void Start(GameLiving target)
		{
			lock (m_LockObject)
			{
				if (m_expireTimer == null)
				{
					m_expireTimer = new EcsGameTimer(target, new EcsGameTimer.EcsTimerCallback(ExpiredCallback), m_duration);
				}
				base.Start(target);
			}
		}

		/// <summary>
		/// Stop the timed effect on owner
		/// </summary>
		public override void Stop()
		{
			lock (m_LockObject)
			{
				if (m_expireTimer != null)
				{
					m_expireTimer.Stop();
					m_expireTimer = null;
				}
				base.Stop();
			}
		}

		private int ExpiredCallback(EcsGameTimer timer)
		{
			Stop();
			return 0;
		}

		/// <summary>
		/// Remaining Time of the effect in milliseconds
		/// </summary>
		public override int RemainingTime
		{
			get
			{
				EcsGameTimer timer = m_expireTimer;
				if (timer == null || !timer.IsAlive)
					return 0;
				return timer.TimeUntilElapsed;
			}
		}

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();

				int seconds = RemainingTime / 1000;
				if (seconds > 0)
				{
					list.Add(" "); //empty line
					if (seconds > 60)
						list.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.DelveInfo.MinutesRemaining", (seconds / 60), (seconds % 60).ToString("00")));
					else
						list.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.DelveInfo.SecondsRemaining", seconds));
				}
				return list;
			}
		}
	}
}
