using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class EffectListService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(EffectListService);

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<EffectListComponent> list = EntityManager.UpdateAndGetAll<EffectListComponent>(EntityManager.EntityType.EffectListComponent, out int lastValidIndex);

            Parallel.For(0, lastValidIndex + 1, i =>
            {
                EffectListComponent effectListComponent = list[i];

                try
                {
                    if (effectListComponent?.EntityManagerId.IsSet != true)
                        return;

                    long startTick = GameLoop.GetCurrentTime();
                    effectListComponent.Tick();
                    long stopTick = GameLoop.GetCurrentTime();

                    if (stopTick - startTick > 25)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {effectListComponent.Owner.Name}({effectListComponent.Owner.ObjectID}) Time: {stopTick - startTick}ms");
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, effectListComponent, effectListComponent.Owner);
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        public static ECSGameEffect GetEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.Null)
        {
            target.effectListComponent._effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

            lock ((effects as ICollection).SyncRoot)
            {
                if (spellType == eSpellType.Null)
                    return effects.FirstOrDefault();
                else
                    return effects.OfType<ECSGameSpellEffect>().Where(e => e.SpellHandler.Spell.SpellType == spellType).FirstOrDefault();
            }
        }

        public static ECSGameSpellEffect GetSpellEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.Null)
        {
            target.effectListComponent._effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

            lock ((effects as ICollection).SyncRoot)
            {
                return effects.OfType<ECSGameSpellEffect>().Where(e => spellType == eSpellType.Null || e.SpellHandler.Spell.SpellType == spellType).FirstOrDefault();
            }
        }

        public static ECSGameAbilityEffect GetAbilityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            target.effectListComponent._effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

            lock ((effects as ICollection).SyncRoot)
            {
                return effects.Where(e => e is ECSGameAbilityEffect).FirstOrDefault() as ECSGameAbilityEffect;
            }
        }

        public static ECSImmunityEffect GetImmunityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            target.effectListComponent._effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

            lock ((effects as ICollection).SyncRoot)
            {
                return effects.Where(e => e is ECSImmunityEffect).FirstOrDefault() as ECSImmunityEffect;
            }
        }

        public static ECSPulseEffect GetPulseEffectOnTarget(GameLiving target, Spell spell)
        {
            target.effectListComponent._effects.TryGetValue(eEffect.Pulse, out List<ECSGameEffect> effects);

            lock ((effects as ICollection).SyncRoot)
            {
                return effects.Where(e => e is ECSPulseEffect && e.SpellHandler.Spell == spell).FirstOrDefault() as ECSPulseEffect;
            }
        }

        public static void CancelFirstEffectOfTypeOnTarget(GameLiving target, eEffect effectType)
        {
            ECSGameEffect effectToCancel = GetEffectOnTarget(target, effectType);

            if (effectToCancel == null)
                return;

            EffectService.RequestImmediateCancelEffect(effectToCancel);
        }

        public static void CancelAllEffectsOfTypeOnTarget(GameLiving target, eEffect effectType)
        {
            foreach (ECSGameEffect effect in target.effectListComponent.GetAllEffects(x => x.EffectType == effectType))
                EffectService.RequestImmediateCancelEffect(effect);
        }
    }
}
