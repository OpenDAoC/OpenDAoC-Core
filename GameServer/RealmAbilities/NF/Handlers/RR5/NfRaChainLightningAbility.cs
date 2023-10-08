using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerRules;

namespace DOL.GS.RealmAbilities
{
    public class NfRaChainLightningAbility : Rr5RealmAbility
    {
        public NfRaChainLightningAbility(DbAbility dba, int level) : base(dba, level) { }

        private double modifier;
        private int damage;
        private int resist;
        private int basedamage;
        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>
        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            bool deactivate = false;
            AbstractServerRules rules = GameServer.ServerRules as AbstractServerRules;
            GamePlayer player = living as GamePlayer;
            GamePlayer target = living.TargetObject as GamePlayer;
            if (player.TargetObject == null || target == null)
            {
                player.Out.SendMessage("You must target a player to launch this spell!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                return;
            }
            if (!GameServer.ServerRules.IsAllowedToAttack(living, target, true))
            {
                player.Out.SendMessage("You must select an enemy target!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                return;
            }

            if (!living.IsWithinRadius( target, (int)(1500 * living.GetModified(EProperty.SpellRange) * 0.01)))
            {
                MessageUtil.ChatToOthers(living, "You are too far away from your target to use this ability!", EChatType.CT_SpellResisted);
                return;
            }
            SendCasterSpellEffectAndCastMessage(living, 3505, true);
            DamageTarget(target, living, 0);
            deactivate = true;
            GamePlayer m_oldtarget = target;
            GamePlayer m_newtarget = null;
            for (int x = 1; x < 5; x++)
            {
                if (m_newtarget != null)
                    m_oldtarget = m_newtarget;
                foreach (GamePlayer p in m_oldtarget.GetPlayersInRadius(500))
                {
                    if (p != m_oldtarget && p != living && GameServer.ServerRules.IsAllowedToAttack(living, p, true))
                    {
                        DamageTarget(p, living, x);
						p.StartInterruptTimer(3000, EAttackType.Spell, living);
                        m_newtarget = p;
                        break;
                    }
                }
            }
            if(deactivate)
            DisableSkill(living);
        }
        private void DamageTarget(GameLiving target, GameLiving caster, double counter)
        {
            int level = caster.GetModifiedSpecLevel("Stormcalling");
            if (level > 50)
                level = 50;
            modifier = 0.5 + (level * 0.01) * Math.Pow(0.75, counter);
            basedamage = (int)(450 * modifier);
            resist = basedamage * target.GetResist(EDamageType.Energy) / -100;
            damage = basedamage + resist;

            GamePlayer player = caster as GamePlayer;
            if (player != null)
                player.Out.SendMessage("You hit " + target.Name + " for " + damage + "(" + resist + ") points of damage!", EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);

            GamePlayer targetPlayer = target as GamePlayer;
            if (targetPlayer != null)
            {
                if (targetPlayer.IsStealthed)
                    targetPlayer.Stealth(false);
            }

            foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                p.Out.SendSpellEffectAnimation(caster, target, 3505, 0, false, 1);
                p.Out.SendCombatAnimation(caster, target, 0, 0, 0, 0, 0x14, target.HealthPercent);
            }

            //target.TakeDamage(caster, eDamageType.Spirit, damage, 0);
            AttackData ad = new AttackData();
            ad.AttackResult = EAttackResult.HitUnstyled;
            ad.Attacker = caster;
            ad.Target = target;
            ad.DamageType = EDamageType.Energy;
            ad.Damage = damage;
            target.OnAttackedByEnemy(ad);
            caster.DealDamage(ad);
        }
        public override int GetReUseDelay(int level)
        {
            return 600;
        }
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Casts a lightning bolt at the enemy target and hits up to 5 targets. If there is only one target available, the spell will hit once. If there are multiple targets, the spell has a chance to jump from target to target and back to the prior target. With each jump, the damage of the spell is reduced by 25%.");
            list.Add("");
            list.Add("Range: 1500");
            list.Add("Target: Realm Enemy");
            list.Add("Casting time: instant");
        }

    }
}