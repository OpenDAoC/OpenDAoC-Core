using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public sealed class TickObjectPoolManager
    {
        private static FrozenDictionary<Type, PooledObjectKey> _typeToKeyMap =
            new Dictionary<Type, PooledObjectKey>
            {
                { typeof(GSPacketIn), PooledObjectKey.InPacket },
                { typeof(GSTCPPacketOut), PooledObjectKey.TcpOutPacket },
                { typeof(GSUDPPacketOut), PooledObjectKey.UdpOutPacket }
            }.ToFrozenDictionary();

        private FrozenDictionary<PooledObjectKey, TickPoolBase> _pools =
            new Dictionary<PooledObjectKey, TickPoolBase>
            {
                { PooledObjectKey.InPacket, new TickObjectPool<GSPacketIn>() },
                { PooledObjectKey.TcpOutPacket, new TickObjectPool<GSTCPPacketOut>() },
                { PooledObjectKey.UdpOutPacket, new TickObjectPool<GSUDPPacketOut>() }
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
