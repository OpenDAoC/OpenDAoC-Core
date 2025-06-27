# Caching System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

**Game Rule Summary**: The caching system improves game performance by remembering frequently needed information instead of recalculating it every time. This makes the game run smoother and respond faster when you interact with NPCs, use items, or move around the world, especially in busy areas with many players.

The Caching System provides comprehensive performance optimization through strategic caching of frequently accessed data, calculations, and objects. It includes multiple specialized cache implementations for different data types and access patterns, significantly reducing computational overhead and database queries.

## Core Caching Architecture

### Cache Types and Strategies

```csharp
// Generic LRU cache interface
public interface ICache<TKey, TValue>
{
    TValue Get(TKey key);
    TValue GetOrAdd(TKey key, Func<TKey, TValue> factory);
    void Set(TKey key, TValue value);
    void Remove(TKey key);
    void Clear();
    int Count { get; }
    int MaxSize { get; }
}

// LRU (Least Recently Used) Cache Implementation
public class LRUCache<TKey, TValue> : ICache<TKey, TValue>
{
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _accessOrder;
    private readonly int _maxSize;
    private readonly Lock _lock = new();
    
    public LRUCache(int maxSize = 1000)
    {
        _maxSize = maxSize;
        _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(maxSize);
        _accessOrder = new LinkedList<CacheItem>();
    }
    
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _accessOrder.Remove(node);
                _accessOrder.AddFirst(node);
                return node.Value.Value;
            }
            
            // Create new entry
            var value = factory(key);
            var newItem = new CacheItem { Key = key, Value = value };
            var newNode = new LinkedListNode<CacheItem>(newItem);
            
            // Add to cache
            _cache[key] = newNode;
            _accessOrder.AddFirst(newNode);
            
            // Evict if over capacity
            if (_cache.Count > _maxSize)
            {
                var lastNode = _accessOrder.Last;
                _accessOrder.RemoveLast();
                _cache.Remove(lastNode.Value.Key);
            }
            
            return value;
        }
    }
    
    private class CacheItem
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }
}
```

## Specialized Cache Implementations

### Market Cache System

```csharp
public class MarketCache
{
    private static readonly Dictionary<string, List<DbInventoryItem>> _marketItems = new();
    private static readonly Dictionary<string, DateTime> _cacheTimestamps = new();
    private static readonly TimeSpan CACHE_EXPIRY = TimeSpan.FromMinutes(15);
    private static readonly Lock _lock = new();
    
    public static List<DbInventoryItem> GetMarketItems(string category)
    {
        lock (_lock)
        {
            if (_marketItems.TryGetValue(category, out var items) && 
                _cacheTimestamps.TryGetValue(category, out var timestamp) &&
                DateTime.UtcNow - timestamp < CACHE_EXPIRY)
            {
                return new List<DbInventoryItem>(items); // Return copy
            }
            
            // Load from database
            var freshItems = LoadMarketItemsFromDatabase(category);
            _marketItems[category] = freshItems;
            _cacheTimestamps[category] = DateTime.UtcNow;
            
            return new List<DbInventoryItem>(freshItems);
        }
    }
    
    public static void InvalidateCategory(string category)
    {
        lock (_lock)
        {
            _marketItems.Remove(category);
            _cacheTimestamps.Remove(category);
        }
    }
    
    public static void InvalidateAll()
    {
        lock (_lock)
        {
            _marketItems.Clear();
            _cacheTimestamps.Clear();
        }
    }
}
```

### Character Class Constructor Cache

```csharp
public static class CharacterClassCache
{
    private static readonly ConcurrentDictionary<int, Func<ICharacterClass>> _characterClassConstructorCache = new();
    
    public static ICharacterClass CreateCharacterClass(int classId)
    {
        if (_characterClassConstructorCache.TryGetValue(classId, out var constructor))
        {
            return constructor();
        }
        
        // Find and compile constructor
        var classType = FindCharacterClassType(classId);
        if (classType != null)
        {
            return _characterClassConstructorCache.GetOrAdd(classId, 
                (key) => CompiledConstructorFactory.CompileConstructor(classType, []) as Func<ICharacterClass>)();
        }
        
        return null;
    }
    
    public static void WarmCache()
    {
        // Pre-load all character class constructors
        for (int i = 1; i <= 60; i++) // All known class IDs
        {
            CreateCharacterClass(i);
        }
    }
    
    public static void ClearCache()
    {
        _characterClassConstructorCache.Clear();
    }
}
```

