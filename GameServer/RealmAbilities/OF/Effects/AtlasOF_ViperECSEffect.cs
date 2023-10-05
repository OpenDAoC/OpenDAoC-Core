using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_ViperECSEffect : EcsGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_ViperECSEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Viper;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4283; } }
        public override string Name { get { return "Viper"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            base.OnStartEffect();
            if(OwnerPlayer != null) OwnerPlayer.Out.SendMessage("The blood of the viper surges in your veins.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
        }

        public override void OnStopEffect()
        {
            if(OwnerPlayer != null) OwnerPlayer.Out.SendMessage("The blood of the viper fades from within.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            base.OnStopEffect();
        }
    }
}
