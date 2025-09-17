namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CureNearsight)]
    public class CureNearsightSpellHandler : RemoveSpellEffectHandler
    {
        public override string ShortDescription => "All nearsight effects are removed from the target.";

        public CureNearsightSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            m_spellTypesToRemove = ["Nearsight", "Silence"];
        }
    }
}