### Objects in Radius Cache

```csharp
public class ObjectsInRadiusCache
{
    private readonly Dictionary<(int x, int y, int radius, Type type), (List<GameObject> objects, long timestamp)> _cache = new();
    private const long CACHE_DURATION_MS = 100; // Cache for 100ms
    private readonly Lock _lock = new();
    
    public List<T> GetObjectsInRadius<T>(int x, int y, int radius) where T : GameObject
    {
        var key = (x, y, radius, typeof(T));
        long currentTime = GameLoop.GameLoopTime;
        
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached) && 
                currentTime - cached.timestamp < CACHE_DURATION_MS)
            {
                return cached.objects.Cast<T>().ToList();
            }
            
            // Calculate fresh results
            var objects = CalculateObjectsInRadius<T>(x, y, radius);
            _cache[key] = (objects.Cast<GameObject>().ToList(), currentTime);
            
            // Clean old entries
            CleanExpiredEntries(currentTime);
            
            return objects;
        }
    }
    
    private void CleanExpiredEntries(long currentTime)
    {
        var expiredKeys = _cache
            .Where(kvp => currentTime - kvp.Value.timestamp >= CACHE_DURATION_MS)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var key in expiredKeys)
        {
            _cache.Remove(key);
        }
    }
}
```

## Database-Level Caching

### Precached Tables System

```csharp
public class PrecachedTableManager
{
    private static readonly Dictionary<Type, IPrecachedTable> _precachedTables = new();
    
    public static void RegisterPrecachedTable<T>(IPrecachedTable<T> table) where T : DataObject
    {
        _precachedTables[typeof(T)] = table;
    }
    
    public static IPrecachedTable<T> GetPrecachedTable<T>() where T : DataObject
    {
        return _precachedTables.GetValueOrDefault(typeof(T)) as IPrecachedTable<T>;
    }
}

public interface IPrecachedTable<T> where T : DataObject
{
    T GetByPrimaryKey(object primaryKey);
    IEnumerable<T> GetAll();
    void RefreshCache();
    void InvalidateEntry(object primaryKey);
}

public class PrecachedTable<T> : IPrecachedTable<T> where T : DataObject, new()
{
    private readonly Dictionary<object, T> _cache = new();
    private readonly Lock _lock = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(5);
    
    public T GetByPrimaryKey(object primaryKey)
    {
        lock (_lock)
        {
            RefreshIfNeeded();
            return _cache.GetValueOrDefault(primaryKey);
        }
    }
    
    public void RefreshCache()
    {
        lock (_lock)
        {
            _cache.Clear();
            var allObjects = GameServer.Database.SelectAllObjects<T>();
            
            foreach (var obj in allObjects)
            {
                var primaryKey = GetPrimaryKeyValue(obj);
                if (primaryKey != null)
                {
                    _cache[primaryKey] = obj;
                }
            }
            
            _lastRefresh = DateTime.UtcNow;
        }
    }
    
    private void RefreshIfNeeded()
    {
        if (DateTime.UtcNow - _lastRefresh > _refreshInterval)
        {
            RefreshCache();
        }
    }
}
```

## Client-Side Caching

### Cached NPC Values

```csharp
public class CachedNpcValues
{
    public string Name { get; set; }
    public string GuildName { get; set; }
    public byte Level { get; set; }
    public byte Realm { get; set; }
    public ushort Model { get; set; }
    public byte Size { get; set; }
    public ushort Head { get; set; }
    public byte MoodFlag { get; set; }
    public short MaxSpeed { get; set; }
    public byte VisibleWeaponSlots { get; set; }
    public long CacheTime { get; set; }
    
    public bool IsExpired(long currentTime, long expireDuration = 30000)
    {
        return currentTime - CacheTime > expireDuration;
    }
}
```

