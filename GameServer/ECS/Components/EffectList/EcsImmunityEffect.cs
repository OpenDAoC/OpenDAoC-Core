using Core.GS.Spells;

namespace Core.GS.ECS;

public class EcsImmunityEffect : EcsGameSpellEffect
{
    public EcsImmunityEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
        : base(new EcsGameEffectInitParams(owner, duration, effectiveness, handler))
    {
        // Some of this is already done in the base constructor and should be cleaned up
        Owner = owner;
        SpellHandler = handler;
        Duration = duration;
        PulseFreq = pulseFreq;
        Effectiveness = effectiveness;
        CancelEffect = cancelEffect;
        EffectType = MapImmunityEffect();
        ExpireTick = duration + GameLoop.GameLoopTime;
        StartTick = GameLoop.GameLoopTime;
        LastTick = 0;
        TriggersImmunity = false;

        EffectService.RequestStartEffect(this);
    }

    protected EcsImmunityEffect(EcsGameEffectInitParams initParams) : base(initParams) { }

    protected EEffect MapImmunityEffect()
    {
        switch (SpellHandler.Spell.SpellType)
        {
            case ESpellType.Mesmerize:
                return EEffect.MezImmunity;
            case ESpellType.StyleStun:
            case ESpellType.Stun:
                return EEffect.StunImmunity;
            case ESpellType.SpeedDecrease:
            case ESpellType.DamageSpeedDecreaseNoVariance:
            case ESpellType.DamageSpeedDecrease:
                return EEffect.SnareImmunity;
            case ESpellType.Nearsight:
                return EEffect.NearsightImmunity;
            default:
                return EEffect.Unknown;
        }
    }
}

public class NpcEcsStunImmunityEffect : EcsImmunityEffect
{
    private int _timesStunned = 1;

    public NpcEcsStunImmunityEffect(EcsGameEffectInitParams initParams) : base(initParams)
    {
        Owner = initParams.Target;
        Duration = 60000;
        EffectType = EEffect.NPCStunImmunity;
        EffectService.RequestStartEffect(this);
    }

    public long CalculateStunDuration(long duration)
    {
        var retVal = duration / (2 * _timesStunned);
        _timesStunned++;
        return retVal;
    }
}

public class NpcEcsMezImmunityEffect : EcsImmunityEffect
{
    private int _timesMezzed = 1;

    public NpcEcsMezImmunityEffect(EcsGameEffectInitParams initParams) : base(initParams)
    {
        Owner = initParams.Target;
        Duration = 60000;
        EffectType = EEffect.NPCMezImmunity;
        EffectService.RequestStartEffect(this);
    }

    public long CalculateMezDuration(long duration)
    {
        var retVal = duration / (2 * _timesMezzed);
        _timesMezzed++;
        return retVal;
    }
}