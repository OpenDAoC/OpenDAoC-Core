using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities;

public class OfRaForestheartAmbusherEcsEffect : EcsGameAbilityEffect
{
    public OfRaForestheartAmbusherEcsEffect(EcsGameEffectInitParams initParams) : base(initParams)
    {
        EffectType = EEffect.ForestheartAmbusher;
        EffectService.RequestStartEffect(this);
    }

    public override ushort Icon => 4268;
    public override string Name => "Forestheart Ambusher";
    public override bool HasPositiveEffect => true;
    public SummonAnimistAmbusherSpell PetSpellHander;

    public override void OnStartEffect()
    {
        SpellLine RAspellLine = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        Spell ForestheartAmbusher = SkillBase.GetSpellByID(90802);

        if (ForestheartAmbusher != null)
            Owner.CastSpell(ForestheartAmbusher, RAspellLine);

        base.OnStartEffect();
    }

    public override void OnStopEffect()
    {
        // The effect can be cancelled before the spell if fired by the casting service, in which case 'PetSpellHander' can be null.
        PetSpellHander?.Pet.TakeDamage(null, EDamageType.Natural, int.MaxValue, 0);
        base.OnStopEffect();
    }

    public void Cancel(bool playerCancel)
    {
        EffectService.RequestImmediateCancelEffect(this, playerCancel);
        OnStopEffect();
    }
}