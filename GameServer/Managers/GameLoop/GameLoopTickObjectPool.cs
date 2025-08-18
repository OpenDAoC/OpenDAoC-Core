using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public sealed class GameLoopTickObjectPool
    {
        private Dictionary<PooledObjectKey, ITickObjectPool> _pools = new()
        {
            { PooledObjectKey.InPacket, new TickObjectPool<GSPacketIn>() },
            { PooledObjectKey.TcpOutPacket, new TickObjectPool<GSTCPPacketOut>() },
            { PooledObjectKey.UdpOutPacket, new TickObjectPool<GSUDPPacketOut>() }
        };

        public T GetForTick<T>(PooledObjectKey key) where T : IPooledObject<T>, new()
        {
            return (_pools[key] as TickObjectPool<T>).GetForTick();
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
            private static readonly double DECAY_FACTOR;   // EMA decay factor based on HALF_LIFE and tick rate.

            private T[] _items = new T[INITIAL_CAPACITY];  // Backing pool array.
            private int _used;                             // Objects rented this tick.
            private double _smoothedUsage;                 // Smoothed recent peak usage.
            private int _logicalSize;                      // Highest non-null index in use.

            static TickObjectPool()
            {
                DECAY_FACTOR = Math.Exp(-Math.Log(2) / (GameLoop.TickDuration * HALF_LIFE / 1000.0));
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

                item.IssuedTimestamp = GameLoop.GameLoopTime;
                return item;
            }

            public void Reset()
            {
                _smoothedUsage = Math.Max(_used, _smoothedUsage * DECAY_FACTOR + _used * (1 - DECAY_FACTOR));
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
        UdpOutPacket
    }

    public interface IPooledObject<T>
    {
        static abstract PooledObjectKey PooledObjectKey { get; }
        static abstract T GetForTick(Action<T> initializer);

        // The game loop tick timestamp when this object was issued.
        // Will be 0 if created outside the game loop (e.g., by a .NET worker thread without local object pools).
        long IssuedTimestamp { get; set; }
    }

    public static class PooledObjectExtensions
    {
        public static bool IsValidForTick<T>(this IPooledObject<T> obj)
        {
            return obj.IssuedTimestamp == GameLoop.GameLoopTime;
        }
    }
}
