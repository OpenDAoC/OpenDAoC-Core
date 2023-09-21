using DOL.GS;

public class AlvarusMorphECSEffect : MorphECSEffect
{
    public AlvarusMorphECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
        TickInterval = 500;
        NextTick = GameLoop.GameLoopTime;
        this.EffectType = eEffect.Morph;
        EffectService.RequestStartEffect(this);
    }

    public override void OnEffectPulse()
    {
        if(Owner is GamePlayer {IsSwimming: false, IsUnderwater: false})
            EffectService.RequestCancelEffect(this);
    }
}