using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
	[SpellHandler("DamageSpeedDecreaseNoVariance")]
    public class DamageSpeedDecreaseNoVarianceSpell : DamageSpeedDecreaseSpell
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
        public DamageSpeedDecreaseNoVarianceSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}

namespace DOL.GS.Spells
{
    /// <summary>
    /// 
    /// </summary>
    [SpellHandler("DirectDamageNoVariance")]
    public class DirectDamageNoVarianceSpell : DirectDamageSpell
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
        public DirectDamageNoVarianceSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}

namespace DOL.GS.Spells
{
	/// <summary>
	/// UnresistableStun 
	/// </summary>
	[SpellHandler("UnresistableStun")]
	public class UnresistableStunSpell : StunSpell
	{

		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (effect.Owner == null) return 0;

			base.OnEffectExpires(effect, noMessages);

			return 0;
		}
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			return Spell.Duration;
		}
		public UnresistableStunSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}