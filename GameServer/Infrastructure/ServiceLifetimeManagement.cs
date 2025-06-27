using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DOL.GS.Infrastructure
{
    /// <summary>
    /// Service lifetime management system for coordinating service lifecycle
    /// </summary>
    public interface IServiceLifetimeManager
    {
        void RegisterService(IServiceLifecycle service);
        Task StartServicesAsync(CancellationToken cancellationToken = default);
        Task StopServicesAsync(CancellationToken cancellationToken = default);
        IEnumerable<IServiceLifecycle> GetStartupOrder();
        ServiceLifetimeReport GetReport();
    }

    /// <summary>
    /// Service lifetime manager implementation
    /// </summary>
    public class ServiceLifetimeManager : IServiceLifetimeManager
    {
        private readonly List<IServiceLifecycle> _services = new();
        private readonly ILogger<ServiceLifetimeManager> _logger;
        private readonly object _lock = new object();
        private bool _isStarting = false;
        private bool _isStopping = false;

        public ServiceLifetimeManager(ILogger<ServiceLifetimeManager> logger)
        {
            _logger = logger;
        }

        public void RegisterService(IServiceLifecycle service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            lock (_lock)
            {
                if (!_services.Contains(service))
                {
                    _services.Add(service);
                    _logger.LogDebug("Registered service {ServiceName}", service.ServiceName);
                }
            }
        }

        public async Task StartServicesAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_isStarting) return;
                _isStarting = true;
            }

            try
            {
                var startupOrder = GetStartupOrder().ToList();
                _logger.LogInformation("Starting {Count} services", startupOrder.Count);

                foreach (var service in startupOrder)
                {
                    await service.OnServiceStartAsync();
                    if (cancellationToken.IsCancellationRequested) break;
                }
            }
            finally
            {
                lock (_lock) { _isStarting = false; }
            }
        }

        public async Task StopServicesAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_isStopping) return;
                _isStopping = true;
            }

            try
            {
                var shutdownOrder = GetStartupOrder().Reverse().ToList();
                foreach (var service in shutdownOrder)
                {
                    try
                    {
                        await service.OnServiceStopAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error stopping {ServiceName}", service.ServiceName);
                    }
                }
            }
            finally
            {
                lock (_lock) { _isStopping = false; }
            }
        }

        public IEnumerable<IServiceLifecycle> GetStartupOrder()
        {
            lock (_lock)
            {
                return _services
                    .OrderBy(s => (int)s.Priority)
                    .ThenBy(s => s.ServiceName)
                    .ToList();
            }
        }

        public ServiceLifetimeReport GetReport()
        {
            lock (_lock)
            {
                return new ServiceLifetimeReport
                {
                    TotalServices = _services.Count,
                    IsStarting = _isStarting,
                    IsStopping = _isStopping
                };
            }
        }
    }

    /// <summary>
    /// Report of service lifetime status
    /// </summary>
    public class ServiceLifetimeReport
    {
        public int TotalServices { get; set; }
        public bool IsStarting { get; set; }
        public bool IsStopping { get; set; }
    }

    /// <summary>
    /// Hosted service wrapper for service lifetime manager
    /// Integrates with .NET hosting infrastructure
    /// </summary>
    public class ServiceLifetimeHostedService : IHostedService
    {
        private readonly IServiceLifetimeManager _lifetimeManager;
        private readonly ILogger<ServiceLifetimeHostedService> _logger;

        public ServiceLifetimeHostedService(
            IServiceLifetimeManager lifetimeManager,
            ILogger<ServiceLifetimeHostedService> logger)
        {
            _lifetimeManager = lifetimeManager;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service lifetime management");
            await _lifetimeManager.StartServicesAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping service lifetime management");
            await _lifetimeManager.StopServicesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Extension methods for service lifetime management
    /// </summary>
    public static class ServiceLifetimeExtensions
    {
        /// <summary>
        /// Add service lifetime management to DI container
        /// </summary>
        public static IServiceCollection AddServiceLifetimeManagement(this IServiceCollection services)
        {
            services.AddSingleton<IServiceLifetimeManager, ServiceLifetimeManager>();
            services.AddHostedService<ServiceLifetimeHostedService>();
            return services;
        }

        /// <summary>
        /// Auto-register all services with the lifetime manager
        /// </summary>
        public static void RegisterAllServicesWithLifetimeManager(this IServiceProvider serviceProvider)
        {
            var lifetimeManager = serviceProvider.GetRequiredService<IServiceLifetimeManager>();
            var services = serviceProvider.GetServices<IServiceLifecycle>();
            
            foreach (var service in services)
            {
                lifetimeManager.RegisterService(service);
            }

            var logger = serviceProvider.GetRequiredService<ILogger<ServiceLifetimeManager>>();
            logger.LogInformation("Registered {Count} services with lifetime manager", services.Count());
        }
    }
} 