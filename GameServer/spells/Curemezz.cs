namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CureMezz)]
    public class CureMezzSpellHandler : RemoveSpellEffectHandler
    {
        public override string ShortDescription => "All mesmerization effects are removed from the target.";

        public CureMezzSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            m_spellTypesToRemove = ["Mesmerize"];
        }
    }
}
