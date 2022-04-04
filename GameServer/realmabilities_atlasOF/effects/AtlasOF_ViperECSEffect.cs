using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_ViperECSEffect : ECSGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_ViperECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Viper;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4283; } }
        public override string Name { get { return "Viper"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
