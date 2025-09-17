using DOL.GS.Spells;

namespace DOL.GS.Scripts
{
    [SpellHandler(eSpellType.CureNearsightCustom)]
    public class CureNearsightCustomSpellHandler : RemoveSpellEffectHandler
    {
        private Spell _spell;

        public override string ShortDescription => "All nearsight effects are removed from the target. This spell's cast time is not influenced by stats.";

        public CureNearsightCustomSpellHandler(GameLiving caster, Spell spell, SpellLine line): base(caster, spell, line)
        {
            // RR4: now it's a list
            m_spellTypesToRemove = ["Nearsight", "Silence"];
            this._spell = spell;
        }

        public override int CalculateCastingTime()
        {
            return _spell.CastTime;
        }
    }
}
