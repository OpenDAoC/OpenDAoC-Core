using System;
using DOL.GS;

public class AtlantisTabletMorphECSEffect : MorphECSEffect
{
    public AtlantisTabletMorphECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
        EffectType = eEffect.Morph;
    }
}