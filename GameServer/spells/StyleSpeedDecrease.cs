using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Style speed decrease effect spell handler
	/// </summary>
	[SpellHandler("StyleSpeedDecrease")]
	public class StyleSpeedDecrease : SpeedDecreaseSpellHandler
	{
		public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
		{
			return new StatDebuffECSEffect(initParams);
		}
		
		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			return Spell.Duration;
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			base.OnEffectExpires(effect, noMessages);
			return 0;
		}

		// constructor
		public StyleSpeedDecrease(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
