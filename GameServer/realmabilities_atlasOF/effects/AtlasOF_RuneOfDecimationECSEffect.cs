using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
    public class AtlasOF_RuneOfDecimationECSEffect : ECSGameSpellEffect
    {
        public AtlasOF_RuneOfDecimationECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.RuneOfDecimation;
            NextTick = 1;
            PulseFreq = 1000;
        }

        public override ushort Icon => 7153;
        public override string Name => "Rune Of Decimation";
        public override bool HasPositiveEffect => false;

        public override void OnEffectPulse()
        {
            bool shouldDetonate = false;
            GameLiving triggeringLiving = null;
            List<GameLiving> DetonateTargets = new();

            foreach (GamePlayer player in Owner?.GetPlayersInRadius((ushort)SpellHandler.Spell.Range))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Owner, player, true))
                {
                    shouldDetonate = true;
                    triggeringLiving ??= player;
                    DetonateTargets.Add(player);
                }
            }

            foreach (GameNPC living in Owner.GetNPCsInRadius((ushort)SpellHandler.Spell.Range))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Owner, living, true))
                {
                    shouldDetonate = true;
                    triggeringLiving ??= living;
                    DetonateTargets.Add(living);
                }
            }

            if (shouldDetonate)
            {
                foreach (GamePlayer player in Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    player.Out.SendMessage($"{SpellHandler.Caster.Name}'s Rune of Decimation trap detonates!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }

                GamePlayer playerCaster = SpellHandler.Caster as GamePlayer;

                foreach (GameLiving target in DetonateTargets)
                {
                    if (triggeringLiving == null)
                        continue;

                    AttackData ad = new()
                    {
                        Attacker = SpellHandler.Caster,
                        Target = target,
                        AttackType = AttackData.eAttackType.Spell,
                        SpellHandler = SpellHandler,
                        AttackResult = eAttackResult.HitUnstyled,
                        IsSpellResisted = false,
                        Damage = CalculateDamageWithFalloff((int) SpellHandler.Spell.Damage, triggeringLiving, target),
                        DamageType = SpellHandler.Spell.DamageType
                    };

                    ad.Modifier = (int) (ad.Damage * ad.Target.GetResist(ad.DamageType) / -100.0);
                    ad.Damage += ad.Modifier;

                    if (target is GamePlayer playerTarget)
                    {
                        playerTarget.Out.SendMessage($"You take {ad.Damage}({ad.Modifier}) damage from a Rune of Decimation!", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                        playerTarget.Out.SendSpellCastAnimation(Owner, 7153, 1);
                    }

                    playerCaster?.Out.SendMessage($"Your Rune of Decimation deals {ad.Damage}({ad.Modifier}) damage to {target?.Name}!", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                    target.DealDamage(ad);
                }

                playerCaster?.Out.SendSpellCastAnimation(Owner, 7153, 1);
                Owner.Die(Owner);
            }
        }

        private static int CalculateDamageWithFalloff(int initialDamage, GameLiving initTarget, GameLiving aeTarget)
        {
            int modDamage = (int) Math.Round((decimal) (initialDamage * ((500 - initTarget.GetDistance(new Point2D(aeTarget.X, aeTarget.Y))) / 500.0)));
            return modDamage;
        }
    }
}
