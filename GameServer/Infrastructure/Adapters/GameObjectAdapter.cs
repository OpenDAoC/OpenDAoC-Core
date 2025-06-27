using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Interfaces.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace DOL.GS.Infrastructure.Adapters
{
    /// <summary>
    /// Thread-safe adapter to bridge legacy GameObject with IGameObject interface
    /// Follows clean architecture principles with proper interface segregation
    /// </summary>
    public class GameObjectAdapter : IGameObject, IIdentifiable, IPositionable
    {
        protected readonly GameObject _legacyObject;
        protected readonly ILogger<GameObjectAdapter> _logger;
        private readonly ConcurrentDictionary<string, object> _cache = new();

        public GameObjectAdapter(GameObject legacyObject, ILogger<GameObjectAdapter> logger = null)
        {
            _legacyObject = legacyObject ?? throw new ArgumentNullException(nameof(legacyObject));
            _logger = logger;
        }

        #region IIdentifiable Implementation

        public virtual string ObjectId => _legacyObject.ObjectID.ToString();
        public virtual string Name => _legacyObject.Name ?? string.Empty;
        public virtual string InternalId => _legacyObject.InternalID ?? string.Empty;
        public virtual eObjectType ObjectType => GetObjectTypeFromLegacy();
        public virtual bool IsValid => _legacyObject != null;

        #endregion

        #region IPositionable Implementation

        public virtual int X => _legacyObject.X;
        public virtual int Y => _legacyObject.Y;
        public virtual int Z => _legacyObject.Z;
        public virtual ushort Heading => _legacyObject.Heading;
        public virtual ushort CurrentRegionId => _legacyObject.CurrentRegionID;

        public virtual double GetDistanceTo(IPositionable other)
        {
            if (other == null) return double.MaxValue;
            
            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public virtual bool IsWithinRadius(IPositionable other, int radius)
        {
            return other != null && GetDistanceTo(other) <= radius;
        }

        public virtual void MoveTo(int x, int y, int z, ushort heading, ushort regionId)
        {
            try
            {
                _legacyObject.MoveTo(regionId, x, y, z, heading);
                _logger?.LogDebug("Moved object {ObjectId} to position ({X}, {Y}, {Z})", ObjectId, x, y, z);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to move object {ObjectId}", ObjectId);
            }
        }

        #endregion

        #region Helper Methods

        protected virtual eObjectType GetObjectTypeFromLegacy()
        {
            // Map legacy GameObjectType to interface ObjectType
            // Note: These are different enums - eGameObjectType vs eObjectType
            return _legacyObject switch
            {
                GamePlayer _ => eObjectType.GenericItem, // Default fallback since ObjectType is for items
                GameNPC _ => eObjectType.GenericItem,    // Default fallback since ObjectType is for items
                GameStaticItem _ => eObjectType.GenericItem,
                _ => eObjectType.GenericItem
            };
        }

        protected T GetCachedValue<T>(string key, Func<T> valueFactory)
        {
            return (T)_cache.GetOrAdd(key, _ => valueFactory());
        }

        protected void InvalidateCache(string key = null)
        {
            if (key != null)
            {
                _cache.TryRemove(key, out _);
            }
            else
            {
                _cache.Clear();
            }
        }

        #endregion

        #region Legacy Object Access

        /// <summary>
        /// Provides access to the underlying legacy object for gradual migration
        /// </summary>
        public GameObject LegacyObject => _legacyObject;

        #endregion
    }

    /// <summary>
    /// Factory for creating appropriate adapters based on object type
    /// </summary>
    public static class GameObjectAdapterFactory
    {
        public static IGameObject CreateAdapter(GameObject legacyObject, IServiceProvider serviceProvider = null)
        {
            if (legacyObject == null) return null;

            var loggerFactory = serviceProvider?.GetService<ILoggerFactory>();

            return legacyObject switch
            {
                GamePlayer player => new GamePlayerAdapter(player, loggerFactory?.CreateLogger<GamePlayerAdapter>()),
                GameNPC npc => new GameNPCAdapter(npc, loggerFactory?.CreateLogger<GameNPCAdapter>()),
                _ => new GameObjectAdapter(legacyObject, loggerFactory?.CreateLogger<GameObjectAdapter>())
            };
        }
    }

    /// <summary>
    /// Extension methods for GameObject adapter creation
    /// </summary>
    public static class GameObjectAdapterExtensions
    {
        public static IGameObject ToAdapter(this GameObject gameObject, ILogger<GameObjectAdapter> logger = null)
        {
            return new GameObjectAdapter(gameObject, logger);
        }
    }
} 