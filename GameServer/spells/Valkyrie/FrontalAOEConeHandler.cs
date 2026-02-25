using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Handler to make the frontal pulsing cone show the effect animation on every pulse
    /// </summary>
    [SpellHandler(eSpellType.FrontalPulseConeDD)]
    public class FrontalAOEConeHandler : DirectDamageSpellHandler
    {
        public FrontalAOEConeHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void OnSpellPulse(PulsingSpellEffect effect)
        {
            SendCastAnimation(0);
            base.OnSpellPulse(effect);
        }
    }
}
