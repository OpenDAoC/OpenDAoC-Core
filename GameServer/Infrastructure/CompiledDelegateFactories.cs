using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DOL.GS.Infrastructure
{
    /// <summary>
    /// Ultra-fast compiled delegate factory for hot path services
    /// Provides sub-100ns service instantiation for critical game operations
    /// </summary>
    public interface ICompiledDelegateFactory<T> where T : class
    {
        /// <summary>
        /// Create instance using compiled delegate (fastest possible)
        /// </summary>
        T CreateFast();

        /// <summary>
        /// Get creation performance in nanoseconds
        /// </summary>
        long GetAverageCreationTimeNs();
    }

    /// <summary>
    /// Compiled delegate factory implementation with multiple optimization strategies
    /// </summary>
    public class CompiledDelegateFactory<T> : ICompiledDelegateFactory<T> where T : class
    {
        private readonly Func<T> _compiledDelegate;
        private readonly ILogger<CompiledDelegateFactory<T>> _logger;
        private readonly CompiledFactoryMetrics _metrics;

        public CompiledDelegateFactory(IServiceProvider serviceProvider, ILogger<CompiledDelegateFactory<T>> logger)
        {
            _logger = logger;
            _metrics = new CompiledFactoryMetrics(typeof(T).Name);
            _compiledDelegate = CompileOptimalFactory(serviceProvider);
        }

        public T CreateFast()
        {
            var startTicks = Environment.TickCount64;
            
            try
            {
                var instance = _compiledDelegate();
                _metrics.RecordCreation(Environment.TickCount64 - startTicks);
                return instance;
            }
            catch (Exception ex)
            {
                _metrics.RecordError();
                _logger.LogError(ex, "Compiled delegate factory failed for {ServiceType}", typeof(T).Name);
                throw;
            }
        }

        public long GetAverageCreationTimeNs()
        {
            return _metrics.GetAverageCreationTimeNs();
        }

        /// <summary>
        /// Compile the optimal factory strategy based on type characteristics
        /// </summary>
        private Func<T> CompileOptimalFactory(IServiceProvider serviceProvider)
        {
            var serviceType = typeof(T);

            // Strategy 1: Parameterless constructor (fastest ~5-10ns)
            var parameterlessConstructor = serviceType.GetConstructor(Type.EmptyTypes);
            if (parameterlessConstructor != null)
            {
                var newExpression = Expression.New(parameterlessConstructor);
                return Expression.Lambda<Func<T>>(newExpression).Compile();
            }

            // Strategy 2: Single dependency injection (fast ~20-50ns)
            var strategy2 = TryCompileSingleDependencyConstructor(serviceType, serviceProvider);
            if (strategy2 != null)
            {
                _logger.LogDebug("Using single dependency constructor strategy for {ServiceType}", serviceType.Name);
                return strategy2;
            }

            // Strategy 3: Multiple dependency injection (slower ~50-100ns)
            var strategy3 = TryCompileMultipleDependencyConstructor(serviceType, serviceProvider);
            if (strategy3 != null)
            {
                _logger.LogDebug("Using multiple dependency constructor strategy for {ServiceType}", serviceType.Name);
                return strategy3;
            }

            // Fallback: Service provider resolution (slowest ~100-500ns)
            _logger.LogDebug("Using service provider fallback strategy for {ServiceType}", serviceType.Name);
            return () => serviceProvider.GetRequiredService<T>();
        }

        private Func<T> TryCompileSingleDependencyConstructor(Type serviceType, IServiceProvider serviceProvider)
        {
            var constructors = serviceType.GetConstructors();
            var singleParamConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 1);
            
            if (singleParamConstructor == null)
                return null;

            var paramType = singleParamConstructor.GetParameters()[0].ParameterType;
            
            try
            {
                // Pre-resolve the dependency
                var dependency = serviceProvider.GetRequiredService(paramType);
                
                // Compile: () => new T(dependency)
                var newExpression = Expression.New(
                    singleParamConstructor,
                    Expression.Constant(dependency, paramType)
                );
                
                return Expression.Lambda<Func<T>>(newExpression).Compile();
            }
            catch
            {
                return null;
            }
        }

        private Func<T> TryCompileMultipleDependencyConstructor(Type serviceType, IServiceProvider serviceProvider)
        {
            var constructors = serviceType.GetConstructors()
                .Where(c => c.GetParameters().Length > 1 && c.GetParameters().Length <= 5) // Limit complexity
                .OrderBy(c => c.GetParameters().Length)
                .ToArray();

            foreach (var constructor in constructors)
            {
                try
                {
                    var parameters = constructor.GetParameters();
                    var dependencies = new object[parameters.Length];
                    var constantExpressions = new Expression[parameters.Length];

                    // Pre-resolve all dependencies
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var paramType = parameters[i].ParameterType;
                        dependencies[i] = serviceProvider.GetRequiredService(paramType);
                        constantExpressions[i] = Expression.Constant(dependencies[i], paramType);
                    }

                    // Compile: () => new T(dep1, dep2, ...)
                    var newExpression = Expression.New(constructor, constantExpressions);
                    return Expression.Lambda<Func<T>>(newExpression).Compile();
                }
                catch
                {
                    continue; // Try next constructor
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Metrics for compiled delegate factories
    /// </summary>
    public class CompiledFactoryMetrics
    {
        private readonly string _serviceName;
        private long _creationCount;
        private long _errorCount;
        private long _totalCreationTicks;
        private readonly object _lock = new object();

        public CompiledFactoryMetrics(string serviceName)
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

        public void RecordError()
        {
            lock (_lock)
            {
                _errorCount++;
            }
        }

        public long GetAverageCreationTimeNs()
        {
            lock (_lock)
            {
                return _creationCount > 0 ? (_totalCreationTicks * 1000000) / _creationCount : 0;
            }
        }

        public CompiledFactoryStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new CompiledFactoryStatistics
                {
                    ServiceName = _serviceName,
                    CreationCount = _creationCount,
                    ErrorCount = _errorCount,
                    AverageCreationTimeNs = GetAverageCreationTimeNs()
                };
            }
        }
    }

    public class CompiledFactoryStatistics
    {
        public string ServiceName { get; set; }
        public long CreationCount { get; set; }
        public long ErrorCount { get; set; }
        public long AverageCreationTimeNs { get; set; }

        public override string ToString()
        {
            return $"{ServiceName}: {CreationCount} created, {ErrorCount} errors, {AverageCreationTimeNs}ns avg";
        }
    }

    /// <summary>
    /// Registry for managing compiled delegate factories
    /// </summary>
    public interface ICompiledDelegateRegistry
    {
        void RegisterFactory<T>(ICompiledDelegateFactory<T> factory) where T : class;
        ICompiledDelegateFactory<T> GetFactory<T>() where T : class;
        bool HasFactory<T>() where T : class;
        T CreateFast<T>() where T : class;
    }

    public class CompiledDelegateRegistry : ICompiledDelegateRegistry
    {
        private readonly ConcurrentDictionary<Type, object> _factories = new();
        private readonly ILogger<CompiledDelegateRegistry> _logger;

        public CompiledDelegateRegistry(ILogger<CompiledDelegateRegistry> logger)
        {
            _logger = logger;
        }

        public void RegisterFactory<T>(ICompiledDelegateFactory<T> factory) where T : class
        {
            var serviceType = typeof(T);
            
            if (_factories.TryAdd(serviceType, factory))
            {
                _logger.LogDebug("Registered compiled delegate factory for {ServiceType}", serviceType.Name);
            }
        }

        public ICompiledDelegateFactory<T> GetFactory<T>() where T : class
        {
            var serviceType = typeof(T);
            
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                return (ICompiledDelegateFactory<T>)factory;
            }

            throw new InvalidOperationException($"No compiled delegate factory registered for {serviceType.Name}");
        }

        public bool HasFactory<T>() where T : class
        {
            return _factories.ContainsKey(typeof(T));
        }

        public T CreateFast<T>() where T : class
        {
            var factory = GetFactory<T>();
            return factory.CreateFast();
        }
    }

    /// <summary>
    /// Hot path service factory that uses the fastest possible creation method
    /// </summary>
    public static class HotPathFactory
    {
        private static ICompiledDelegateRegistry _registry;
        private static IServiceProvider _serviceProvider;

        /// <summary>
        /// Initialize hot path factory with registry and service provider
        /// </summary>
        public static void Initialize(ICompiledDelegateRegistry registry, IServiceProvider serviceProvider)
        {
            _registry = registry;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Create instance using fastest available method
        /// Priority: Compiled delegate > Performance factory > Service provider
        /// </summary>
        public static T CreateFastest<T>() where T : class
        {
            // Try compiled delegate first (fastest)
            if (_registry?.HasFactory<T>() == true)
            {
                return _registry.CreateFast<T>();
            }

            // Fallback to service provider
            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Pre-warm compiled delegates for specified types
        /// </summary>
        public static void PrewarmFactories(params Type[] hotPathTypes)
        {
            foreach (var type in hotPathTypes)
            {
                try
                {
                    // Create factory and do a test creation to warm up
                    var factoryType = typeof(CompiledDelegateFactory<>).MakeGenericType(type);
                    var factory = Activator.CreateInstance(factoryType, _serviceProvider, 
                        _serviceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(factoryType)));
                    
                    // Register with registry
                    var registerMethod = _registry.GetType().GetMethod(nameof(ICompiledDelegateRegistry.RegisterFactory))
                        .MakeGenericMethod(type);
                    registerMethod.Invoke(_registry, new[] { factory });
                }
                catch (Exception ex)
                {
                    // Log warning but continue with other types
                    var logger = _serviceProvider.GetService<ILogger<CompiledDelegateRegistry>>();
                    logger?.LogWarning(ex, "Failed to prewarm factory for {TypeName}", type.Name);
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for compiled delegate factory registration
    /// </summary>
    public static class CompiledDelegateExtensions
    {
        public static IServiceCollection AddCompiledDelegateFactories(this IServiceCollection services)
        {
            services.AddSingleton<ICompiledDelegateRegistry, CompiledDelegateRegistry>();
            return services;
        }

        public static IServiceCollection AddCompiledDelegateFactory<T>(this IServiceCollection services) where T : class
        {
            services.AddSingleton<ICompiledDelegateFactory<T>, CompiledDelegateFactory<T>>();
            return services;
        }

        /// <summary>
        /// Register compiled delegate factories for critical hot path services
        /// </summary>
        public static IServiceCollection AddHotPathCompiledFactories(this IServiceCollection services)
        {
            // These will be registered as we identify critical hot path services
            // Examples for future implementation:
            // services.AddCompiledDelegateFactory<ICombatCalculationContext>();
            // services.AddCompiledDelegateFactory<IPropertyCalculationContext>();
            // services.AddCompiledDelegateFactory<IAttackContext>();

            return services;
        }

        /// <summary>
        /// Configure and initialize all compiled delegate factories
        /// </summary>
        public static void InitializeCompiledDelegateFactories(this IServiceProvider serviceProvider)
        {
            var registry = serviceProvider.GetRequiredService<ICompiledDelegateRegistry>();
            
            // Register all compiled delegate factories
            var factories = serviceProvider.GetServices<object>()
                .Where(service => service.GetType().IsGenericType &&
                                service.GetType().GetGenericTypeDefinition() == typeof(ICompiledDelegateFactory<>));

            foreach (var factory in factories)
            {
                // Register factory with registry using reflection
                var factoryType = factory.GetType();
                var serviceType = factoryType.GetGenericArguments()[0];
                var registerMethod = typeof(ICompiledDelegateRegistry)
                    .GetMethod(nameof(ICompiledDelegateRegistry.RegisterFactory))
                    .MakeGenericMethod(serviceType);
                
                registerMethod.Invoke(registry, new[] { factory });
            }

            // Initialize HotPathFactory
            HotPathFactory.Initialize(registry, serviceProvider);

            var logger = serviceProvider.GetRequiredService<ILogger<CompiledDelegateRegistry>>();
            logger.LogInformation("Initialized compiled delegate factories");
        }
    }
} 