using System.Collections.Generic;

namespace DOL.GS
{
    public static class EffectListService
    {
        public static void Tick(long tick)
        {
            foreach (var p in EntityManager.GetAllPlayers())
            {
                HandleEffects(tick, p);
            }
            foreach (var npc in EntityManager.GetAllNpcs())
            {
                HandleEffects(tick, (GameLiving)npc);
            }
        }

        private static void HandleEffects(long tick, GameLiving living)
        {
            if (living.effectListComponent.Effects.Count > 0)
            {
                foreach (var effect in living.effectListComponent.Effects)
                {
                    if (tick > effect.Value.ExpireTick && !effect.Value.SpellHandler.Spell.IsConcentration)
                    {
                        if (effect.Value.SpellHandler.Spell.IsPulsing && (effect.Value.SpellHandler.Caster.LastPulseCast == effect.Value.SpellHandler.Spell))
                        {
                            if (effect.Value.Owner is GamePlayer)
                                ((GamePlayer)effect.Value.Owner).Out.SendUpdateIcons(new List<ECSGameEffect>() { effect.Value }, ref effect.Value.Owner.effectListComponent._lastUpdateEffectsCount);
                            effect.Value.ExpireTick += effect.Value.PulseFreq;
                            
                        }
                        else
                        {
                            effect.Value.CancelEffect = true;
                            EntityManager.AddEffect(effect.Value);
                        }
                    }
                    if (effect.Value.EffectType == eEffect.DamageOverTime)
                    {
                        if (effect.Value.LastTick == 0)
                        {
                            EffectService.OnEffectPulse(effect.Value);
                            effect.Value.LastTick = GameLoop.GameLoopTime;
                        }
                        else if (tick > effect.Value.PulseFreq + effect.Value.LastTick)
                        {
                            EffectService.OnEffectPulse(effect.Value);
                            effect.Value.LastTick += effect.Value.PulseFreq;
                        }
                    }
                }
            }
        }
    }
}