namespace DOL.GS
{
    public class CombatHealECSEffect : ECSGameSpellEffect
    {
        public CombatHealECSEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            // "You start healing faster."
            // "{0} starts healing faster."
            OnEffectStartsMsg(true, true, true);
        }

        public override void OnStopEffect()
        {
            //"Your meditative state fades."
            //"{0}'s meditative state fades."
            OnEffectExpiresMsg(true, false, true);
        }
    }
}
