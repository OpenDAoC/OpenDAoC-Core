using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_RuneOfDecimationECSEffect : ECSGameSpellEffect
    {
        public AtlasOF_RuneOfDecimationECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.RuneOfDecimation;
            this.NextTick = 1;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 3019; } }
        public override string Name { get { return "Rune Of Decimation"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnEffectPulse()
        {
            bool shouldDetonate = false;
            List<GameLiving> DetonateTargets = new List<GameLiving>();
            foreach (GamePlayer player in Owner?.GetPlayersInRadius((ushort)SpellHandler.Spell.Range))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Owner, player, true))
                {
                    shouldDetonate = true;
                    DetonateTargets.Add(player);
                }
            }

            foreach (GameNPC living in Owner.GetNPCsInRadius((ushort)SpellHandler.Spell.Range))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Owner, living, true))
                {
                    shouldDetonate = true;
                    DetonateTargets.Add(living);
                }
            }

            if (shouldDetonate)
            {
                foreach (GamePlayer i_player in Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    i_player.Out.SendMessage($"{SpellHandler.Caster}'s Rune of Decimation trap detonates!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
                
                foreach (var target in DetonateTargets)
                {
                    target.TakeDamage(SpellHandler.Caster, SpellHandler.Spell.DamageType, (int) SpellHandler.Spell.Damage, 0);
                }
                
                Owner.Die(Owner);
            }
            else
            {
                NextTick = 1000; //check again in a second
            }
            
            
            base.OnEffectPulse();
        }
    }
}
