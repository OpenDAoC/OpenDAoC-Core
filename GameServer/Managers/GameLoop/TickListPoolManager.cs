using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using DOL.GS.Keeps;

namespace DOL.GS
{
    public sealed class TickListPoolManager
    {
        private static readonly FrozenDictionary<Type, PooledListKey> _typeToKeyMap =
            new Dictionary<Type, PooledListKey>
            {
                { typeof(GameClient), PooledListKey.Client },
                { typeof(GameLiving), PooledListKey.Living },
                { typeof(GamePlayer), PooledListKey.Player },
                { typeof(GameNPC), PooledListKey.Npc },
                { typeof(GameStaticItem), PooledListKey.Item },
                { typeof(GameDoorBase), PooledListKey.Door },
                { typeof(GameKeepComponent), PooledListKey.KeepComponent },
                { typeof(ECSGameEffect), PooledListKey.Effect },
                { typeof(ECSGameSpellEffect), PooledListKey.SpellEffect },
                { typeof(ECSPulseEffect), PooledListKey.PulseEffect },
                { typeof(ECSGameAbilityEffect), PooledListKey.AbilityEffect }
            }.ToFrozenDictionary();

        private readonly FrozenDictionary<PooledListKey, TickPoolBase> _pools =
            new Dictionary<PooledListKey, TickPoolBase>
            {
                { PooledListKey.Client, new TickListPool<GameClient>() },
                { PooledListKey.Living, new TickListPool<GameLiving>() },
                { PooledListKey.Player, new TickListPool<GamePlayer>() },
                { PooledListKey.Npc, new TickListPool<GameNPC>() },
                { PooledListKey.Item, new TickListPool<GameStaticItem>() },
                { PooledListKey.Door, new TickListPool<GameDoorBase>() },
                { PooledListKey.KeepComponent, new TickListPool<GameKeepComponent>() },
                { PooledListKey.Effect, new TickListPool<ECSGameEffect>() },
                { PooledListKey.SpellEffect, new TickListPool<ECSGameSpellEffect>() },
                { PooledListKey.PulseEffect, new TickListPool<ECSPulseEffect>() },
                { PooledListKey.AbilityEffect, new TickListPool<ECSGameAbilityEffect>() }
            }.ToFrozenDictionary();

        public List<T> GetForTick<T>() where T : IPooledList<T>
        {
            if (!_typeToKeyMap.TryGetValue(typeof(T), out PooledListKey key))
                throw new ArgumentException($"No pool is registered for lists of type '{typeof(T).Name}'.", nameof(T));

            if (_pools[key] is not TickListPool<T> typedPool)
                throw new InvalidCastException($"The pool for key '{key}' is not of the expected type '{typeof(T).Name}'.");

            return typedPool.GetForTick();
        }

        public void Reset()
        {
            foreach (var pair in _pools)
                pair.Value.Reset();
        }
    }

    public enum PooledListKey
    {
        Client,
        Living,
        Player,
        Npc,
        Item,
        Door,
        KeepComponent,
        Effect,
        SpellEffect,
        PulseEffect,
        AbilityEffect
    }

    public interface IPooledList<T> { }
}
