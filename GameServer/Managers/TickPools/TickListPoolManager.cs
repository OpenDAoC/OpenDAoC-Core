using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using DOL.GS.Housing;
using DOL.GS.Keeps;

namespace DOL.GS
{
    public sealed class TickListPoolManager
    {
        private static readonly FrozenDictionary<Type, TickPooledListKey> _typeToKeyMap =
            new Dictionary<Type, TickPooledListKey>
            {
                { typeof(GameClient), TickPooledListKey.Client },
                { typeof(GameLiving), TickPooledListKey.Living },
                { typeof(GamePlayer), TickPooledListKey.Player },
                { typeof(GameNPC), TickPooledListKey.Npc },
                { typeof(GameStaticItem), TickPooledListKey.Item },
                { typeof(GameDoorBase), TickPooledListKey.Door },
                { typeof(GameKeepComponent), TickPooledListKey.KeepComponent },
                { typeof(House), TickPooledListKey.House },
                { typeof(ECSGameEffect), TickPooledListKey.Effect },
                { typeof(ECSGameSpellEffect), TickPooledListKey.SpellEffect },
                { typeof(ECSPulseEffect), TickPooledListKey.PulseEffect },
                { typeof(ECSGameAbilityEffect), TickPooledListKey.AbilityEffect },
                { typeof(IArea), TickPooledListKey.Area }
            }.ToFrozenDictionary();

        private readonly FrozenDictionary<TickPooledListKey, TickPoolBase> _pools =
            new Dictionary<TickPooledListKey, TickPoolBase>
            {
                { TickPooledListKey.Client, new TickListPool<GameClient>() },
                { TickPooledListKey.Living, new TickListPool<GameLiving>() },
                { TickPooledListKey.Player, new TickListPool<GamePlayer>() },
                { TickPooledListKey.Npc, new TickListPool<GameNPC>() },
                { TickPooledListKey.Item, new TickListPool<GameStaticItem>() },
                { TickPooledListKey.Door, new TickListPool<GameDoorBase>() },
                { TickPooledListKey.KeepComponent, new TickListPool<GameKeepComponent>() },
                { TickPooledListKey.House, new TickListPool<House>() },
                { TickPooledListKey.Effect, new TickListPool<ECSGameEffect>() },
                { TickPooledListKey.SpellEffect, new TickListPool<ECSGameSpellEffect>() },
                { TickPooledListKey.PulseEffect, new TickListPool<ECSPulseEffect>() },
                { TickPooledListKey.AbilityEffect, new TickListPool<ECSGameAbilityEffect>() },
                { TickPooledListKey.Area, new TickListPool<IArea>() }
            }.ToFrozenDictionary();

        public List<T> GetForTick<T>() where T : ITickPooledList<T>
        {
            if (!_typeToKeyMap.TryGetValue(typeof(T), out TickPooledListKey key))
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

    public enum TickPooledListKey
    {
        Client,
        Living,
        Player,
        Npc,
        Item,
        Door,
        KeepComponent,
        House,
        Effect,
        SpellEffect,
        PulseEffect,
        AbilityEffect,
        Area
    }

    public interface ITickPooledList<TSelf> { }
}
