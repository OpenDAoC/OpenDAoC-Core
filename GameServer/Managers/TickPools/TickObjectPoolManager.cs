using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public sealed class TickObjectPoolManager
    {
        private static FrozenDictionary<Type, TickPooledObjectKey> _typeToKeyMap =
            new Dictionary<Type, TickPooledObjectKey>
            {
                { typeof(GSPacketIn), TickPooledObjectKey.InPacket },
                { typeof(GSTCPPacketOut), TickPooledObjectKey.TcpOutPacket },
                { typeof(GSUDPPacketOut), TickPooledObjectKey.UdpOutPacket }
            }.ToFrozenDictionary();

        private FrozenDictionary<TickPooledObjectKey, TickPoolBase> _pools =
            new Dictionary<TickPooledObjectKey, TickPoolBase>
            {
                { TickPooledObjectKey.InPacket, new TickObjectPool<GSPacketIn>() },
                { TickPooledObjectKey.TcpOutPacket, new TickObjectPool<GSTCPPacketOut>() },
                { TickPooledObjectKey.UdpOutPacket, new TickObjectPool<GSUDPPacketOut>() }
            }.ToFrozenDictionary();

        public T GetForTick<T>() where T : ITickPooledObject<T>, new()
        {
            if (!_typeToKeyMap.TryGetValue(typeof(T), out TickPooledObjectKey key))
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

    public enum TickPooledObjectKey
    {
        InPacket,
        TcpOutPacket,
        UdpOutPacket,
        SubZoneTransition
    }

    public interface ITickPooledObject<TSelf>
    {
        // The game loop tick timestamp when this object was issued.
        // Will be 0 if created outside the game loop (e.g., by a .NET worker thread without local object pools).
        long IssuedTimestamp { get; set; }
    }

    public static class TickPooledObjectExtensions
    {
        public static void ReleasePooledObject<T>(this ITickPooledObject<T> pooledObject)
        {
            pooledObject.IssuedTimestamp = 0;
        }
    }
}
