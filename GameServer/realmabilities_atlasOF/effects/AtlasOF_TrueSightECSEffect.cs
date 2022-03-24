using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_TrueSightECSEffect : ECSGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_TrueSightECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.TrueSight;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 3012; } }
        public override string Name { get { return "True Sight"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
