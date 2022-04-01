using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_MajesticWillECSEffect : ECSGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_MajesticWillECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.MajesticWill;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4239; } }
        public override string Name { get { return "Majestic Will"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
