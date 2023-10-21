using Core.GS.Spells;

namespace Core.GS.Effects
{
	public class FocusShellEffect : GameSpellEffect
	{
		public FocusShellEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness) : base(handler, duration, pulseFreq, effectiveness) { }

		/// <summary>
		/// There is no duration!
		/// </summary>
		public new int RemainingTime
		{
			get
			{
				return 1;
			}
		}
	}
}
