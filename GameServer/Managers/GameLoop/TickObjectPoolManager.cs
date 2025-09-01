using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class TickObjectPoolManager
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static FrozenDictionary<Type, PooledObjectKey> _typeToKeyMap =
            new Dictionary<Type, PooledObjectKey>
            {
                { typeof(GSPacketIn), PooledObjectKey.InPacket },
                { typeof(GSTCPPacketOut), PooledObjectKey.TcpOutPacket },
                { typeof(GSUDPPacketOut), PooledObjectKey.UdpOutPacket },
                { typeof(SubZoneTransition), PooledObjectKey.SubZoneTransition }
            }.ToFrozenDictionary();

        private FrozenDictionary<PooledObjectKey, ITickObjectPool> _pools =
            new Dictionary<PooledObjectKey, ITickObjectPool>
            {
                { PooledObjectKey.InPacket, new TickObjectPool<GSPacketIn>() },
                { PooledObjectKey.TcpOutPacket, new TickObjectPool<GSTCPPacketOut>() },
                { PooledObjectKey.UdpOutPacket, new TickObjectPool<GSUDPPacketOut>() },
                { PooledObjectKey.SubZoneTransition, new TickObjectPool<SubZoneTransition>() }
            }.ToFrozenDictionary();

        public T GetForTick<T>() where T : IPooledObject<T>, new()
        {
            if (!_typeToKeyMap.TryGetValue(typeof(T), out PooledObjectKey key))
                throw new ArgumentException($"No pool is registered for lists of type '{typeof(T).Name}'.", nameof(T));

            if (_pools[key] is not TickObjectPool<T> typedPool)
                throw new InvalidCastException($"The pool for key '{key}' is not of the expected type '{typeof(T).Name}'.");

            return typedPool.GetForTick();
        }

        public void Reset()
        {
            foreach (var pair in _pools)
                pair.Value.Reset();
        }

        private sealed class TickObjectPool<T> : ITickObjectPool where T : IPooledObject<T>, new()
        {
            private const int INITIAL_CAPACITY = 64;       // Initial capacity of the pool.
            private const double TRIM_SAFETY_FACTOR = 2.5; // Trimming allowed when size > smoothed usage * this factor.
            private const int HALF_LIFE = 300_000;         // Half-life (ms) for EMA decay.

            private static readonly double _decayFactor;   // EMA decay factor based on HALF_LIFE and tick rate.
            private T[] _items = new T[INITIAL_CAPACITY];  // Backing pool array.
            private int _used;                             // Objects rented this tick.
            private double _smoothedUsage;                 // Smoothed recent peak usage.
            private int _logicalSize;                      // Highest non-null index in use.

            static TickObjectPool()
            {
                // Will become outdated if `GameLoop.TickDuration` is changed at runtime.
                _decayFactor = Math.Exp(-Math.Log(2) / (GameLoop.TickDuration * HALF_LIFE / 1000.0));
            }

            public T GetForTick()
            {
                T item;

                if (_used < _logicalSize)
                    item = _items[_used++];
                else
                {
                    item = new();

                    if (_used >= _items.Length)
                        Array.Resize(ref _items, _items.Length * 2);

                    _items[_used++] = item;
                    _logicalSize = Math.Max(_logicalSize, _used);
                }

                // Create a new item if this one wasn't released last tick.
                if (item.IssuedTimestamp != 0)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Item '{item}' was not released last tick (IssuedTimestamp: {item.IssuedTimestamp}) (CurrentTime: {GameLoop.GameLoopTime}).");

                    item = new();
                    _items[_used - 1] = item;
                }

                item.IssuedTimestamp = GameLoop.GameLoopTime;
                return item;
            }

            public void Reset()
            {
                _smoothedUsage = Math.Max(_used, _smoothedUsage * _decayFactor + _used * (1 - _decayFactor));
                int newLogicalSize = (int) (_smoothedUsage * TRIM_SAFETY_FACTOR);

                if (_logicalSize > newLogicalSize)
                {
                    for (int i = newLogicalSize; i < _logicalSize; i++)
                        _items[i] = default;

                    _logicalSize = newLogicalSize;
                }

                _used = 0;
            }
        }

        private interface ITickObjectPool
        {
            void Reset();
        }
    }

    public enum PooledObjectKey
    {
        InPacket,
        TcpOutPacket,
        UdpOutPacket,
        SubZoneTransition
    }

    public interface IPooledObject<T>
    {
        // The game loop tick timestamp when this object was issued.
        // Will be 0 if created outside the game loop (e.g., by a .NET worker thread without local object pools).
        long IssuedTimestamp { get; set; }
    }

    public static class PooledObjectFactory
    {
        public static T GetForTick<T>() where T : IPooledObject<T>, new()
        {
            return GameLoop.GetObjectForTick<T>();
        }
    }

    public static class PooledObjectExtensions
    {
        public static void ReleasePooledObject<T>(this IPooledObject<T> pooledObject)
        {
            pooledObject.IssuedTimestamp = 0;
        }
    }
}
