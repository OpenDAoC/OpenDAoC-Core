namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CurePoison)]
    public class CurePoisonSpellHandler : RemoveSpellEffectHandler
    {
        public override string ShortDescription => "All nearsight effects are removed from the target.";

        public CurePoisonSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            m_spellTypesToRemove = ["DamageOverTime", "StyleBleeding"];
        }
    }
}
