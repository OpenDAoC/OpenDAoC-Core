using System;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.Events;

namespace Core.GS.Spells
{
    [SpellHandler("Stun")]
    public class StunSpell : ACrowdControlSpell
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
            if ((target.effectListComponent.Effects.ContainsKey(EEffect.StunImmunity) && this is not UnresistableStunSpell) || (EffectListService.GetEffectOnTarget(target, EEffect.Stun) != null && !(Caster is GameSummonedPet)))//target.HasAbility(Abilities.StunImmunity))
            {
                MessageToCaster(target.Name + " is immune to this effect!", EChatType.CT_SpellResisted);
                target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
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

        public StunSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
