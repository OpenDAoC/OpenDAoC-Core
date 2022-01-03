using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class ResurrectionIllnessECSGameEffect : ECSGameSpellEffect
    {
        public ResurrectionIllnessECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            GamePlayer gPlayer = Owner as GamePlayer;
            if (gPlayer != null)
            {
                gPlayer.Effectiveness -= SpellHandler.Spell.Value * 0.01;
                gPlayer.Out.SendUpdateWeaponAndArmorStats();
                gPlayer.Out.SendStatusUpdate();
            }
        }

        public override void OnStopEffect()
        {
            GamePlayer gPlayer = Owner as GamePlayer;
            if (gPlayer != null)
            {
                gPlayer.Effectiveness += SpellHandler.Spell.Value * 0.01;
                gPlayer.Out.SendUpdateWeaponAndArmorStats();
                gPlayer.Out.SendStatusUpdate();
            }
        }
    }
}
