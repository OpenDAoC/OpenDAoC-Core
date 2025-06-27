using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Text;

namespace DOL.GS.Infrastructure
{
    #region Core Interfaces

    /// <summary>
    /// Interface for objects that can be reset/cleaned when returned to pool.
    /// DAoC Rule: All pooled objects must be resettable to prevent state leakage.
    /// </summary>
    public interface IResettable
    {
        /// <summary>
        /// Resets the object to its initial state for reuse.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// High-performance object pool manager for game objects.
    /// Performance Target: <100ns for Get/Return operations.
    /// </summary>
    public interface IGameObjectPoolManager
    {
        /// <summary>
        /// Gets a pool for the specified type, creating it if necessary.
        /// </summary>
        ObjectPool<T> GetPool<T>() where T : class, IResettable, new();
        
        /// <summary>
        /// Gets a pool with custom policy.
        /// </summary>
        ObjectPool<T> GetPool<T>(IPooledObjectPolicy<T> policy) where T : class;
        
        /// <summary>
        /// Gets pool statistics for monitoring.
        /// </summary>
        PoolStatistics GetStatistics<T>() where T : class;
        
        /// <summary>
        /// Gets all pool statistics for monitoring dashboard.
        /// </summary>
        IDictionary<Type, PoolStatistics> GetAllStatistics();
        
        /// <summary>
        /// Clears all pools (for testing/cleanup).
        /// </summary>
        void ClearAll();
    }

    /// <summary>
    /// Thread-safe statistics for object pool monitoring.
    /// </summary>
    public class PoolStatistics
    {
        private long _totalCreated;
        private long _totalReturned;
        private long _lastAccessedTicks = DateTime.UtcNow.Ticks;

        /// <summary>
        /// Object type for this pool
        /// </summary>
        public Type ObjectType { get; set; }

        /// <summary>
        /// Total number of objects created (thread-safe)
        /// </summary>
        public long TotalCreated => Interlocked.Read(ref _totalCreated);

        /// <summary>
        /// Total number of objects returned (thread-safe)
        /// </summary>
        public long TotalReturned => Interlocked.Read(ref _totalReturned);

        /// <summary>
        /// Last access time (thread-safe)
        /// </summary>
        public DateTime LastAccessed => new DateTime(Interlocked.Read(ref _lastAccessedTicks));

        /// <summary>
        /// Current pool efficiency as percentage
        /// </summary>
        public double Efficiency => TotalCreated == 0 ? 0 : (double)TotalReturned / TotalCreated * 100;

        /// <summary>
        /// Average objects per second created
        /// </summary>
        public double CreationRate
        {
            get
            {
                var elapsed = DateTime.UtcNow - new DateTime(Interlocked.Read(ref _lastAccessedTicks));
                return elapsed.TotalSeconds > 0 ? TotalCreated / elapsed.TotalSeconds : 0;
            }
        }

        internal void IncrementCreated()
        {
            Interlocked.Increment(ref _totalCreated);
            UpdateLastAccessed();
        }

        internal void IncrementReturned()
        {
            Interlocked.Increment(ref _totalReturned);
            UpdateLastAccessed();
        }

        private void UpdateLastAccessed()
        {
            Interlocked.Exchange(ref _lastAccessedTicks, DateTime.UtcNow.Ticks);
        }
    }

    #endregion

    #region Pool Policies

    /// <summary>
    /// Base pool policy for resettable game objects.
    /// </summary>
    public abstract class GameObjectPoolPolicy<T> : IPooledObjectPolicy<T> where T : class, IResettable, new()
    {
        private readonly ILogger<GameObjectPoolPolicy<T>> _logger;

        protected GameObjectPoolPolicy(ILogger<GameObjectPoolPolicy<T>> logger = null)
        {
            _logger = logger;
        }

        public virtual T Create()
        {
            try
            {
                return new T();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create pooled object of type {ObjectType}", typeof(T).Name);
                throw;
            }
        }

        public virtual bool Return(T obj)
        {
            if (obj == null)
                return false;

            try
            {
                obj.Reset();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to reset pooled object of type {ObjectType}, discarding", typeof(T).Name);
                return false;
            }
        }
    }

