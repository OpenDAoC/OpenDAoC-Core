using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.GS.Spells;

namespace Core.GS.Effects
{
    public class OfRaViperEcsEffect : EcsGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public OfRaViperEcsEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.Viper;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4283; } }
        public override string Name { get { return "Viper"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            base.OnStartEffect();
            if(OwnerPlayer != null) OwnerPlayer.Out.SendMessage("The blood of the viper surges in your veins.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
        }

        public override void OnStopEffect()
        {
            if(OwnerPlayer != null) OwnerPlayer.Out.SendMessage("The blood of the viper fades from within.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
            base.OnStopEffect();
        }
    }
}
