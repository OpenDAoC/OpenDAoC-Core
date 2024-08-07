using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
	[SpellHandlerAttribute("DamageSpeedDecreaseNoVariance")]
    public class DamageSpeedDecreaseNoVarianceSpellHandler : DamageSpeedDecreaseSpellHandler
	{
		public override double CalculateDamageBase(GameLiving target)
		{
			return Spell.Damage;
		}
		public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
		{
			min = 1.00;
			max = 1.00;
		}
        public DamageSpeedDecreaseNoVarianceSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}

namespace DOL.GS.Spells
{
    /// <summary>
    /// 
    /// </summary>
    [SpellHandlerAttribute("DirectDamageNoVariance")]
    public class DirectDamageNoVarianceSpellHandler : DirectDamageSpellHandler
    {
		public override double CalculateDamageBase(GameLiving target)
        {
            return Spell.Damage;
        }
        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            min = 1.00;
            max = 1.00;
        }
        public DirectDamageNoVarianceSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}

namespace DOL.GS.Spells
{
    /// <summary>
    /// UnresistableStun 
    /// </summary>
    [SpellHandlerAttribute("UnresistableStun")]
	public class UnresistableStunSpellHandler : StunSpellHandler
	{
		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (effect.Owner == null) return 0;

			base.OnEffectExpires(effect, noMessages);

			return 0;
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			return Spell.Duration;
		}

		public UnresistableStunSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
