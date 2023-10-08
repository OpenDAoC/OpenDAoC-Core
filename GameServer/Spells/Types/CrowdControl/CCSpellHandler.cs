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
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Abstract CC spell handler
    /// </summary>
    public abstract class AbstractCCSpellHandler : ImmunityEffectSpellHandler
    {
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.CCImmunity))
            {
                MessageToCaster(target.Name + " is immune to this effect!", eChatType.CT_SpellResisted);
                return;
            }

            if (target.EffectList.GetOfType<ChargeEffect>() != null || target.TempProperties.GetProperty("Charging", false))
            {
                MessageToCaster(target.Name + " is moving too fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner == null)
                return 0;

            base.OnEffectExpires(effect, noMessages);

            if (effect.Owner is GamePlayer player)
            {
                player.Client.Out.SendUpdateMaxSpeed();

                if (player.Group != null)
                    player.Group.UpdateMember(player, false, false);
            }

            effect.Owner.Notify(GameLivingEvent.CrowdControlExpired, effect.Owner);
            return (effect.Name == "Pet Stun") ? 0 : 60000;
        }

        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = base.CalculateEffectDuration(target, effectiveness);

            // [Atlas - Takii] Disabling MOC effectiveness scaling in OF.
            // double mocFactor = 1.0;
            // MasteryofConcentrationEffect moc = Caster.EffectList.GetOfType<MasteryofConcentrationEffect>();
            // if (moc != null)
            // {
            //     AtlasOF_MasteryofConcentration ra = Caster.GetAbility<AtlasOF_MasteryofConcentration>();
            //     if (ra != null)
            //         mocFactor = System.Math.Round((double)ra.GetAmountForLevel(ra.Level) / 100, 2);
            //     duration = (double)Math.Round(duration * mocFactor);
            // }

            if (Spell.SpellType != ESpellType.StyleStun)
            {
                // capping duration adjustment to 100%, live cap unknown - Tolakram
                int hitChance = Math.Min(200, CalculateToHitChance(target));

                if (hitChance <= 0)
                {
                    duration = 0;
                }
                else if (hitChance < 55)
                {
                    duration -= (int)(duration * (55 - hitChance) * 0.01);
                }
                else if (hitChance > 100)
                {
                    duration += (int)(duration * (hitChance - 100) * 0.01);
                }
            }

            return (int)duration;
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            int resistChance;

            /*
            GameSpellEffect fury = SpellHandler.FindEffectOnTarget(target, "Fury");
            if (fury != null)
            {
                resist += (int)fury.Spell.Value;
            }*/

            // Bonedancer RR5.
            if (target.EffectList.GetOfType<AllureofDeathEffect>() != null)
                return AllureofDeathEffect.ccchance;

            if (m_spellLine.KeyName == GlobalSpellsLines.Combat_Styles_Effect)
                return 0;
            if (HasPositiveEffect)
                return 0;

            int hitchance = CalculateToHitChance(target);

            // Calculate the resist chance.
            resistChance = 100 - hitchance;

            if (resistChance > 100)
                resistChance = 100;

            // Use ResurrectHealth = 1 if the CC should not be resisted.
            if (Spell.ResurrectHealth == 1)
                resistChance = 0;
            else if (resistChance < 1)
                resistChance = 1;

            return resistChance;
        }

        public AbstractCCSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Mezz
    /// </summary>
    [SpellHandler("Mesmerize")]
    public class MesmerizeSpellHandler : AbstractCCSpellHandler
    {
        public const int FLUTE_MEZ_END_OF_CAST_MESSAGE_INTERVAL = 2000;
        public long FluteMezLastEndOfCastMessage { get; set; } // Flute mez should probably have its own spell handler.

        public override void CreateECSEffect(EcsGameEffectInitParams initParams)
        {
            new MezEcsSpellEffect(initParams);
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            SendEffectAnimation(effect.Owner, 0, false, 1);
            base.OnEffectPulse(effect);
        }

        //If mez resisted, just rupt, dont demez
        protected override void OnSpellResisted(GameLiving target)
        {
            // Flute Mez (pulse>0)
            if (Spell.Pulse > 0)
            {
                if (target != null && (!target.IsAlive))
                {
                    EcsGameSpellEffect effect = EffectListService.GetSpellEffectOnTarget(target, EEffect.Mez);

                    if (effect != null)
                    {
                        EffectService.RequestImmediateCancelEffect(effect);
                        EffectService.RequestImmediateCancelConcEffect(EffectListService.GetPulseEffectOnTarget(effect.SpellHandler.Caster, Spell));
                        MessageToCaster("You stop playing your song.", eChatType.CT_Spell);
                    }
                    return;
                }

                if (Spell.Range != 0)
                {
                    if (!Caster.IsWithinRadius(target, this.Spell.Range))
                        return;
                }

                if (target != Caster.TargetObject)
                    return;
            }

            EcsGameEffect mezz = EffectListService.GetEffectOnTarget(target, EEffect.Mez);

            if (mezz != null)
            {
                MessageToCaster("Your target is already mezzed!!!", eChatType.CT_SpellResisted);
                return;
            }

            if (EffectListService.GetEffectOnTarget(target, EEffect.MezImmunity) is EcsImmunityEffect immunity)
            {
                MessageToCaster(immunity.Owner.GetName(0, true) + " can't have that effect again yet!!!", eChatType.CT_SpellPulse);
                return;
            }

            SendEffectAnimation(target, 0, false, 0);
            MessageToCaster(target.GetName(0, true) + " resists the effect!" + " (" + CalculateSpellResistChance(target).ToString("0.0") + "%)", eChatType.CT_SpellResisted);
            target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.EAttackType.Spell, Caster);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            // Flute Mez (pulse>0)
            if (Spell.Pulse > 0)
            {
                if (Caster.IsWithinRadius(target, this.Spell.Range * 5) == false)
                {
                    CancelPulsingSpell(Caster, this.Spell.SpellType);
                    MessageToCaster("You are far away from the target. You stop playing your song.", eChatType.CT_Spell);
                    return;
                }

                if (target != null && (!target.IsAlive)) 
                {
                    EcsGameSpellEffect effect = EffectListService.GetSpellEffectOnTarget(target, EEffect.Mez);

                    if (effect != null)
                    {
                        EffectService.RequestImmediateCancelEffect(effect);
                        EffectService.RequestImmediateCancelConcEffect(EffectListService.GetPulseEffectOnTarget(effect.SpellHandler.Caster, Spell));
                        MessageToCaster("You stop playing your song.", eChatType.CT_Spell);
                    }

                    return;
                }

                if (Spell.Range != 0)
                {
                    if (!Caster.IsWithinRadius(target, Spell.Range) && !m_spell.IsPulsing && m_spell.SpellType != ESpellType.Mesmerize)
                        return;
                }
            }

            if (target.effectListComponent.Effects.ContainsKey(EEffect.MezImmunity) || target.HasAbility(Abilities.MezzImmunity))
            {
                MessageToCaster(target.Name + " is immune to this effect!", eChatType.CT_SpellResisted);
                SendEffectAnimation(target, 0, false, 0);
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.EAttackType.Spell, Caster);
                return;
            }

            if (FindStaticEffectOnTarget(target, typeof(MezzRootImmunityEffect)) != null)
            {
                MessageToCaster("Your target is immune!", eChatType.CT_System);
                SendEffectAnimation(target, 0, false, 0);
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.EAttackType.Spell, Caster);
                return;
            }

            if(target is GameNPC && target.HealthPercent < 75)
            {
                MessageToCaster("Your target is enraged and resists the spell!", eChatType.CT_System);
                SendEffectAnimation(target, 0, false, 0);
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.EAttackType.Spell, Caster);
                return;
            }

            // Do nothing when already mez, but inform caster.
            target.effectListComponent.Effects.TryGetValue(EEffect.Mez, out var mezz);

            if (mezz != null)
            {
                MessageToCaster("Your target is already mezzed!", eChatType.CT_SpellResisted);
                return;
            }

            /*
            GameSpellEffect mezblock = SpellHandler.FindEffectOnTarget(target, "CeremonialBracerMezz");
            if (mezblock != null)
            {
                mezblock.Cancel(false);
                if (target is GamePlayer)
                    (target as GamePlayer).Out.SendMessage("Your item effect intercepts the mesmerization spell and fades!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                //inform caster
                MessageToCaster("Ceremonial Bracer intercept your mez!", eChatType.CT_SpellResisted);
                SendEffectAnimation(target, 0, false, 0);
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
                return;
            }*/

            base.ApplyEffectOnTarget(target);
        }

        /// <summary>
        /// Calculates the effect duration in milliseconds
        /// </summary>
        /// <param name="target">The effect target</param>
        /// <param name="effectiveness">The effect effectiveness</param>
        /// <returns>The effect duration in milliseconds</returns>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = base.CalculateEffectDuration(target, effectiveness);
            duration *= target.GetModified(EProperty.MesmerizeDurationReduction) * 0.01;
            NpcEcsMezImmunityEffect npcImmune = (NpcEcsMezImmunityEffect)EffectListService.GetEffectOnTarget(target, EEffect.NPCMezImmunity);

            if (npcImmune != null)
                duration = npcImmune.CalculateMezDuration((long)duration);

            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = Spell.Duration * 4;

            return (int)duration;
        }

        public MesmerizeSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Stun
    /// </summary>
    [SpellHandler("Stun")]
    public class StunSpellHandler : AbstractCCSpellHandler
    {
        public override void CreateECSEffect(EcsGameEffectInitParams initParams)
        {
            new StunEcsSpellEffect(initParams);
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            // Use ResurrectMana=1 if the Stun should not have immunity.

            if (Spell.ResurrectMana == 1)
            {
                int freq = Spell != null ? Spell.Frequency : 0;
                return new GameSpellEffect(this, CalculateEffectDuration(target, effectiveness), freq, effectiveness);
            }

            else
                return new GameSpellAndImmunityEffect(this, CalculateEffectDuration(target, effectiveness), 0, effectiveness);
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.IsStunned=false;
            effect.Owner.DisableTurning(false);

            // Use ResurrectHealth > 0 to calculate stun immunity timer (such pet stun spells), actually (1.90) pet stun immunity is 5x the stun duration.
            if (Spell.ResurrectHealth > 0)
            {
                base.OnEffectExpires(effect, noMessages);
                return Spell.Duration * Spell.ResurrectHealth;
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if ((target.effectListComponent.Effects.ContainsKey(EEffect.StunImmunity) && this is not UnresistableStunSpellHandler) || (EffectListService.GetEffectOnTarget(target, EEffect.Stun) != null && !(Caster is GameSummonedPet)))//target.HasAbility(Abilities.StunImmunity))
            {
                MessageToCaster(target.Name + " is immune to this effect!", eChatType.CT_SpellResisted);
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.EAttackType.Spell, Caster);
                base.OnSpellResisted(target);
                return;
            }

            // Ceremonial bracer doesn't intercept physical stun.
            if(Spell.SpellType != ESpellType.StyleStun)
            {
                /*
                GameSpellEffect stunblock = SpellHandler.FindEffectOnTarget(target, "CeremonialBracerStun");
                if (stunblock != null)
                {
                    stunblock.Cancel(false);
                    if (target is GamePlayer)
                        (target as GamePlayer).Out.SendMessage("Your item effect intercepts the stun spell and fades!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    base.OnSpellResisted(target);
                    return;
                }*/
            }

            base.ApplyEffectOnTarget(target);
        }
        
        /// <summary>
        /// Calculates the effect duration in milliseconds
        /// </summary>
        /// <param name="target">The effect target</param>
        /// <param name="effectiveness">The effect effectiveness</param>
        /// <returns>The effect duration in milliseconds</returns>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = base.CalculateEffectDuration(target, effectiveness);
            duration *= target.GetModified(EProperty.StunDurationReduction) * 0.01;
            NpcEcsStunImmunityEffect npcImmune = (NpcEcsStunImmunityEffect)EffectListService.GetEffectOnTarget(target, EEffect.NPCStunImmunity);

            if (npcImmune != null)
                duration = npcImmune.CalculateStunDuration((long)duration); //target.GetModified(eProperty.StunDurationReduction) * 0.01;

            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = Spell.Duration * 4;

            return (int)duration;
        }

        /// <summary>
        /// Determines wether this spell is compatible with given spell
        /// and therefore overwritable by better versions
        /// spells that are overwritable cannot stack
        /// </summary>
        public override bool IsOverwritable(EcsGameSpellEffect compare)
        {
            if (Spell.EffectGroup != 0 || compare.SpellHandler.Spell.EffectGroup != 0)
                return Spell.EffectGroup == compare.SpellHandler.Spell.EffectGroup;
            if (compare.SpellHandler.Spell.SpellType == ESpellType.StyleStun) return true;
            return base.IsOverwritable(compare);
        }

        public StunSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
