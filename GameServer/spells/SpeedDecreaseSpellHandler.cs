using System;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.SpeedDecrease)]
    public class SpeedDecreaseSpellHandler : ImmunityEffectSpellHandler
    {
        public override string ShortDescription =>
            Spell.Value >= 99 ?
            "The target is rooted in place." :
            $"The target is slowed by {Spell.Value}%.";

        public SpeedDecreaseSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, static (in i) => new StatDebuffECSEffect(i));
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            // Check for immunities.
            if (target.HasAbility(Abilities.CCImmunity) ||
                target.HasAbility(Abilities.RootImmunity) || // Also affects snares?
                target.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity) ||
                target.effectListComponent.ContainsEffectForEffectType(eEffect.SpeedOfSound))
            {
                MessageToCaster("Your target is immune to this effect!", eChatType.CT_SpellResisted);
                OnSpellNegated(target, SpellNegatedReason.Immune);
                return;
            }

            if (target.EffectList.GetOfType<ChargeEffect>() != null)
            {
                MessageToCaster($"{target.Name} is moving to fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }

        public override bool HasConflictingEffectWith(ISpellHandler compare)
        {
            // Snares and roots always conflict with each other.
            // Ideally this should be implemented using effect groups, but the database isn't consistent.
            return true;
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double duration = base.CalculateEffectDuration(target);
            duration *= target.GetModified(eProperty.SpeedDecreaseDurationReduction) * 0.01;

            if (duration < 1)
                duration = 1;
            else if (duration > Spell.Duration * 4)
                duration = Spell.Duration * 4;

            return (int) duration;
        }

        protected override double GetDebuffEffectivenessCriticalModifier()
        {
            // Roots are not allowed to crit, since the lack of walking animation is very confusing.
            if (Spell.Value == 99)
                return 1.0;

            int criticalChance = Caster.DebuffCriticalChance;

            if (criticalChance <= 0)
                return 1.0;

            if (!Caster.Chance(RandomDeckEvent.CriticalChance, Math.Min(50, criticalChance)))
                return 1.0;

            (Caster as GamePlayer)?.Out.SendMessage($"Your snare is doubly effective!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            return 2.0;
        }
    }
}
