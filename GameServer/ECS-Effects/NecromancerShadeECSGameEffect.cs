using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class NecromancerShadeECSGameEffect : ShadeECSGameEffect
    {
		public NecromancerShadeECSGameEffect(ECSGameEffectInitParams initParams)
			: base(initParams) { }

		protected int m_timeRemaining = -1;

		/// <summary>
		/// Remaining time of the effect in seconds.
		/// </summary>
		public override long GetRemainingTimeForClient()
		{
			return (m_timeRemaining < 0) ? 0 : m_timeRemaining * 1000;
		}
		/// <summary>
		/// Set timer when pet is out of range.
		/// </summary>
		/// <param name="seconds"></param>
		public void SetTetherTimer(int seconds)
		{
			m_timeRemaining = seconds;
		}
	}
}
