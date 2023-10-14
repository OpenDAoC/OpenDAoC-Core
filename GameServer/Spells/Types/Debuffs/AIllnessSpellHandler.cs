namespace DOL.GS.Spells
{
	/// <summary>
	/// Contains all common code for illness spell handlers (and negative spell effects without animation) 
	/// </summary>
	public class AIllnessSpellHandler : SpellHandler
	{
		public override bool HasPositiveEffect 
		{
			get 
			{ 
				return false;
			}
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
			double modifier = 1.0;
			RealmAbilities.NfRaVeilRecoveryAbility ab = target.GetAbility<RealmAbilities.NfRaVeilRecoveryAbility>();
			if (ab != null)
				modifier -= ((double)ab.Amount / 100);

			return (int)((double)Spell.Duration * modifier); 
		}

		public AIllnessSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	
	}
}
