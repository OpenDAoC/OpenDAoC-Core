namespace DOL.GS
{
    public class AblativeArmorECSGameEffect : ECSGameSpellEffect
    {
        public int RemainingValue { get; set; }

        public AblativeArmorECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            RemainingValue = (int) (SpellHandler.Spell.Value * Effectiveness);
        }

        public override void OnStartEffect()
        {
            // "A crystal shield covers you."
            // "A crystal shield covers {0}'s skin."
            OnEffectStartsMsg(true, false, true);
        }

        public override void OnStopEffect()
        {
            // "Your crystal shield fades."
            // "{0}'s crystal shield fades."
            OnEffectExpiresMsg(true, false, true);
        }
    }
}
