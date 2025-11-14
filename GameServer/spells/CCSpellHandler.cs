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
                MessageToCaster("Your target is immune to this effect!", eChatType.CT_SpellResisted);
                return;
            }

            if (target.EffectList.GetOfType<ChargeEffect>() != null || target.TempProperties.GetProperty<bool>("Charging"))
            {
                MessageToCaster("Your target is moving too fast for this spell to have any effect!", eChatType.CT_SpellResisted);
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

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double duration = base.CalculateEffectDuration(target);

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

            if (Spell.SpellType != eSpellType.StyleStun)
            {
                // capping duration adjustment to 100%, live cap unknown - Tolakram
                double hitChance = Math.Min(200, CalculateToHitChance(target));

                if (hitChance <= 0)
                {
                    duration = 0;
                }
                else if (hitChance < 55)
                {
                    duration -= duration * (55 - hitChance) * 0.01;
                }
                else if (hitChance > 100)
                {
                    duration += duration * (hitChance - 100) * 0.01;
                }
            }

            return (int)duration;
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            double resistChance;

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

            double hitChance = CalculateToHitChance(target);

            // Calculate the resist chance.
            resistChance = 100 - hitChance;

            if (resistChance > 100)
                resistChance = 100;

            // Use ResurrectHealth = 1 if the CC should not be resisted.
            if (Spell.ResurrectHealth == 1)
                resistChance = 0;

            return resistChance;
        }

        public AbstractCCSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Mezz
    /// </summary>
    [SpellHandler(eSpellType.Mesmerize)]
    public class MesmerizeSpellHandler : AbstractCCSpellHandler
    {
        public override string ShortDescription => "The target is mesmerized and cannot take any actions.";

        public MesmerizeSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, static (in ECSGameEffectInitParams i) => new MezECSGameEffect(i));
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            SendEffectAnimation(effect.Owner, 0, false, 1);
            base.OnEffectPulse(effect);
        }

        protected override bool CheckSpellResist(GameLiving target)
        {
            bool isImmune = false;
            string message = string.Empty;

            if (target.effectListComponent.ContainsEffectForEffectType(eEffect.MezImmunity) || target.HasAbility(Abilities.MezzImmunity))
                isImmune = true;
            else
            {
                if (target is GamePlayer)
                    isImmune = target.effectListComponent.ContainsEffectForEffectType(eEffect.Mez);
                else if (EffectListService.GetEffectOnTarget(target, eEffect.NPCMezImmunity) is NpcMezImmunityEffect immunityEffect)
                    isImmune = !immunityEffect.CanApplyNewEffect(base.CalculateEffectDuration(target));
            }

            if (isImmune)
                message = "Your target is immune to this effect!";
            else if (target is GameNPC && target.HealthPercent < 75)
            {
                message = "Your target is enraged and resists the spell!";
                isImmune = true;
            }

            GameSpellEffect mezblock = FindEffectOnTarget(target, "CeremonialBracerMezz");

            if (mezblock != null)
            {
                mezblock.Cancel(false);
                (target as GamePlayer)?.Out.SendMessage("Your item effect intercepts the mesmerization spell and fades!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                message = "Ceremonial Bracer intercept your mez!";
                isImmune = true;
            }

            if (isImmune)
            {
                MessageToCaster(message, eChatType.CT_SpellResisted);
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
                OnSpellNegated(target, SpellNegatedReason.Immune);
                return true;
            }

            return base.CheckSpellResist(target);
        }

        public override void OnDurationEffectApply(GameLiving target)
        {
            base.OnDurationEffectApply(target);

            if (EffectListService.GetEffectOnTarget(target, eEffect.NPCMezImmunity) is NpcMezImmunityEffect immunityEffect)
                immunityEffect.OnApplyNewEffect();
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double duration = base.CalculateEffectDuration(target);
            duration *= target.GetModified(eProperty.MesmerizeDurationReduction) * 0.01;

            if (EffectListService.GetEffectOnTarget(target, eEffect.NPCMezImmunity) is NpcMezImmunityEffect immunityEffect)
                duration = immunityEffect.CalculateNewEffectDuration((long) duration);

            return (int) Math.Clamp(duration, 1, Spell.Duration * 4);
        }
    }

    /// <summary>
    /// Stun
    /// </summary>
    [SpellHandler(eSpellType.Stun)]
    public class StunSpellHandler : AbstractCCSpellHandler
    {
        public override string ShortDescription => $"The target is stunned and cannot take any actions for {Spell.Duration / 1000.0} seconds.";

        public StunSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, static (in ECSGameEffectInitParams i) => new StunECSGameEffect(i));
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            // Use ResurrectMana=1 if the Stun should not have immunity.

            if (Spell.ResurrectMana == 1)
            {
                int freq = Spell != null ? Spell.Frequency : 0;
                return new GameSpellEffect(this, CalculateEffectDuration(target), freq, effectiveness);
            }

            else
                return new GameSpellAndImmunityEffect(this, CalculateEffectDuration(target), 0, effectiveness);
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

        protected override bool CheckSpellResist(GameLiving target)
        {
            bool isImmune = false;

            if ((this is not UnresistableStunSpellHandler && target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity)) || target.HasAbility(Abilities.StunImmunity))
                isImmune = true;
            else
            {
                if (target is GamePlayer)
                    isImmune = target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun);
                else if (EffectListService.GetEffectOnTarget(target, eEffect.NPCStunImmunity) is NpcStunImmunityEffect immunityEffect)
                    isImmune = !immunityEffect.CanApplyNewEffect(base.CalculateEffectDuration(target));
            }

            if (isImmune)
            {
                MessageToCaster("Your target is immune to this effect!", eChatType.CT_SpellResisted);
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
                OnSpellNegated(target, SpellNegatedReason.Immune);
                return true;
            }

            // Ceremonial bracer doesn't intercept physical stun.
            if (Spell.SpellType is not eSpellType.StyleStun)
            {
                /* GameSpellEffect stunblock = SpellHandler.FindEffectOnTarget(target, "CeremonialBracerStun");
                if (stunblock != null)
                {
                    stunblock.Cancel(false);
                    if (target is GamePlayer)
                        (target as GamePlayer).Out.SendMessage("Your item effect intercepts the stun spell and fades!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    base.OnSpellNegated(target);
                    return;
                }*/
            }

            return base.CheckSpellResist(target);
        }

        public override void OnDurationEffectApply(GameLiving target)
        {
            base.OnDurationEffectApply(target);

            if (EffectListService.GetEffectOnTarget(target, eEffect.NPCStunImmunity) is NpcStunImmunityEffect immunityEffect)
                immunityEffect.OnApplyNewEffect();
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double duration = base.CalculateEffectDuration(target);
            duration *= target.GetModified(eProperty.StunDurationReduction) * 0.01;

            if (EffectListService.GetEffectOnTarget(target, eEffect.NPCStunImmunity) is NpcStunImmunityEffect immunityEffect)
                duration = immunityEffect.CalculateNewEffectDuration((long) duration);

            return (int) Math.Clamp(duration, 1, Spell.Duration * 4);
        }

        public override bool HasConflictingEffectWith(ISpellHandler compare)
        {
            if (Spell.EffectGroup != 0 || compare.Spell.EffectGroup != 0)
                return Spell.EffectGroup == compare.Spell.EffectGroup;
            if (compare.Spell.SpellType == eSpellType.StyleStun) return true;
            return base.HasConflictingEffectWith(compare);
        }
    }
}
