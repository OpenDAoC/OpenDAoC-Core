/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("Bolt")]
    public class BoltSpellHandler : SpellHandler
    {
        private bool _combatBlock;

        public BoltSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void FinishSpellCast(GameLiving target)
        {
            Caster.Mana -= PowerCost(target);

            if ((target is GameKeepDoor || target is GameKeepComponent) && Spell.SpellType != (byte) eSpellType.SiegeArrow && Spell.SpellType != (byte) eSpellType.SiegeDirectDamage)
            {
                MessageToCaster($"Your spell has no effect on the {target.Name}!", eChatType.CT_SpellResisted);
                return;
            }

            base.FinishSpellCast(target);
        }

        public override bool StartSpell(GameLiving target)
        {
            foreach (GameLiving livingTarget in SelectTargets(target))
            {
                if (livingTarget is GamePlayer playerTarget && Spell.Target.ToLower() == "cone")
                    playerTarget.Out.SendCheckLOS(Caster, playerTarget, LosCheckCallback);
                else
                    LaunchBolt(livingTarget);
            }

            return true;
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target.CurrentRegionID != Caster.CurrentRegionID)
                return;

            if (target.ObjectState != GameObject.eObjectState.Active)
                return;

            if (!target.IsAlive)
                return;

            AttackData ad = CalculateDamageToTarget(target, effectiveness);

            SendDamageMessages(ad);
            MessageToLiving(target, Spell.Message1, eChatType.CT_Spell); // "A bolt of runic energy hits you!"
            Message.SystemToArea(target, Util.MakeSentence(Spell.Message2, target.GetName(0, true)), eChatType.CT_System, target, Caster); // "{0} is hit by a bolt of runic energy!"

            DamageTarget(ad, false, ad.AttackResult == eAttackResult.Blocked ? 0x02 : 0x14);
            target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
        }

        public override int ModifyDamageWithTargetResist(AttackData ad, int damage)
        {
            // Modify half of the damage using magic resists. The other half is modified by the target's armor, or discarded if the target blocks.
            // Resources indicate that resistances aren't applied on the physical part of the damage.
            damage = base.ModifyDamageWithTargetResist(ad, damage / 2);

            if (!ad.Target.attackComponent.CheckBlock(null, ad, null, 0.0) || ad.Target.attackComponent.CheckGuard(ad, false, 0.0))
            {
                // This is normally set in 'AttackComponent.CalculateEnemyAttackResult', but we don't call it.
                if (ad.Target is GamePlayer playerTarget)
                    ad.ArmorHitLocation = playerTarget.CalculateArmorHitLocation(ad);

                // We need a fake weapon skill for the target's armor to have something to be compared with.
                // Since 'damage' is already modified by intelligence, power relics, spell variance, and everything else; we can use a constant only modified by the caster's level.
                double weaponSkill = Caster.attackComponent.CalculateModifiedWeaponSkill(ad.Target, Caster.Level * 5, 1.0, 1.0);
                double targetArmor = Caster.attackComponent.CalculateTargetArmor(ad.Target, ad.ArmorHitLocation);
                damage += (int) (weaponSkill / targetArmor * damage / 2);
            }
            else
            {
                ad.AttackResult = eAttackResult.Blocked;
                MessageToLiving(ad.Target, "You partially block " + Caster.GetName(0, false) + "'s spell!", eChatType.CT_Missed);
                MessageToCaster(ad.Target.GetName(0, true) + " blocks!", eChatType.CT_YouHit);
            }

            return damage;
        }

        public override int CalculateToHitChance(GameLiving target)
        {
            if (target is GameKeepDoor)
                return 0;

            int hitChance = base.CalculateToHitChance(target);

            if (Caster is GamePlayer && target is GamePlayer && target.InCombat)
            {
                // 200 unit range restriction added in 1.84.
                // Kept for OpenDAoC to make bolts a little friendlier.
                // Each attacker removes 20% chance to hit.
                foreach (GameLiving attacker in target.attackComponent.Attackers)
                {
                    if (attacker != Caster && target.GetDistanceTo(attacker) <= 200)
                    {
                        _combatBlock = true;
                        hitChance -= 20;
                    }
                }
            }

            // Use defense bonus from last executed style if any.
            AttackData targetAD = target.TempProperties.getProperty<AttackData>(GameLiving.LAST_ATTACK_DATA, null);

            if (targetAD?.AttackResult == eAttackResult.HitStyle && targetAD.Style != null)
                hitChance -= targetAD.Style.BonusToDefense;

            return hitChance;
        }

        public override void SendSpellResistAnimation(GameLiving target)
        {
            // Allowing an animation to be played here would show a second bolt.
            return;
        }

        public override void SendSpellResistMessages(GameLiving target)
        {
            if (_combatBlock)
            {
                MessageToCaster($"{target.Name} is in combat and your bolt misses!", eChatType.CT_YouHit);
                _combatBlock = false; // One spell handler can launch multiple bolts, so it needs to be reset (checked one at a time).
            }
            else
                MessageToCaster($"You miss!", eChatType.CT_YouHit);

            MessageToLiving(target, Caster.GetName(0, false) + " missed!", eChatType.CT_Missed);
        }

        public void BaseStartSpell(GameLiving target)
        {
            base.StartSpell(target);
        }

        private void LosCheckCallback(GamePlayer player, ushort response, ushort sourceOID, ushort targetOID)
        {
            if (player == null || sourceOID == 0 || targetOID == 0)
                return;

            if ((response & 0x100) == 0x100)
            {
                if (Caster.CurrentRegion.GetObject(targetOID) is GameLiving target)
                    LaunchBolt(target);
            }
        }

        private void LaunchBolt(GameLiving target)
        {
            int ticksToTarget = Caster.GetDistanceTo(target) * 1000 / 850; // 850 units per second.
            int delay = 1 + ticksToTarget / 100;

            foreach (GamePlayer playerInRadius in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                playerInRadius.Out.SendSpellEffectAnimation(Caster, target, m_spell.ClientEffect, (ushort) delay, false, 1);

            new BoltOnTargetTimer(target, this, ticksToTarget);
        }

        protected class BoltOnTargetTimer
        {
            private GameLiving _target;
            private BoltSpellHandler _spellHandler;

            public BoltOnTargetTimer(GameLiving target, BoltSpellHandler spellHandler, int ticksToTarget)
            {
                _target = target;
                _spellHandler = spellHandler;
                new ECSGameTimer(_spellHandler.Caster, new ECSGameTimer.ECSTimerCallback(Tick), ticksToTarget);
            }

            private int Tick(ECSGameTimer timer)
            {
                _spellHandler.BaseStartSpell(_target);
                return 0;
            }
        }
    }
}