### Cached Item Values

```csharp
public class CachedItemValues
{
    public string Name { get; set; }
    public ushort Model { get; set; }
    public byte Extension { get; set; }
    public int Color { get; set; }
    public byte Effect { get; set; }
    public short Emblems { get; set; }
    public long CacheTime { get; set; }
    
    public bool IsExpired(long currentTime, long expireDuration = 60000)
    {
        return currentTime - CacheTime > expireDuration;
    }
}
```

## Region and Zone Caching

### Zone Data Cache

```csharp
public static class ZoneCache
{
    private static readonly Dictionary<ushort, Zone> _zoneCache = new();
    private static readonly Dictionary<(ushort regionId, int x, int y), Zone> _positionCache = new();
    private static readonly Lock _lock = new();
    
    public static Zone GetZone(ushort zoneId)
    {
        lock (_lock)
        {
            return _zoneCache.GetValueOrDefault(zoneId);
        }
    }
    
    public static Zone GetZoneByPosition(ushort regionId, int x, int y)
    {
        var key = (regionId, x, y);
        
        lock (_lock)
        {
            if (_positionCache.TryGetValue(key, out var zone))
                return zone;
                
            // Calculate zone from position
            zone = CalculateZoneFromPosition(regionId, x, y);
            if (zone != null)
            {
                _positionCache[key] = zone;
            }
            
            return zone;
        }
    }
    
    public static void RegisterZone(Zone zone)
    {
        lock (_lock)
        {
            _zoneCache[zone.ID] = zone;
        }
    }
    
    public static void InvalidateRegion(ushort regionId)
    {
        lock (_lock)
        {
            var keysToRemove = _positionCache.Keys
                .Where(key => key.regionId == regionId)
                .ToList();
                
            foreach (var key in keysToRemove)
            {
                _positionCache.Remove(key);
            }
        }
    }
}
```

## Property Calculator Caching

```csharp
public class PropertyCalculatorCache
{
    private readonly Dictionary<(IPropertySource source, eProperty property), (int value, long timestamp)> _cache = new();
    private const long CACHE_DURATION = 1000; // 1 second cache
    private readonly Lock _lock = new();
    
    public int GetCachedValue(IPropertySource source, eProperty property, Func<int> calculator)
    {
        var key = (source, property);
        long currentTime = GameLoop.GameLoopTime;
        
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached) && 
                currentTime - cached.timestamp < CACHE_DURATION)
            {
                return cached.value;
            }
            
            // Calculate fresh value
            var value = calculator();
            _cache[key] = (value, currentTime);
            
            // Periodic cleanup
            if (_cache.Count > 10000)
            {
                CleanExpiredEntries(currentTime);
            }
            
            return value;
        }
    }
    
    public void InvalidateSource(IPropertySource source)
    {
        lock (_lock)
        {
            var keysToRemove = _cache.Keys
                .Where(key => key.source == source)
                .ToList();
                
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }
    }
    
    private void CleanExpiredEntries(long currentTime)
    {
        var expiredKeys = _cache
            .Where(kvp => currentTime - kvp.Value.timestamp >= CACHE_DURATION)
            .Select(kvp => kvp.Key)
            .Take(1000) // Limit cleanup batch size
            .ToList();
            
        foreach (var key in expiredKeys)
        {
            _cache.Remove(key);
        }
    }
}
```

## Line of Sight Cache

```csharp
public class LoSCache
{
    private readonly Dictionary<(int x1, int y1, int z1, int x2, int y2, int z2, ushort regionId), (bool result, long timestamp)> _cache = new();
    private const long CACHE_DURATION = 5000; // 5 second cache for LoS
    private readonly Lock _lock = new();
    
    public bool GetCachedLoS(int x1, int y1, int z1, int x2, int y2, int z2, ushort regionId, Func<bool> calculator)
    {
        var key = (x1, y1, z1, x2, y2, z2, regionId);
        long currentTime = GameLoop.GameLoopTime;
        
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached) && 
                currentTime - cached.timestamp < CACHE_DURATION)
            {
                return cached.result;
            }
            
            // Calculate fresh LoS
            var result = calculator();
            _cache[key] = (result, currentTime);
            
            // Reverse direction cache (A->B = B->A for LoS)
            var reverseKey = (x2, y2, z2, x1, y1, z1, regionId);
            _cache[reverseKey] = (result, currentTime);
            
            // Cleanup if cache gets too large
            if (_cache.Count > 5000)
            {
                CleanExpiredEntries(currentTime);
            }
            
            return result;
        }
    }
}
```

