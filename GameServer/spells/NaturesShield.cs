namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.NaturesShield)]
    public class NaturesShieldSpellHandler : SpellHandler
    {
        public override string ShortDescription => $"Gives the user {Spell.Value}% chance to block attacks while this style is prepared.";

        public NaturesShieldSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
}
