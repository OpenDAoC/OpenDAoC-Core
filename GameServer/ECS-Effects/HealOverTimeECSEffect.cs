using DOL.GS.Spells;

namespace DOL.GS
{
    public class HealOverTimeECSGameEffect : ECSGameSpellEffect
    {
        public HealOverTimeECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams) 
        {
            NextTick = StartTick;
        }

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

        public override void OnEffectPulse()
        {
            ((HoTSpellHandler)SpellHandler).OnDirectEffect(Owner);
        }
    }
}
