using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
    public static class EffectListService
    {
        public static ECSGameEffect GetEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.None)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null && spellType == eSpellType.None)
                    return effects.FirstOrDefault();
                else
                    return effects?.OfType<ECSGameSpellEffect>().FirstOrDefault(e => e.SpellHandler.Spell.SpellType == spellType);
            }
        }

        public static ECSGameSpellEffect GetSpellEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.None)
        {
            if (target == null)
                return null;

            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);
                return effects?.OfType<ECSGameSpellEffect>().FirstOrDefault(e => spellType is eSpellType.None || e.SpellHandler.Spell.SpellType == spellType);
            }
        }

        public static ECSGameAbilityEffect GetAbilityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);
                return effects?.FirstOrDefault(e => e is ECSGameAbilityEffect) as ECSGameAbilityEffect;
            }
        }

        public static ECSImmunityEffect GetImmunityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);
                return effects?.FirstOrDefault(e => e is ECSImmunityEffect) as ECSImmunityEffect;
            }
        }

        public static ECSPulseEffect GetPulseEffectOnTarget(GameLiving target, Spell spell)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(eEffect.Pulse, out List<ECSGameEffect> effects);
                return effects?.FirstOrDefault(e => e is ECSPulseEffect && e.SpellHandler.Spell == spell) as ECSPulseEffect;
            }
        }

        public static bool TryCancelFirstEffectOfTypeOnTarget(GameLiving target, eEffect effectType)
        {
            if (target?.effectListComponent == null)
                return false;

            lock (target.effectListComponent.EffectsLock)
            {
                if (!target.effectListComponent.ContainsEffectForEffectType(effectType))
                    return false;

                ECSGameEffect effectToCancel = GetEffectOnTarget(target, effectType);

                if (effectToCancel == null)
                    return false;

                effectToCancel.Stop();
                return true;
            }
        }
    }
}
