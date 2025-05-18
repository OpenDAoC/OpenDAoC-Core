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
                else if (effects != null)
                    return effects.OfType<ECSGameSpellEffect>().Where(e => e.SpellHandler.Spell.SpellType == spellType).FirstOrDefault();
                else
                    return null;
            }
        }

        public static ECSGameSpellEffect GetSpellEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.None)
        {
            if (target == null)
                return null;

            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null)
                    return effects.OfType<ECSGameSpellEffect>().Where(e => e != null && (spellType == eSpellType.None || e.SpellHandler.Spell.SpellType == spellType)).FirstOrDefault();
                else
                    return null;
            }
        }

        public static ECSGameAbilityEffect GetAbilityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null)
                    return (ECSGameAbilityEffect) effects.Where(e => e is ECSGameAbilityEffect).FirstOrDefault();
                else
                    return null;
            }
        }

        public static ECSImmunityEffect GetImmunityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null)
                    return (ECSImmunityEffect) effects.Where(e => e is ECSImmunityEffect).FirstOrDefault();
                else
                    return null;
            }
        }

        public static ECSPulseEffect GetPulseEffectOnTarget(GameLiving target, Spell spell)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(eEffect.Pulse, out List<ECSGameEffect> effects);

                if (effects != null)
                    return (ECSPulseEffect) effects.Where(e => e is ECSPulseEffect && e.SpellHandler.Spell == spell).FirstOrDefault();
                else
                    return null;
            }
        }

        public static bool TryCancelFirstEffectOfTypeOnTarget(GameLiving target, eEffect effectType)
        {
            if (target == null || target.effectListComponent == null)
                return false;

            ECSGameEffect effectToCancel;

            lock (target.effectListComponent.EffectsLock)
            {
                if (!target.effectListComponent.ContainsEffectForEffectType(effectType))
                    return false;

                effectToCancel = GetEffectOnTarget(target, effectType);

                if (effectToCancel == null)
                    return false;

                effectToCancel.Stop();
                return true;
            }
        }
    }
}
