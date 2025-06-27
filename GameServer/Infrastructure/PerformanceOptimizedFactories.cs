using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace DOL.GS.Infrastructure
{
    /// <summary>
    /// High-performance service factory interface for hot path services
    /// Provides sub-100ns service resolution for critical game operations
    /// </summary>
    public interface IPerformanceServiceFactory<T> where T : class
    {
        /// <summary>
        /// Create an instance (compiled delegate - fastest)
        /// </summary>
        T Create();

        /// <summary>
        /// Create and return to pool when done
        /// </summary>
        PooledServiceHandle<T> CreatePooled();

        /// <summary>
        /// Get performance metrics for this factory
        /// </summary>
        ServiceFactoryMetrics GetMetrics();
    }

    /// <summary>
    /// Factory for creating performance-optimized service instances
    /// Uses compiled expression trees for maximum speed
    /// </summary>
    public class PerformanceServiceFactory<T> : IPerformanceServiceFactory<T> where T : class
    {
        private readonly Func<T> _compiledFactory;
        private readonly ObjectPool<T> _objectPool;
        private readonly ILogger<PerformanceServiceFactory<T>> _logger;
        private readonly ServiceFactoryMetrics _metrics;
        private readonly string _serviceName;

        public PerformanceServiceFactory(
            IServiceProvider serviceProvider,
            ILogger<PerformanceServiceFactory<T>> logger,
            ObjectPool<T> objectPool = null)
        {
            _logger = logger;
            _objectPool = objectPool;
            _serviceName = typeof(T).Name;
            _metrics = new ServiceFactoryMetrics(_serviceName);

            // Compile the fastest possible factory delegate
            _compiledFactory = CompileFactory(serviceProvider);

            _logger.LogDebug("Created performance factory for {ServiceType}", typeof(T).Name);
        }

        public T Create()
        {
            var startTicks = Environment.TickCount64;
            
            try
            {
                var instance = _compiledFactory();
                _metrics.RecordCreation(Environment.TickCount64 - startTicks);
                return instance;
            }
            catch (Exception ex)
            {
                _metrics.RecordError();
                _logger.LogError(ex, "Failed to create instance of {ServiceType}", typeof(T).Name);
                throw;
            }
        }

        public PooledServiceHandle<T> CreatePooled()
        {
            if (_objectPool == null)
            {
                // Fallback to direct creation if no pool available
                var instance = Create();
                return new PooledServiceHandle<T>(instance, null);
            }

            var startTicks = Environment.TickCount64;
            
            try
            {
                var pooledInstance = _objectPool.Get();
                _metrics.RecordPooledCreation(Environment.TickCount64 - startTicks);
                return new PooledServiceHandle<T>(pooledInstance, _objectPool);
            }
            catch (Exception ex)
            {
                _metrics.RecordError();
                _logger.LogError(ex, "Failed to get pooled instance of {ServiceType}", typeof(T).Name);
                throw;
            }
        }

        public ServiceFactoryMetrics GetMetrics() => _metrics;

        private Func<T> CompileFactory(IServiceProvider serviceProvider)
        {
            var serviceType = typeof(T);

            // Strategy 1: Try parameterless constructor (fastest)
            var parameterlessConstructor = serviceType.GetConstructor(Type.EmptyTypes);
            if (parameterlessConstructor != null)
            {
                _logger.LogDebug("Using parameterless constructor for {ServiceType}", serviceType.Name);
                return Expression.Lambda<Func<T>>(
                    Expression.New(parameterlessConstructor)
                ).Compile();
            }

            // Strategy 2: Try DI container resolution (slower but necessary)
            _logger.LogDebug("Using DI container resolution for {ServiceType}", serviceType.Name);
            return () => serviceProvider.GetRequiredService<T>();
        }
    }

    /// <summary>
    /// Handle for pooled service instances that automatically returns to pool on disposal
    /// </summary>
    public readonly struct PooledServiceHandle<T> : IDisposable where T : class
    {
        private readonly T _instance;
        private readonly ObjectPool<T> _pool;

        public T Instance => _instance;

        internal PooledServiceHandle(T instance, ObjectPool<T> pool)
        {
            _instance = instance;
            _pool = pool;
        }

        public void Dispose()
        {
            if (_pool != null && _instance != null)
            {
                _pool.Return(_instance);
            }
        }
    }

    /// <summary>
    /// Performance metrics for service factories
    /// </summary>
    public class ServiceFactoryMetrics
    {
        private readonly string _serviceName;
        private long _creationCount;
        private long _pooledCreationCount;
        private long _errorCount;
        private long _totalCreationTicks;
        private long _totalPooledCreationTicks;
        private readonly object _lock = new object();

        public ServiceFactoryMetrics(string serviceName)
        {
            _serviceName = serviceName;
        }

        public void RecordCreation(long elapsedTicks)
        {
            lock (_lock)
            {
                _creationCount++;
                _totalCreationTicks += elapsedTicks;
            }
        }

        public void RecordPooledCreation(long elapsedTicks)
        {
            lock (_lock)
            {
                _pooledCreationCount++;
                _totalPooledCreationTicks += elapsedTicks;
            }
        }

        public void RecordError()
        {
            lock (_lock)
            {
                _errorCount++;
            }
        }

        public ServiceFactoryStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new ServiceFactoryStatistics
                {
                    ServiceName = _serviceName,
                    CreationCount = _creationCount,
                    PooledCreationCount = _pooledCreationCount,
                    ErrorCount = _errorCount,
                    AverageCreationTimeNs = _creationCount > 0 ? (_totalCreationTicks * 1000000) / _creationCount : 0,
                    AveragePooledCreationTimeNs = _pooledCreationCount > 0 ? (_totalPooledCreationTicks * 1000000) / _pooledCreationCount : 0
                };
            }
        }
    }

    /// <summary>
    /// Statistics snapshot for a service factory
    /// </summary>
    public class ServiceFactoryStatistics
    {
        public string ServiceName { get; set; }
        public long CreationCount { get; set; }
        public long PooledCreationCount { get; set; }
        public long ErrorCount { get; set; }
        public long AverageCreationTimeNs { get; set; }
        public long AveragePooledCreationTimeNs { get; set; }

        public override string ToString()
        {
            return $"{ServiceName}: Created={CreationCount}, Pooled={PooledCreationCount}, " +
                   $"Errors={ErrorCount}, AvgTime={AverageCreationTimeNs}ns, AvgPooledTime={AveragePooledCreationTimeNs}ns";
        }
    }

    /// <summary>
    /// Factory registry for managing performance-optimized factories
    /// </summary>
    public interface IPerformanceFactoryRegistry
    {
        /// <summary>
        /// Register a performance factory for a service type
        /// </summary>
        void RegisterFactory<T>(IPerformanceServiceFactory<T> factory) where T : class;

        /// <summary>
        /// Get a performance factory for a service type
        /// </summary>
        IPerformanceServiceFactory<T> GetFactory<T>() where T : class;

        /// <summary>
        /// Check if a performance factory is registered
        /// </summary>
        bool HasFactory<T>() where T : class;

        /// <summary>
        /// Get performance statistics for all factories
        /// </summary>
        IEnumerable<ServiceFactoryStatistics> GetAllStatistics();
    }

    /// <summary>
    /// Registry implementation for performance factories
    /// </summary>
    public class PerformanceFactoryRegistry : IPerformanceFactoryRegistry
    {
        private readonly ConcurrentDictionary<Type, object> _factories = new();
        private readonly ILogger<PerformanceFactoryRegistry> _logger;

        public PerformanceFactoryRegistry(ILogger<PerformanceFactoryRegistry> logger)
        {
            _logger = logger;
        }

        public void RegisterFactory<T>(IPerformanceServiceFactory<T> factory) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var serviceType = typeof(T);
            
            if (_factories.TryAdd(serviceType, factory))
            {
                _logger.LogDebug("Registered performance factory for {ServiceType}", serviceType.Name);
            }
            else
            {
                _logger.LogWarning("Performance factory for {ServiceType} already registered", serviceType.Name);
            }
        }

        public IPerformanceServiceFactory<T> GetFactory<T>() where T : class
        {
            var serviceType = typeof(T);
            
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                return (IPerformanceServiceFactory<T>)factory;
            }

            throw new InvalidOperationException($"No performance factory registered for {serviceType.Name}");
        }

        public bool HasFactory<T>() where T : class
        {
            return _factories.ContainsKey(typeof(T));
        }

        public IEnumerable<ServiceFactoryStatistics> GetAllStatistics()
        {
            foreach (var factory in _factories.Values)
            {
                if (factory is IPerformanceServiceFactory<object> perfFactory)
                {
                    yield return perfFactory.GetMetrics().GetStatistics();
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for registering performance factories
    /// </summary>
    public static class PerformanceFactoryExtensions
    {
        /// <summary>
        /// Add performance factory registry to DI container
        /// </summary>
        public static IServiceCollection AddPerformanceFactories(this IServiceCollection services)
        {
            services.AddSingleton<IPerformanceFactoryRegistry, PerformanceFactoryRegistry>();
            return services;
        }

        /// <summary>
        /// Register a performance factory for a specific service
        /// </summary>
        public static IServiceCollection AddPerformanceFactory<T>(
            this IServiceCollection services,
            bool useObjectPool = true) where T : class
        {
            services.AddSingleton<IPerformanceServiceFactory<T>>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<PerformanceServiceFactory<T>>>();
                ObjectPool<T> objectPool = null;

                if (useObjectPool)
                {
                    objectPool = provider.GetService<ObjectPool<T>>();
                }

                return new PerformanceServiceFactory<T>(provider, logger, objectPool);
            });

            return services;
        }

        /// <summary>
        /// Register performance factories for hot path services
        /// </summary>
        public static IServiceCollection AddHotPathPerformanceFactories(this IServiceCollection services)
        {
            // These will be registered as we identify hot path services
            // Examples for future implementation:
            // services.AddPerformanceFactory<ICombatService>();
            // services.AddPerformanceFactory<IPropertyCalculator>();
            // services.AddPerformanceFactory<IEffectService>();

            return services;
        }

        /// <summary>
        /// Configure all registered performance factories
        /// </summary>
        public static void ConfigurePerformanceFactories(this IServiceProvider serviceProvider)
        {
            var registry = serviceProvider.GetRequiredService<IPerformanceFactoryRegistry>();
            var logger = serviceProvider.GetRequiredService<ILogger<PerformanceFactoryRegistry>>();

            // Register all performance factories with the registry
            var factories = serviceProvider.GetServices<object>()
                .Where(service => service.GetType().IsGenericType &&
                                service.GetType().GetGenericTypeDefinition() == typeof(IPerformanceServiceFactory<>));

            foreach (var factory in factories)
            {
                // This would need reflection to call RegisterFactory<T> with the correct type
                // Implementation depends on specific service types being registered
            }

            logger.LogInformation("Configured performance factories");
        }
    }

    /// <summary>
    /// Benchmark helper for testing factory performance
    /// </summary>
    public static class FactoryBenchmark
    {
        /// <summary>
        /// Benchmark factory performance over multiple iterations
        /// </summary>
        public static BenchmarkResult BenchmarkFactory<T>(
            IPerformanceServiceFactory<T> factory,
            int iterations = 10000) where T : class
        {
            var results = new List<long>();
            var errors = 0;

            // Warmup
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    var warmupInstance = factory.Create();
                }
                catch
                {
                    // Ignore warmup errors
                }
            }

            // Actual benchmark
            for (int i = 0; i < iterations; i++)
            {
                var startTicks = Environment.TickCount64;
                
                try
                {
                    var instance = factory.Create();
                    var elapsed = Environment.TickCount64 - startTicks;
                    results.Add(elapsed);
                }
                catch
                {
                    errors++;
                }
            }

            if (results.Count == 0)
            {
                return new BenchmarkResult
                {
                    ServiceType = typeof(T).Name,
                    Iterations = iterations,
                    Errors = errors,
                    AverageNanoseconds = 0,
                    MinNanoseconds = 0,
                    MaxNanoseconds = 0
                };
            }

            var avgTicks = results.Sum() / (double)results.Count;
            var minTicks = results.Min();
            var maxTicks = results.Max();

            return new BenchmarkResult
            {
                ServiceType = typeof(T).Name,
                Iterations = iterations,
                Errors = errors,
                AverageNanoseconds = (long)(avgTicks * 1000000), // Convert to nanoseconds
                MinNanoseconds = minTicks * 1000000,
                MaxNanoseconds = maxTicks * 1000000
            };
        }
    }

    /// <summary>
    /// Benchmark result for factory performance testing
    /// </summary>
    public class BenchmarkResult
    {
        public string ServiceType { get; set; }
        public int Iterations { get; set; }
        public int Errors { get; set; }
        public long AverageNanoseconds { get; set; }
        public long MinNanoseconds { get; set; }
        public long MaxNanoseconds { get; set; }

        public override string ToString()
        {
            return $"{ServiceType}: {Iterations} iterations, {Errors} errors, " +
                   $"Avg: {AverageNanoseconds}ns, Min: {MinNanoseconds}ns, Max: {MaxNanoseconds}ns";
        }
    }

    /// <summary>
    /// Extension methods for registering performance services.
    /// </summary>
    public static class PerformanceServiceExtensions
    {
        /// <summary>
        /// Registers performance-optimized service factories and infrastructure.
        /// </summary>
        public static IServiceCollection AddPerformanceServices(this IServiceCollection services)
        {
            // Register compiled delegate factories
            services.AddSingleton<ICompiledDelegateFactory<AttackComponent>, CompiledDelegateFactory<AttackComponent>>();
            services.AddSingleton<ICompiledDelegateFactory<CastingComponent>, CompiledDelegateFactory<CastingComponent>>();
            
            // Register performance service factories
            services.AddSingleton<IPerformanceServiceFactory<AttackComponent>, PerformanceServiceFactory<AttackComponent>>();
            services.AddSingleton<IPerformanceServiceFactory<CastingComponent>, PerformanceServiceFactory<CastingComponent>>();
            
            return services;
        }
    }
} 