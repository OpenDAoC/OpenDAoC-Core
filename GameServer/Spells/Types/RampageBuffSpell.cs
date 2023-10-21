namespace Core.GS.Spells
{
    [SpellHandler("Rampage")]
    public class RampageBuffSpell : SpellHandler
    {
        public RampageBuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
