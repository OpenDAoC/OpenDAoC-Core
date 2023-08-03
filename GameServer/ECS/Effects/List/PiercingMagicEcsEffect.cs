﻿namespace DOL.GS
{
    public class PiercingMagicEcsEffect : EcsGameSpellEffect
    {
        public PiercingMagicEcsEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.PiercingMagic;
        }

        public override void OnStartEffect()
        {
            //Owner.Effectiveness += (SpellHandler.Spell.Value / 100);
            OnEffectStartsMsg(Owner, true, false, true);

        }

        public override void OnStopEffect()
        {
             //Owner.Effectiveness -= (SpellHandler.Spell.Value / 100);
             OnEffectExpiresMsg(Owner, true, false, true);
        }
    }
}