using Core.GS.Effects;
using Core.GS.Spells;

namespace Core.GS.ECS;

public class EcsPulseEffect : EcsGameSpellEffect, IConcentrationEffect
{
    public override string OwnerName => $"Pulse: {SpellHandler.Spell.Name}";

    public EcsPulseEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
        : base (new EcsGameEffectInitParams(owner, duration, effectiveness, handler))
    {
        PulseFreq = pulseFreq;
        CancelEffect = cancelEffect;
        EffectType = EEffect.Pulse;
        ExpireTick = pulseFreq + GameLoop.GameLoopTime;
        StartTick = GameLoop.GameLoopTime;
        LastTick = 0;

        EffectService.RequestStartEffect(this);
    }
}