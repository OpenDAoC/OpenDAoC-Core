
using DOL.GS;
using DOL.GS.Effects;

public class IncomingHealBoostECSEffect : ECSGameSpellEffect
{
    public IncomingHealBoostECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
        EffectType = eEffect.IncomingHealBonus;
    }
}