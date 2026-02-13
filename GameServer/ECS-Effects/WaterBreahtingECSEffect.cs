using System;
using DOL.GS.Effects;

namespace DOL.GS.Effects
{
    public class WaterBreathingECSEffect : ECSGameSpellEffect
    {
        public WaterBreathingECSEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            base.OnStartEffect();

            if (Owner is GamePlayer player)
            {
                player.CanBreathUnderWater = true;
                player.BaseBuffBonusCategory[eProperty.WaterSpeed] += (int)SpellHandler.Spell.Value;
                player.OnMaxSpeedChange();

                OnEffectStartsMsg(true, true, true);
            }
        }

        public override void OnStopEffect()
        {
            if (Owner is GamePlayer player)
            {
                player.CanBreathUnderWater = false;
                player.BaseBuffBonusCategory[eProperty.WaterSpeed] -= (int)SpellHandler.Spell.Value;
                player.OnMaxSpeedChange();
            }
            base.OnStopEffect();
        }
    }
}