    /// <summary>
    /// Default policy for generic resettable objects.
    /// </summary>
    public class DefaultGameObjectPoolPolicy<T> : GameObjectPoolPolicy<T> where T : class, IResettable, new()
    {
        public DefaultGameObjectPoolPolicy(ILogger<GameObjectPoolPolicy<T>> logger = null) : base(logger) { }
    }

    /// <summary>
    /// Specialized policy for attack context objects with validation.
    /// </summary>
    public class AttackContextPoolPolicy : GameObjectPoolPolicy<AttackContext>
    {
        public AttackContextPoolPolicy(ILogger<AttackContextPoolPolicy> logger = null) : base(logger) { }

        public override bool Return(AttackContext obj)
        {
            // Validate object state before reset
            if (obj == null || obj.IsCorrupted())
                return false;

            return base.Return(obj);
        }
    }

    /// <summary>
    /// Specialized policy for damage calculation objects.
    /// </summary>
    public class DamageCalculationPoolPolicy : GameObjectPoolPolicy<DamageCalculation>
    {
        public DamageCalculationPoolPolicy(ILogger<DamageCalculationPoolPolicy> logger = null) : base(logger) { }
    }

    /// <summary>
    /// Specialized policy for property calculation context.
    /// </summary>
    public class PropertyCalculationContextPoolPolicy : GameObjectPoolPolicy<PropertyCalculationContext>
    {
        public PropertyCalculationContextPoolPolicy(ILogger<PropertyCalculationContextPoolPolicy> logger = null) : base(logger) { }
    }

    #endregion

    #region Pool Manager Implementation

    /// <summary>
    /// High-performance object pool manager implementation.
    /// Uses concurrent collections for thread-safety without locking.
    /// </summary>
    public class GameObjectPoolManager : IGameObjectPoolManager
    {
        private readonly ConcurrentDictionary<Type, object> _pools = new();
        private readonly ConcurrentDictionary<Type, PoolStatistics> _statistics = new();
        private readonly ConcurrentDictionary<Type, IPooledObjectPolicy<object>> _policies = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameObjectPoolManager> _logger;
        private readonly ObjectPoolingOptions _options;

