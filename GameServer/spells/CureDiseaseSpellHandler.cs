namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CureDisease)]
    public class CureDiseaseSpellHandler : RemoveSpellEffectHandler
    {
        public override string ShortDescription => "All disease effects are removed from the target.";

        public CureDiseaseSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            m_spellTypesToRemove = ["Disease"];
        }
    }
}
