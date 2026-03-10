namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CurePoison)]
    public class CurePoisonSpellHandler : RemoveSpellEffectHandler
    {
        public override string ShortDescription => "All damage over time effects (such as poisons) are removed from the target.";

        public CurePoisonSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            m_spellTypesToRemove = ["DamageOverTime", "StyleBleeding"];
        }
    }
}
