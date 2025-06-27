using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DOL.GS.Infrastructure
{
    /// <summary>
    /// Manages the dependency injection container and service lifecycle for GameServer
    /// Implements clean architecture principles with proper service registration and orchestration
    /// </summary>
    public class GameServerHost : IHost
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceLifetimeManager _lifetimeManager;
        private readonly ILogger<GameServerHost> _logger;
        private readonly CancellationTokenSource _shutdownCts;

        public IServiceProvider Services => _serviceProvider;

        public GameServerHost(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _shutdownCts = new CancellationTokenSource();
            
            // Resolve required services with proper error handling
            try
            {
                _lifetimeManager = serviceProvider.GetRequiredService<IServiceLifetimeManager>();
                _logger = serviceProvider.GetRequiredService<ILogger<GameServerHost>>();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    "Required services not registered. Ensure IServiceLifetimeManager and ILogger<GameServerHost> are registered.", ex);
            }
            
            // Auto-register all services with the lifetime manager
            RegisterServicesWithLifetimeManager();
        }

        /// <summary>
        /// Starts the GameServer host and all registered services using orchestrated startup
        /// Services are started in priority order for proper dependency management
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting GameServer Host with service orchestration...");
                
                // Use the service lifetime manager for orchestrated startup
                await _lifetimeManager.StartServicesAsync(cancellationToken);
                
                // Log startup report for monitoring
                var report = _lifetimeManager.GetReport();
                _logger.LogInformation("GameServer Host started successfully with {ServiceCount} services", report.TotalServices);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to start GameServer Host");
                throw;
            }
        }

        /// <summary>
        /// Stops the GameServer host and all registered services using orchestrated shutdown
        /// Services are stopped in reverse priority order for clean dependency teardown
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Stopping GameServer Host with service orchestration...");
                
                _shutdownCts.Cancel();

                // Use the service lifetime manager for orchestrated shutdown
                await _lifetimeManager.StopServicesAsync(cancellationToken);

                _logger.LogInformation("GameServer Host stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GameServer Host shutdown");
            }
        }

        /// <summary>
        /// Auto-registers all services that implement IServiceLifecycle with the lifetime manager
        /// </summary>
        private void RegisterServicesWithLifetimeManager()
        {
            try
            {
                var services = _serviceProvider.GetServices<IServiceLifecycle>();
                
                foreach (var service in services)
                {
                    _lifetimeManager.RegisterService(service);
                    _logger.LogDebug("Registered service {ServiceName} with lifetime manager", service.ServiceName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register services with lifetime manager");
                throw;
            }
        }

        public void Dispose()
        {
            _shutdownCts?.Cancel();
            _shutdownCts?.Dispose();
            
            if (_serviceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }
    }

    /// <summary>
    /// Builder for configuring and creating GameServerHost instances
    /// </summary>
    public class GameServerHostBuilder : IDisposable
    {
        private readonly ServiceCollection _services;
        private readonly ILoggerFactory _loggerFactory;
        private bool _disposed;

        public GameServerHostBuilder()
        {
            _services = new ServiceCollection();
            
            // Configure logging first
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole()
                       .AddDebug()
                       .SetMinimumLevel(LogLevel.Information);
            });

            // Register core services
            _services.AddSingleton(_loggerFactory);
            _services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            _services.AddLogging();
        }

        /// <summary>
        /// Configure services for the GameServer
        /// </summary>
        public GameServerHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            configureServices?.Invoke(_services);
            return this;
        }

        /// <summary>
        /// Register GameServer core services including service lifetime management
        /// </summary>
        public GameServerHostBuilder AddGameServerServices()
        {
            // Register service lifetime management system
            _services.AddServiceLifetimeManagement();
            
            // Core game services will be registered here
            // This will be expanded in subsequent tasks
            
            return this;
        }

        /// <summary>
        /// Register performance-optimized services for hot paths
        /// </summary>
        public GameServerHostBuilder AddPerformanceServices()
        {
            // Performance-critical services will be registered here
            // Will be implemented in later tasks (FDIS-008, FDIS-009)
            
            return this;
        }

        /// <summary>
        /// Build the GameServerHost
        /// </summary>
        public GameServerHost Build()
        {
            // Validate required services are registered
            ValidateServices();

            var serviceProvider = _services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = false, // Disable for performance in production
                ValidateOnBuild = true
            });

            return new GameServerHost(serviceProvider);
        }

        private void ValidateServices()
        {
            // Validate that required services are registered
            var requiredServices = new[]
            {
                typeof(IServiceLifetimeManager),
                typeof(ILogger<GameServerHost>)
            };

            foreach (var serviceType in requiredServices)
            {
                var descriptor = _services.FirstOrDefault(s => s.ServiceType == serviceType);
                if (descriptor == null)
                {
                    throw new InvalidOperationException(
                        $"Required service {serviceType.Name} is not registered. " +
                        "Call AddGameServerServices() to register required services.");
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _loggerFactory?.Dispose();
                _disposed = true;
            }
        }
    }
} 