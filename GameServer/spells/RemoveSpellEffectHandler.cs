using System;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Base class for all spells that remove effects
    /// </summary>
    public abstract class RemoveSpellEffectHandler : SpellHandler
    {
        protected abstract ReadOnlySpan<eEffect> EffectsToRemove { get; }

        protected RemoveSpellEffectHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            base.OnEffectPulse(effect);
            OnDirectEffect(effect.Owner);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target == null || !target.IsAlive)
                return;

            base.OnDirectEffect(target);

            foreach (eEffect effectType in EffectsToRemove)
            {
                foreach (ECSGameEffect effect in target.effectListComponent.GetEffects(effectType))
                    effect?.End();
            }

            SendEffectAnimation(target, 0, false, 1);
        }
    }
}