## Cache Performance Monitoring

```csharp
public static class CacheMetrics
{
    private static readonly Dictionary<string, CacheStats> _cacheStats = new();
    
    public static void RecordHit(string cacheName)
    {
        var stats = GetOrCreateStats(cacheName);
        Interlocked.Increment(ref stats.Hits);
    }
    
    public static void RecordMiss(string cacheName)
    {
        var stats = GetOrCreateStats(cacheName);
        Interlocked.Increment(ref stats.Misses);
    }
    
    public static void RecordEviction(string cacheName)
    {
        var stats = GetOrCreateStats(cacheName);
        Interlocked.Increment(ref stats.Evictions);
    }
    
    public static CacheStats GetStats(string cacheName)
    {
        return _cacheStats.GetValueOrDefault(cacheName, new CacheStats());
    }
    
    public static void LogAllStats()
    {
        foreach (var kvp in _cacheStats)
        {
            var stats = kvp.Value;
            var hitRate = stats.TotalRequests > 0 ? (double)stats.Hits / stats.TotalRequests : 0;
            
            Log.Info($"Cache {kvp.Key}: Hits={stats.Hits}, Misses={stats.Misses}, " +
                    $"HitRate={hitRate:P2}, Evictions={stats.Evictions}");
        }
    }
    
    private static CacheStats GetOrCreateStats(string cacheName)
    {
        return _cacheStats.GetOrAdd(cacheName, _ => new CacheStats());
    }
}

public class CacheStats
{
    public long Hits;
    public long Misses;
    public long Evictions;
    public long TotalRequests => Hits + Misses;
}
```

## Cache Configuration

```csharp
public static class CacheConfiguration
{
    // Cache sizes
    public static int MARKET_CACHE_SIZE = 1000;
    public static int OBJECT_RADIUS_CACHE_SIZE = 500;
    public static int CHARACTER_CLASS_CACHE_SIZE = 100;
    public static int PROPERTY_CACHE_SIZE = 10000;
    public static int LOS_CACHE_SIZE = 5000;
    
    // Cache durations (milliseconds)
    public static long MARKET_CACHE_DURATION = 900000; // 15 minutes
    public static long OBJECT_RADIUS_CACHE_DURATION = 100; // 100ms
    public static long PROPERTY_CACHE_DURATION = 1000; // 1 second
    public static long LOS_CACHE_DURATION = 5000; // 5 seconds
    public static long CLIENT_CACHE_DURATION = 30000; // 30 seconds
    
    // Cache behavior
    public static bool ENABLE_MARKET_CACHE = true;
    public static bool ENABLE_PROPERTY_CACHE = true;
    public static bool ENABLE_LOS_CACHE = true;
    public static bool ENABLE_RADIUS_CACHE = true;
    public static bool LOG_CACHE_STATISTICS = false;
}
```

## Implementation Status

**Completed**:
- ✅ LRU cache implementation
- ✅ Market cache system
- ✅ Character class constructor cache
- ✅ Objects in radius cache
- ✅ Database precaching
- ✅ Client-side caching
- ✅ Property calculator cache
- ✅ Line of sight cache

**In Progress**:
- 🔄 Advanced cache eviction policies
- 🔄 Distributed caching support
- 🔄 Cache warming strategies

**Planned**:
- ⏳ Machine learning-based cache prediction
- ⏳ Dynamic cache sizing
- ⏳ Cross-server cache synchronization

## References

- **Market Cache**: `GameServer/gameutils/MarketCache.cs`
- **Class Cache**: `GameServer/gameutils/ScriptMgr.cs`
- **Radius Cache**: `GameServer/gameobjects/GameObject.cs`
- **Database Cache**: Various precached table implementations 