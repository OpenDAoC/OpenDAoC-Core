using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Spell handler for Facilitate Painworking.
    /// </summary>
    /// <author>Aredhel</author>
    [SpellHandlerAttribute("FacilitatePainworking")]
    class FacilitatePainworking : SpellHandler
    {
        public FacilitatePainworking(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new FacilitatePainworkingECSGameEffect(initParams);
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new FacilitatePainworkingEffect(this, CalculateEffectDuration(target, effectiveness), 0, effectiveness);
        }
    }
}
