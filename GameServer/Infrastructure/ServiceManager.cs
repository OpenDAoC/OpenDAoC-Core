using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace DOL.GS.Infrastructure
{
    /// <summary>
    /// Manages service lifecycle with priority-based ordering for startup and shutdown
    /// Implements clean architecture service coordination
    /// </summary>
    public class ServiceManager : IServiceManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceManager> _logger;
        private readonly ConcurrentDictionary<Type, IServiceLifecycle> _services;
        private readonly object _lockObject = new object();
        private bool _isStarting = false;
        private bool _isStopping = false;

        public ServiceManager(IServiceProvider serviceProvider, ILogger<ServiceManager> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _services = new ConcurrentDictionary<Type, IServiceLifecycle>();
        }

        /// <summary>
        /// Register a service with the manager
        /// </summary>
        public void RegisterService(IServiceLifecycle service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var serviceType = service.GetType();
            
            if (_services.TryAdd(serviceType, service))
            {
                _logger.LogDebug("Registered service: {ServiceName} (Type: {ServiceType}, Priority: {Priority})", 
                    service.ServiceName, serviceType.Name, service.Priority);
            }
            else
            {
                _logger.LogWarning("Service {ServiceName} is already registered", service.ServiceName);
            }
        }

        /// <summary>
        /// Start all registered services in priority order (lower priority numbers start first)
        /// </summary>
        public async Task StartAllServicesAsync(CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                if (_isStarting)
                {
                    _logger.LogWarning("Service startup already in progress");
                    return;
                }
                _isStarting = true;
            }

            try
            {
                _logger.LogInformation("Starting all services in priority order...");

                // Get services ordered by priority (Critical first, Optional last)
                var orderedServices = GetServicesOrderedByPriority();

                foreach (var priorityGroup in orderedServices)
                {
                    var priority = priorityGroup.Key;
                    var services = priorityGroup.Value;

                    _logger.LogInformation("Starting {Count} services with priority {Priority}", 
                        services.Count, priority);

                    // Start services in parallel within the same priority group
                    var startupTasks = services.Select(async service =>
                    {
                        try
                        {
                            await service.OnServiceStartAsync(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to start service: {ServiceName}", service.ServiceName);
                            throw; // Re-throw to fail fast on critical services
                        }
                    });

                    await Task.WhenAll(startupTasks);
                    
                    _logger.LogInformation("Completed starting services with priority {Priority}", priority);
                }

                _logger.LogInformation("All services started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error during service startup");
                throw;
            }
            finally
            {
                lock (_lockObject)
                {
                    _isStarting = false;
                }
            }
        }

        /// <summary>
        /// Stop all registered services in reverse priority order (Optional stops first, Critical last)
        /// </summary>
        public async Task StopAllServicesAsync(CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                if (_isStopping)
                {
                    _logger.LogWarning("Service shutdown already in progress");
                    return;
                }
                _isStopping = true;
            }

            try
            {
                _logger.LogInformation("Stopping all services in reverse priority order...");

                // Get services ordered by priority in reverse (Optional first, Critical last)
                var orderedServices = GetServicesOrderedByPriority(reverse: true);

                foreach (var priorityGroup in orderedServices)
                {
                    var priority = priorityGroup.Key;
                    var services = priorityGroup.Value;

                    _logger.LogInformation("Stopping {Count} services with priority {Priority}", 
                        services.Count, priority);

                    // Stop services in parallel within the same priority group
                    var shutdownTasks = services.Select(async service =>
                    {
                        try
                        {
                            await service.OnServiceStopAsync(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error stopping service: {ServiceName}", service.ServiceName);
                            // Continue with other services even if one fails during shutdown
                        }
                    });

                    await Task.WhenAll(shutdownTasks);
                    
                    _logger.LogInformation("Completed stopping services with priority {Priority}", priority);
                }

                _logger.LogInformation("All services stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during service shutdown");
                // Don't re-throw during shutdown
            }
            finally
            {
                lock (_lockObject)
                {
                    _isStopping = false;
                }
            }
        }

        /// <summary>
        /// Get a service by type
        /// </summary>
        public T GetService<T>() where T : class, IServiceLifecycle
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return service as T;
            }

            // Try to resolve from DI container as fallback
            try
            {
                return _serviceProvider.GetService<T>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve service of type {ServiceType}", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool IsServiceRegistered<T>() where T : class, IServiceLifecycle
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Get all services of a specific priority level
        /// </summary>
        public IEnumerable<IServiceLifecycle> GetServicesByPriority(ServicePriority priority)
        {
            return _services.Values.Where(s => s.Priority == priority);
        }

        /// <summary>
        /// Get all registered services
        /// </summary>
        public IEnumerable<IServiceLifecycle> GetAllServices()
        {
            return _services.Values;
        }

        /// <summary>
        /// Get service statistics
        /// </summary>
        public ServiceManagerStatistics GetStatistics()
        {
            var servicesByStatus = _services.Values
                .GroupBy(s => s.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var servicesByPriority = _services.Values
                .GroupBy(s => s.Priority)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ServiceManagerStatistics
            {
                TotalServices = _services.Count,
                ServicesByStatus = servicesByStatus,
                ServicesByPriority = servicesByPriority,
                IsStarting = _isStarting,
                IsStopping = _isStopping
            };
        }

        /// <summary>
        /// Auto-register all services from the DI container that implement IServiceLifecycle
        /// </summary>
        public void AutoRegisterServices()
        {
            try
            {
                var services = _serviceProvider.GetServices<IServiceLifecycle>();
                
                foreach (var service in services)
                {
                    RegisterService(service);
                }

                _logger.LogInformation("Auto-registered {Count} services", _services.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-registration of services");
                throw;
            }
        }

        private Dictionary<ServicePriority, List<IServiceLifecycle>> GetServicesOrderedByPriority(bool reverse = false)
        {
            var serviceGroups = _services.Values
                .GroupBy(s => s.Priority)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Create ordered dictionary based on priority
            var orderedGroups = new Dictionary<ServicePriority, List<IServiceLifecycle>>();

            var priorities = reverse 
                ? Enum.GetValues<ServicePriority>().OrderByDescending(p => (int)p)
                : Enum.GetValues<ServicePriority>().OrderBy(p => (int)p);

            foreach (var priority in priorities)
            {
                if (serviceGroups.TryGetValue(priority, out var services))
                {
                    // Within the same priority, order by IOrderedService.InitializationOrder if implemented
                    var orderedServices = services.OrderBy(s => s is IOrderedService ordered ? ordered.InitializationOrder : 0).ToList();
                    orderedGroups[priority] = orderedServices;
                }
            }

            return orderedGroups;
        }
    }

    /// <summary>
    /// Statistics about the service manager state
    /// </summary>
    public class ServiceManagerStatistics
    {
        public int TotalServices { get; set; }
        public Dictionary<ServiceStatus, int> ServicesByStatus { get; set; } = new();
        public Dictionary<ServicePriority, int> ServicesByPriority { get; set; } = new();
        public bool IsStarting { get; set; }
        public bool IsStopping { get; set; }

        public override string ToString()
        {
            var statusSummary = string.Join(", ", ServicesByStatus.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            var prioritySummary = string.Join(", ", ServicesByPriority.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            
            return $"Services: {TotalServices} | Status: [{statusSummary}] | Priority: [{prioritySummary}] | Starting: {IsStarting} | Stopping: {IsStopping}";
        }
    }

    /// <summary>
    /// Extension methods for service registration
    /// </summary>
    public static class ServiceManagerExtensions
    {
        /// <summary>
        /// Add the service manager to the DI container
        /// </summary>
        public static IServiceCollection AddServiceManager(this IServiceCollection services)
        {
            services.AddSingleton<IServiceManager, ServiceManager>();
            return services;
        }

        /// <summary>
        /// Register a service lifecycle implementation
        /// </summary>
        public static IServiceCollection AddServiceLifecycle<TInterface, TImplementation>(
            this IServiceCollection services, 
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class, IServiceLifecycle
            where TImplementation : class, TInterface
        {
            services.Add(new ServiceDescriptor(typeof(TInterface), typeof(TImplementation), lifetime));
            services.Add(new ServiceDescriptor(typeof(IServiceLifecycle), 
                provider => provider.GetRequiredService<TInterface>(), lifetime));
            
            return services;
        }

        /// <summary>
        /// Register a service lifecycle implementation with a factory
        /// </summary>
        public static IServiceCollection AddServiceLifecycle<TInterface>(
            this IServiceCollection services,
            Func<IServiceProvider, TInterface> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class, IServiceLifecycle
        {
            services.Add(new ServiceDescriptor(typeof(TInterface), factory, lifetime));
            services.Add(new ServiceDescriptor(typeof(IServiceLifecycle), 
                provider => provider.GetRequiredService<TInterface>(), lifetime));
                
            return services;
        }
    }
} 