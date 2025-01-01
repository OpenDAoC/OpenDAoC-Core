using System.Collections.Generic;
using System.Threading;
using DOL.Language;

namespace DOL.GS.Effects
{
	/// <summary>
	/// base for all effects that are timed and should stop on itself
	/// </summary>
	public class TimedEffect : StaticEffect
	{
		private readonly Lock _lockObject = new();

		protected int m_duration;

		/// <summary>
		/// The timer that will cancel the effect
		/// </summary>
		protected ECSGameTimer m_expireTimer;

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
			lock (_lockObject)
			{
				if (m_expireTimer == null)
				{
					m_expireTimer = new ECSGameTimer(target, new ECSGameTimer.ECSTimerCallback(ExpiredCallback), m_duration);
				}
				base.Start(target);
			}
		}

		/// <summary>
		/// Stop the timed effect on owner
		/// </summary>
		public override void Stop()
		{
			lock (_lockObject)
			{
				if (m_expireTimer != null)
				{
					m_expireTimer.Stop();
					m_expireTimer = null;
				}
				base.Stop();
			}
		}

		private int ExpiredCallback(ECSGameTimer timer)
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
				ECSGameTimer timer = m_expireTimer;
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
