using DOL.GS.Spells;

namespace DOL.GS.Effects
{
	/// <summary>
	/// The effect associated with the UniPortal teleport spell.
	/// </summary>
	public class UniPortalEffect : GameSpellEffect
	{
		/// <summary>
		/// Create a new portal effect.
		/// </summary>
		public UniPortalEffect(ISpellHandler handler, int duration)
			: base(handler, duration, 0)
		{
		}
	}
}