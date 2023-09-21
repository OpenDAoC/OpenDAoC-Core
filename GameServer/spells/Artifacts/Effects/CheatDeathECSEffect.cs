
using DOL.GS;

public class CheatDeathECSEffect : ECSGameSpellEffect
{
    public CheatDeathECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
        EffectType = eEffect.CheatDeath;
    }
}