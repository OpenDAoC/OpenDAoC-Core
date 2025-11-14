namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.PreventFlight)]
    public class PreventFlightSpellHandler : SpellHandler
    {
        public PreventFlightSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string ShortDescription => $"The target is slowed by {Spell.Value}%.";

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, static (in ECSGameEffectInitParams i) => new StatDebuffECSEffect(i));
        }

        public override bool HasConflictingEffectWith(ISpellHandler compare)
        {
            return false;
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            return Spell.Duration;
        }

        protected override double GetDebuffEffectivenessCriticalModifier()
        {
            return 1.0;
        }
    }
}
