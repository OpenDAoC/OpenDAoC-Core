﻿namespace DOL.GS
{
    public class CombatHealEcsEffect : EcsGameSpellEffect
    {
        public CombatHealEcsEffect(ECSGameEffectInitParams initParams) : base(initParams) 
        {
            NextTick = StartTick;
        }

        public override void OnStartEffect()
        {
            // "You start healing faster."
            // "{0} starts healing faster."
            OnEffectStartsMsg(Owner, true, true, true);
        }

        public override void OnStopEffect()
        {
            //"Your meditative state fades."
            //"{0}'s meditative state fades."
            OnEffectExpiresMsg(Owner, true, false, true);
        }
    }
}
