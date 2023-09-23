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

using System;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("StarsProc")]
    public class StarsProc : SpellHandler
    {
        private StarsProc subHandler1;
        private StarsProc subHandler2;
        
        public override bool StartSpell(GameLiving target)
        {
            if (Spell.SubSpellID != 0 && SkillBase.GetSpellByID(Spell.SubSpellID) != null)
            {
                subHandler1 = ScriptMgr.CreateSpellHandler(Caster, SkillBase.GetSpellByID(Spell.SubSpellID), SkillBase.GetSpellLine("Mob Spells")) as StarsProc;

                if (subHandler1 != null && subHandler1.Spell.SubSpellID != 0 && SkillBase.GetSpellByID(subHandler1.Spell.SubSpellID) != null)
                {
                    subHandler2 = ScriptMgr.CreateSpellHandler(Caster, SkillBase.GetSpellByID(subHandler1.Spell.SubSpellID), SkillBase.GetSpellLine("Mob Spells")) as StarsProc;
                }
            }
            
            foreach (GameLiving targ in SelectTargets(target))
            {
                DealDamage(targ);
            }

            return true;
        }
        
        private void DealDamage(GameLiving target)
        {
           Target = target;
           subHandler1.Target = target;
           subHandler2.Target = target;
            
            FireBolt(Caster, target, this);
            new ECSGameTimer(Caster, CastBolt2).Start(100);
            new ECSGameTimer(Caster, CastBolt3).Start(200);
        }

        private int CastBolt2(ECSGameTimer timer)
        {
            FireBolt(Caster, Target, subHandler1);
            return 0;
        }

        private int CastBolt3(ECSGameTimer timer)
        {
            FireBolt(Caster, Target, subHandler2);
            return 0;
        }
        
        private void FireBolt(GameLiving caster, GameLiving target, StarsProc handler)
        {
            if (handler == null) return;
            int ticksToTarget = caster.GetDistanceTo(target) * 100 / 85; // 85 units per 1/10s
            int delay = 1 + ticksToTarget / 100;
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(caster, target, handler.Spell.ClientEffect, (ushort)(delay), false, 1);
            }
            BoltOnTargetAction bolt = new BoltOnTargetAction(caster, target, handler);
            bolt.Start(1 + ticksToTarget);
        }

        public override void FinishSpellCast(GameLiving target)
        {
            if (target is Keeps.GameKeepDoor || target is Keeps.GameKeepComponent)
            {
                MessageToCaster("Your spell has no effect on the keep component!", eChatType.CT_SpellResisted);
                return;
            }
            base.FinishSpellCast(target);
        }
        
        protected class BoltOnTargetAction : ECSGameTimerWrapperBase
        {
            protected readonly GameLiving m_boltTarget;
            protected readonly StarsProc m_handler;
            
            public BoltOnTargetAction(GameLiving actionSource, GameLiving boltTarget, StarsProc spellHandler) : base(actionSource)
            {
                if (boltTarget == null)
                    throw new ArgumentNullException("boltTarget");
                if (spellHandler == null)
                    throw new ArgumentNullException("spellHandler");
                m_boltTarget = boltTarget;
                m_handler = spellHandler;
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                GameLiving target = m_boltTarget;
                GameLiving caster = (GameLiving) timer.Owner;

                if (target == null || !target.IsAlive || target.ObjectState != GameObject.eObjectState.Active || target.CurrentRegionID != caster.CurrentRegionID)
                    return 0;

                m_handler.Effectiveness = 1;
                AttackData ad = m_handler.CalculateDamageToTarget(target);
                ad.Damage = (int)m_handler.Spell.Damage;
                m_handler.SendDamageMessages(ad);
                m_handler.DamageTarget(ad, false);

                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, caster);
                return 0;
            }
        }

        public StarsProc(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
    }

    [SpellHandlerAttribute("StarsProc2")]
    public class StarsProc2 : SpellHandler
    {
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            new BandOfStarsMorphECSEffect(new ECSGameEffectInitParams(target, Spell.Duration, 1, this));
            if (target.Realm == 0 || Caster.Realm == 0)
            {
                target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
            }
            else
            {
                target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
            }
            if(target is GameNPC) 
            {
                IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
            }
        }

        public StarsProc2(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
