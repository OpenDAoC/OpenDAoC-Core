using System;
using Core.GS.Spells;

namespace Core.GS.Effects
{
	/// <summary>
	/// Spell Effect assists SpellHandler with duration spells with immunity
	/// </summary>
	public class GameSpellAndImmunityEffect : GameSpellEffect
	{
		/// <summary>
		/// The amount of times this effect started
		/// </summary>
		protected volatile int m_startedCount;

		/// <summary>
		/// Creates a new game spell effect
		/// </summary>
		/// <param name="handler"></param>
		/// <param name="duration"></param>
		/// <param name="pulseFreq"></param>
		public GameSpellAndImmunityEffect(ISpellHandler handler, int duration, int pulseFreq) : this(handler, duration, pulseFreq, 1)
		{
		}

		/// <summary>
		/// Creates a new game spell effect
		/// </summary>
		/// <param name="handler"></param>
		/// <param name="duration"></param>
		/// <param name="pulseFreq"></param>
		/// <param name="effectiveness"></param>
		public GameSpellAndImmunityEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness) : base(handler, duration, pulseFreq, effectiveness)
		{
			m_startedCount = 0;
		}

		/// <summary>
		/// Starts the timers for this effect
		/// </summary>
		protected override void StartTimers()
		{
			if (!IsExpired)
			{
				int duration = Duration;
				int startcount = m_startedCount;
				if (startcount > 0)
				{
					duration /= Math.Min(20, startcount*2);
					if (duration < 1) duration = 1;
				}
				Duration = duration;
				m_startedCount++;
			}
			base.StartTimers();
		}

		/// <summary>
		/// Gets the amount of times this effect started
		/// </summary>
		public int StartedCount
		{
			get { return m_startedCount; }
		}
	}
}
