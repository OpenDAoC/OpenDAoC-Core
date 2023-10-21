using System;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.Events;
using Core.GS.ECS;

namespace Core.GS.Spells
{
    [SpellHandler("Mesmerize")]
    public class MesmerizeSpell : ACrowdControlSpell
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
                        MessageToCaster("You stop playing your song.", EChatType.CT_Spell);
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
                MessageToCaster("Your target is already mezzed!!!", EChatType.CT_SpellResisted);
                return;
            }

            if (EffectListService.GetEffectOnTarget(target, EEffect.MezImmunity) is EcsImmunityEffect immunity)
            {
                MessageToCaster(immunity.Owner.GetName(0, true) + " can't have that effect again yet!!!", EChatType.CT_SpellPulse);
                return;
            }

            SendEffectAnimation(target, 0, false, 0);
            MessageToCaster(target.GetName(0, true) + " resists the effect!" + " (" + CalculateSpellResistChance(target).ToString("0.0") + "%)", EChatType.CT_SpellResisted);
            target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            // Flute Mez (pulse>0)
            if (Spell.Pulse > 0)
            {
                if (Caster.IsWithinRadius(target, this.Spell.Range * 5) == false)
                {
                    CancelPulsingSpell(Caster, this.Spell.SpellType);
                    MessageToCaster("You are far away from the target. You stop playing your song.", EChatType.CT_Spell);
                    return;
                }

                if (target != null && (!target.IsAlive)) 
                {
                    EcsGameSpellEffect effect = EffectListService.GetSpellEffectOnTarget(target, EEffect.Mez);

                    if (effect != null)
                    {
                        EffectService.RequestImmediateCancelEffect(effect);
                        EffectService.RequestImmediateCancelConcEffect(EffectListService.GetPulseEffectOnTarget(effect.SpellHandler.Caster, Spell));
                        MessageToCaster("You stop playing your song.", EChatType.CT_Spell);
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
                MessageToCaster(target.Name + " is immune to this effect!", EChatType.CT_SpellResisted);
                SendEffectAnimation(target, 0, false, 0);
                target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
                return;
            }

            if (FindStaticEffectOnTarget(target, typeof(MezzRootImmunityEffect)) != null)
            {
                MessageToCaster("Your target is immune!", EChatType.CT_System);
                SendEffectAnimation(target, 0, false, 0);
                target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
                return;
            }

            if(target is GameNpc && target.HealthPercent < 75)
            {
                MessageToCaster("Your target is enraged and resists the spell!", EChatType.CT_System);
                SendEffectAnimation(target, 0, false, 0);
                target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
                return;
            }

            // Do nothing when already mez, but inform caster.
            target.effectListComponent.Effects.TryGetValue(EEffect.Mez, out var mezz);

            if (mezz != null)
            {
                MessageToCaster("Your target is already mezzed!", EChatType.CT_SpellResisted);
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

        public MesmerizeSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