        public GameObjectPoolManager(
            IServiceProvider serviceProvider, 
            ILogger<GameObjectPoolManager> logger,
            ObjectPoolingOptions options = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options ?? new ObjectPoolingOptions();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ObjectPool<T> GetPool<T>() where T : class, IResettable, new()
        {
            var type = typeof(T);
            
            if (_pools.TryGetValue(type, out var existingPool))
                return (ObjectPool<T>)existingPool;

            // Double-checked locking for thread safety
            lock (_pools)
            {
                if (_pools.TryGetValue(type, out existingPool))
                    return (ObjectPool<T>)existingPool;

                // Create pool with default policy
                var policy = GetOrCreatePolicy<T>();
                return CreatePoolInternal<T>(policy);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ObjectPool<T> GetPool<T>(IPooledObjectPolicy<T> policy) where T : class
        {
            var type = typeof(T);
            
            if (_pools.TryGetValue(type, out var existingPool))
                return (ObjectPool<T>)existingPool;

            // Double-checked locking for thread safety
            lock (_pools)
            {
                if (_pools.TryGetValue(type, out existingPool))
                    return (ObjectPool<T>)existingPool;

                return CreatePoolInternal<T>(policy);
            }
        }

        private ObjectPool<T> CreatePoolInternal<T>(IPooledObjectPolicy<T> policy) where T : class
        {
            var type = typeof(T);
            
            // Use DefaultObjectPool for high performance
            var pool = new DefaultObjectPool<T>(policy, _options.DefaultMaxPoolSize);
            var instrumentedPool = _options.EnableStatistics 
                ? (ObjectPool<T>)new InstrumentedObjectPool<T>(pool, GetOrCreateStatistics(type))
                : (ObjectPool<T>)pool;
            
            _pools[type] = instrumentedPool;
            
            _logger?.LogInformation("Created object pool for type {ObjectType} with max size {MaxSize}", 
                type.Name, _options.DefaultMaxPoolSize);
                
            return instrumentedPool;
        }

        private IPooledObjectPolicy<T> GetOrCreatePolicy<T>() where T : class, IResettable, new()
        {
            var type = typeof(T);
            
            if (_policies.TryGetValue(type, out var existingPolicy))
                return (IPooledObjectPolicy<T>)existingPolicy;

            // Try to get policy from DI container first
            var policy = _serviceProvider.GetService<IPooledObjectPolicy<T>>() 
                      ?? new DefaultGameObjectPoolPolicy<T>();

            _policies[type] = (IPooledObjectPolicy<object>)policy;
            return policy;
        }

        public PoolStatistics GetStatistics<T>() where T : class
        {
            return GetOrCreateStatistics(typeof(T));
        }

        public IDictionary<Type, PoolStatistics> GetAllStatistics()
        {
            return new Dictionary<Type, PoolStatistics>(_statistics);
        }

        public void ClearAll()
        {
            _pools.Clear();
            _statistics.Clear();
            _policies.Clear();
        }

        private PoolStatistics GetOrCreateStatistics(Type type)
        {
            return _statistics.GetOrAdd(type, _ => new PoolStatistics
            {
                ObjectType = type
            });
        }
    }

    #endregion

    #region Instrumented Pool Wrapper

    /// <summary>
    /// Wrapper that adds performance monitoring to object pools.
    /// </summary>
    public class InstrumentedObjectPool<T> : ObjectPool<T>, IDisposable where T : class
    {
        private readonly ObjectPool<T> _innerPool;
        private readonly PoolStatistics _statistics;
        private readonly Stopwatch _lifetimeStopwatch;

        public InstrumentedObjectPool(ObjectPool<T> innerPool, PoolStatistics statistics)
        {
            _innerPool = innerPool;
            _statistics = statistics;
            _lifetimeStopwatch = Stopwatch.StartNew();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T Get()
        {
            var obj = _innerPool.Get();
            
            // Update statistics
            _statistics.IncrementCreated();
            
            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Return(T obj)
        {
            _innerPool.Return(obj);
            
            // Update statistics
            _statistics.IncrementReturned();
        }

        public void Dispose()
        {
            _lifetimeStopwatch?.Stop();
            // Note: Statistics are automatically updated through Get/Return methods
            // Lifetime tracking removed for simplicity and performance
        }
    }

    #endregion

    #region Poolable Objects

    /// <summary>
    /// Poolable attack context for combat calculations.
    /// DAoC Rule: Attack context contains all data needed for attack resolution.
    /// </summary>
    public class AttackContext : IResettable
    {
        // Attack participants
        public object Attacker { get; set; }
        public object Target { get; set; }
        
        // Attack data
        public object Weapon { get; set; }
        public int AttackType { get; set; }
        public double AttackSpeed { get; set; }
        
        // Modifiers
        public int StrengthModifier { get; set; }
        public int DexterityModifier { get; set; }
        public int WeaponSkillModifier { get; set; }
        
        // Results
        public bool Hit { get; set; }
        public int Damage { get; set; }
        public bool Critical { get; set; }
        public int DefenseType { get; set; }
        
        // Timing
        public long Timestamp { get; set; }

        public void Reset()
        {
            Attacker = null;
            Target = null;
            Weapon = null;
            AttackType = 0;
            AttackSpeed = 0;
            StrengthModifier = 0;
            DexterityModifier = 0;
            WeaponSkillModifier = 0;
            Hit = false;
            Damage = 0;
            Critical = false;
            DefenseType = 0;
            Timestamp = 0;
        }

        /// <summary>
        /// Validates object state for corruption detection.
        /// </summary>
        public bool IsCorrupted()
        {
            // Basic corruption checks
            return AttackSpeed < 0 || Damage < 0 || WeaponSkillModifier < -1000;
        }
    }

    /// <summary>
    /// Poolable damage calculation context.
    /// DAoC Rule: Damage calculation involves base damage, variance, crits, and resistances.
    /// </summary>
    public class DamageCalculation : IResettable
    {
        // Base values
        public int BaseDamage { get; set; }
        public double DamageVariance { get; set; }
        
        // Modifiers
        public double StrengthBonus { get; set; }
        public double WeaponSkillBonus { get; set; }
        public double SpecializationBonus { get; set; }
        
        // Resistances
        public int ArmorFactor { get; set; }
        public int ArmorAbsorption { get; set; }
        public int MagicResistance { get; set; }
        
        // Results
        public int FinalDamage { get; set; }
        public bool IsCritical { get; set; }
        public double CriticalMultiplier { get; set; }

        public void Reset()
        {
            BaseDamage = 0;
            DamageVariance = 0;
            StrengthBonus = 0;
            WeaponSkillBonus = 0;
            SpecializationBonus = 0;
            ArmorFactor = 0;
            ArmorAbsorption = 0;
            MagicResistance = 0;
            FinalDamage = 0;
            IsCritical = false;
            CriticalMultiplier = 0;
        }
    }

    /// <summary>
    /// Poolable property calculation context.
    /// DAoC Rule: Property calculations involve base, item, buff, and debuff values.
    /// </summary>
    public class PropertyCalculationContext : IResettable
    {
        // Source data
        public object PropertySource { get; set; }
        public int PropertyType { get; set; }
        
        // Base values
        public int BaseValue { get; set; }
        public int ItemBonus { get; set; }
        public int BuffBonus { get; set; }
        public int DebuffValue { get; set; }
        
        // Caps and limits
        public int LevelCap { get; set; }
        public int ItemCap { get; set; }
        public int BuffCap { get; set; }
        
        // Result
        public int FinalValue { get; set; }
        public bool WasCapped { get; set; }

        public void Reset()
        {
            PropertySource = null;
            PropertyType = 0;
            BaseValue = 0;
            ItemBonus = 0;
            BuffBonus = 0;
            DebuffValue = 0;
            LevelCap = 0;
            ItemCap = 0;
            BuffCap = 0;
            FinalValue = 0;
            WasCapped = false;
        }
    }

    #endregion

    #region Service Registration Extensions

    /// <summary>
    /// Extension methods for registering object pooling infrastructure
    /// </summary>
    public static class ObjectPoolingServiceExtensions
    {
        /// <summary>
        /// Registers object pooling infrastructure with the service container
        /// </summary>
        public static IServiceCollection AddObjectPooling(this IServiceCollection services)
        {
            services.AddSingleton<ObjectPoolingOptions>();
            services.AddSingleton<IGameObjectPoolManager, GameObjectPoolManager>();
            services.AddSingleton<ObjectPoolDiagnostics>();
            
            // Register common pooled objects
            services.AddSingleton<ObjectPool<AttackContext>>(provider =>
            {
                var policy = new DefaultPooledObjectPolicy<AttackContext>();
                return new DefaultObjectPool<AttackContext>(policy);
            });
            
            services.AddSingleton<ObjectPool<DamageCalculation>>(provider =>
            {
                var policy = new DefaultPooledObjectPolicy<DamageCalculation>();
                return new DefaultObjectPool<DamageCalculation>(policy);
            });
            
            services.AddSingleton<ObjectPool<PropertyCalculationContext>>(provider =>
            {
                var policy = new DefaultPooledObjectPolicy<PropertyCalculationContext>();
                return new DefaultObjectPool<PropertyCalculationContext>(policy);
            });
            
            return services;
        }

        /// <summary>
        /// Registers object pooling with custom configuration.
        /// </summary>
        public static IServiceCollection AddObjectPooling(this IServiceCollection services, 
            Action<ObjectPoolingOptions> configureOptions)
        {
            services.Configure(configureOptions);
            return services.AddObjectPooling();
        }
    }

    /// <summary>
    /// Configuration options for object pooling.
    /// </summary>
    public class ObjectPoolingOptions
    {
        /// <summary>
        /// Default maximum pool size.
        /// </summary>
        public int DefaultMaxPoolSize { get; set; } = 1000;
        
        /// <summary>
        /// Whether to enable pool statistics collection.
        /// </summary>
        public bool EnableStatistics { get; set; } = true;
        
        /// <summary>
        /// Whether to log pool operations for debugging.
        /// </summary>
        public bool EnableDebugLogging { get; set; } = false;
    }

    #endregion

    #region Usage Example Helper

    /// <summary>
    /// Example service showing proper object pool usage patterns.
    /// </summary>
    public class ExamplePooledCombatService
    {
        private readonly ObjectPool<AttackContext> _attackContextPool;
        private readonly ObjectPool<DamageCalculation> _damageCalculationPool;
        private readonly ILogger<ExamplePooledCombatService> _logger;

        public ExamplePooledCombatService(
            ObjectPool<AttackContext> attackContextPool,
            ObjectPool<DamageCalculation> damageCalculationPool,
            ILogger<ExamplePooledCombatService> logger)
        {
            _attackContextPool = attackContextPool;
            _damageCalculationPool = damageCalculationPool;
            _logger = logger;
        }

        /// <summary>
        /// Example of proper pooled object usage with try/finally pattern.
        /// Performance Target: Zero allocations in this hot path.
        /// </summary>
        public void ProcessAttackExample(object attacker, object target, object weapon)
        {
            // Rent objects from pools
            var attackContext = _attackContextPool.Get();
            var damageCalc = _damageCalculationPool.Get();
            
            try
            {
                // Initialize context
                attackContext.Attacker = attacker;
                attackContext.Target = target;
                attackContext.Weapon = weapon;
                attackContext.Timestamp = Environment.TickCount64;
                
                // Perform calculations (zero additional allocations)
                damageCalc.BaseDamage = CalculateBaseDamage(attackContext);
                damageCalc.FinalDamage = ApplyModifiers(damageCalc);
                
                // Use results
                ApplyDamage(target, damageCalc.FinalDamage);
                
                _logger?.LogDebug("Processed attack: {Damage} damage", damageCalc.FinalDamage);
            }
            finally
            {
                // Always return objects to pools
                _attackContextPool.Return(attackContext);
                _damageCalculationPool.Return(damageCalc);
            }
        }

        private int CalculateBaseDamage(AttackContext context) => 100; // Stub
        private int ApplyModifiers(DamageCalculation calc) => calc.BaseDamage; // Stub
        private void ApplyDamage(object target, int damage) { } // Stub
    }

    #endregion

    #region Object Pool Diagnostics

    /// <summary>
    /// Helper class for logging and reporting object pool statistics.
    /// </summary>
    public class ObjectPoolDiagnostics
    {
        private readonly ConcurrentDictionary<string, PoolStatistics> _poolStatistics = new();
        private readonly ILogger<ObjectPoolDiagnostics> _logger;

        public ObjectPoolDiagnostics(ILogger<ObjectPoolDiagnostics> logger)
        {
            _logger = logger;
        }

        public void LogStatistics()
        {
            foreach (var (poolName, stats) in _poolStatistics)
            {
                _logger.LogInformation(
                    "Pool '{PoolName}': Created={Created}, Returned={Returned}, Efficiency={Efficiency:F1}%, CreationRate={CreationRate:F2}/sec",
                    poolName, stats.TotalCreated, stats.TotalReturned, stats.Efficiency, stats.CreationRate);
            }
        }

        public string GetFormattedReport()
        {
            var report = new StringBuilder();
            report.AppendLine("Object Pool Diagnostics Report");
            report.AppendLine("=" + new string('=', 40));

            foreach (var (poolName, stats) in _poolStatistics)
            {
                report.AppendLine($"Pool: {poolName}");
                report.AppendLine($"  Type: {stats.ObjectType?.Name ?? "Unknown"}");
                report.AppendLine($"  Created: {stats.TotalCreated}");
                report.AppendLine($"  Returned: {stats.TotalReturned}");
                report.AppendLine($"  Efficiency: {stats.Efficiency:F1}%");
                report.AppendLine($"  Creation Rate: {stats.CreationRate:F2}/sec");
                report.AppendLine($"  Last Accessed: {stats.LastAccessed:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine();
            }

            return report.ToString();
        }
    }

    #endregion
} 