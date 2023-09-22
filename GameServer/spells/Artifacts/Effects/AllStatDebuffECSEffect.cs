using DOL.GS;

public class AllStatDebuffECSEffect : StatDebuffECSEffect
{
    public AllStatDebuffECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
        EffectType = eEffect.AllStatDebuff;
    }
}

public class AllStatPercentDebuffECSEffect : StatDebuffECSEffect
{
    public AllStatPercentDebuffECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
        EffectType = eEffect.AllStatPercentDebuff;
    }
}