namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.UnbreakableSpeedDecrease)]
    public class UnbreakableSpeedDecreaseSpellHandler : SpeedDecreaseSpellHandler
    {
        public UnbreakableSpeedDecreaseSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        protected override double GetDebuffEffectivenessCriticalModifier()
        {
            // Not sure why unbreakable snares aren't allowed to crit.
            return 1.0;
        }
    }
}
