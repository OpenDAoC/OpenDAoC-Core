using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class OfRaBunkerOfFaithEcsEffect : StatBuffEcsSpellEffect
{
    public override ushort Icon => 4242;
    public override string Name => "Bunker of Faith";
    public override bool HasPositiveEffect => true;

    public OfRaBunkerOfFaithEcsEffect(EcsGameEffectInitParams initParams) : base(initParams)
    {
        EffectType = EEffect.ArmorAbsorptionBuff;
    }
}