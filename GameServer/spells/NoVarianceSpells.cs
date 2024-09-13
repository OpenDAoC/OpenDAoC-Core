using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.DirectDamageNoVariance)]
    public class DirectDamageNoVarianceSpellHandler : DirectDamageSpellHandler
    {
        public DirectDamageNoVarianceSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override double CalculateDamageBase(GameLiving target)
        {
            return Spell.Damage;
        }

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            min = 1.00;
            max = 1.00;
        }
    }

    [SpellHandler(eSpellType.DirectDamageWithDebuffNoVariance)]
    public class DirectDamageDebuffNoVariance : DirectDamageDebuffSpellHandler
    {
        public DirectDamageDebuffNoVariance(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override double CalculateDamageBase(GameLiving target)
        {
            return Spell.Damage;
        }

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            min = 1.00;
            max = 1.00;
        }
    }

    [SpellHandler(eSpellType.DamageOverTimeNoVariance)]
    public class DoTSpellHandlerNoVariance : DoTSpellHandler
    {
        public DoTSpellHandlerNoVariance(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override double CalculateDamageBase(GameLiving target)
        {
            return Spell.Damage;
        }

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            min = 1.00;
            max = 1.00;
        }
    }

    [SpellHandler(eSpellType.DamageSpeedDecreaseNoVariance)]
    public class DamageSpeedDecreaseNoVarianceSpellHandler : DamageSpeedDecreaseSpellHandler
    {
        public DamageSpeedDecreaseNoVarianceSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override double CalculateDamageBase(GameLiving target)
        {
            return Spell.Damage;
        }

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            min = 1.00;
            max = 1.00;
        }
    }

    [SpellHandler(eSpellType.LifedrainNoVariance)]
    public class LifedrainNoVarianceSpellHandler : LifedrainSpellHandler
    {
        public LifedrainNoVarianceSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override double CalculateDamageBase(GameLiving target)
        {
            return Spell.Damage;
        }

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            min = 1.00;
            max = 1.00;
        }
    }

    [SpellHandler(eSpellType.UnresistableStun)]
    public class UnresistableStunSpellHandler : StunSpellHandler
    {
        public UnresistableStunSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner == null)
                return 0;

            base.OnEffectExpires(effect, noMessages);
            return 0;
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            return Spell.Duration;
        }
    }
}
