namespace DOL.GS
{
    public class AblativeArmorECSGameEffect : ECSGameSpellEffect
    {
        public int RemainingValue { get; set; }

        public AblativeArmorECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            RemainingValue = (int)SpellHandler.Spell.Value;
        }

        public override void OnStartEffect()
        {
            // "A crystal shield covers you."
            // "A crystal shield covers {0}'s skin."
            OnEffectStartsMsg(Owner, true, false, true);
        }

        public override void OnStopEffect()
        {
            // "Your crystal shield fades."
            // "{0}'s crystal shield fades."
            OnEffectExpiresMsg(Owner, true, false, true);
        }
    }
}
