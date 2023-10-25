using Core.GS.Enums;

namespace Core.GS.ECS;

public class PiercingMagicEcsSpellEffect : EcsGameSpellEffect
{
    public PiercingMagicEcsSpellEffect(EcsGameEffectInitParams initParams)
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