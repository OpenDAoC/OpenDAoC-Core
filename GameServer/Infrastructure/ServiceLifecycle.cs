using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DOL.GS.Infrastructure
{
    /// <summary>
    /// Core interface for service lifecycle management
    /// All services that need startup/shutdown coordination should implement this
    /// </summary>
    public interface IServiceLifecycle
    {
        /// <summary>
        /// Gets the service name for logging and identification
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Gets the service status
        /// </summary>
        ServiceStatus Status { get; }

        /// <summary>
        /// Gets the service priority for startup/shutdown ordering
        /// </summary>
        ServicePriority Priority { get; }

        /// <summary>
        /// Asynchronous service start with cancellation support
        /// </summary>
        Task OnServiceStartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronous service stop with cancellation support
        /// </summary>
        Task OnServiceStopAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for services that need synchronous lifecycle methods (legacy compatibility)
    /// </summary>
    public interface ISynchronousServiceLifecycle : IServiceLifecycle
    {
        /// <summary>
        /// Synchronous service start (for legacy compatibility)
        /// </summary>
        void OnServiceStart();

        /// <summary>
        /// Synchronous service stop (for legacy compatibility)
        /// </summary>
        void OnServiceStop();
    }

    /// <summary>
    /// Extended interface for services that can be enabled/disabled
    /// </summary>
    public interface IToggleableService : IServiceLifecycle
    {
        /// <summary>
        /// Gets whether the service is enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Enable the service
        /// </summary>
        Task EnableAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disable the service
        /// </summary>
        Task DisableAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for services that need periodic updates (game loop services)
    /// </summary>
    public interface ITickableService : IServiceLifecycle
    {
        /// <summary>
        /// Gets the tick interval in milliseconds
        /// </summary>
        int TickInterval { get; }

        /// <summary>
        /// Execute a service tick
        /// </summary>
        void Tick();

        /// <summary>
        /// Execute an asynchronous service tick
        /// </summary>
        Task TickAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for services that require ordered initialization
    /// </summary>
    public interface IOrderedService : IServiceLifecycle
    {
        /// <summary>
        /// Gets the initialization order (lower numbers start first)
        /// </summary>
        int InitializationOrder { get; }
    }

    /// <summary>
    /// Service status enumeration
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>
        /// Service has not been started
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// Service is starting up
        /// </summary>
        Starting = 1,

        /// <summary>
        /// Service is running normally
        /// </summary>
        Running = 2,

        /// <summary>
        /// Service is stopping
        /// </summary>
        Stopping = 3,

        /// <summary>
        /// Service has stopped
        /// </summary>
        Stopped = 4,

        /// <summary>
        /// Service encountered an error
        /// </summary>
        Error = 5,

        /// <summary>
        /// Service is disabled
        /// </summary>
        Disabled = 6
    }

    /// <summary>
    /// Service priority for startup/shutdown ordering
    /// Lower priority services start first and stop last
    /// </summary>
    public enum ServicePriority
    {
        /// <summary>
        /// Critical infrastructure services (database, networking)
        /// </summary>
        Critical = 0,

        /// <summary>
        /// Core game systems (world manager, player manager)
        /// </summary>
        Core = 10,

        /// <summary>
        /// Game mechanics services (combat, property calculations)
        /// </summary>
        GameMechanics = 20,

        /// <summary>
        /// Content services (NPCs, quests, housing)
        /// </summary>
        Content = 30,

        /// <summary>
        /// Optional features (metrics, debugging tools)
        /// </summary>
        Optional = 40
    }

    /// <summary>
    /// Base implementation of IServiceLifecycle providing common functionality
    /// Thread-safe implementation with proper status management
    /// </summary>
    public abstract class ServiceLifecycleBase : IServiceLifecycle, ISynchronousServiceLifecycle
    {
        protected readonly ILogger _logger;
        private volatile ServiceStatus _status = ServiceStatus.NotStarted;
        private readonly object _statusLock = new object();

        public abstract string ServiceName { get; }
        public virtual ServicePriority Priority => ServicePriority.Core;
        public ServiceStatus Status => _status;

        protected ServiceLifecycleBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Validate service name
            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                throw new InvalidOperationException("ServiceName cannot be null or empty");
            }
        }

        public virtual async Task OnServiceStartAsync(CancellationToken cancellationToken = default)
        {
            lock (_statusLock)
            {
                if (_status != ServiceStatus.NotStarted && _status != ServiceStatus.Stopped)
                {
                    _logger.LogWarning("Attempting to start service {ServiceName} in state {Status}", ServiceName, _status);
                    return;
                }
                _status = ServiceStatus.Starting;
            }

            try
            {
                _logger.LogInformation("Starting service: {ServiceName}", ServiceName);

                await StartAsyncCore(cancellationToken);
                
                // Call synchronous version if implemented
                if (this is ISynchronousServiceLifecycle syncService)
                {
                    syncService.OnServiceStart();
                }

                lock (_statusLock)
                {
                    _status = ServiceStatus.Running;
                }
                
                _logger.LogInformation("Service started successfully: {ServiceName}", ServiceName);
            }
            catch (Exception ex)
            {
                lock (_statusLock)
                {
                    _status = ServiceStatus.Error;
                }
                _logger.LogError(ex, "Failed to start service: {ServiceName}", ServiceName);
                throw;
            }
        }

        public virtual async Task OnServiceStopAsync(CancellationToken cancellationToken = default)
        {
            lock (_statusLock)
            {
                if (_status != ServiceStatus.Running)
                {
                    _logger.LogWarning("Attempting to stop service {ServiceName} in state {Status}", ServiceName, _status);
                    return;
                }
                _status = ServiceStatus.Stopping;
            }

            try
            {
                _logger.LogInformation("Stopping service: {ServiceName}", ServiceName);

                await StopAsyncCore(cancellationToken);
                
                // Call synchronous version if implemented
                if (this is ISynchronousServiceLifecycle syncService)
                {
                    syncService.OnServiceStop();
                }

                lock (_statusLock)
                {
                    _status = ServiceStatus.Stopped;
                }
                
                _logger.LogInformation("Service stopped successfully: {ServiceName}", ServiceName);
            }
            catch (Exception ex)
            {
                lock (_statusLock)
                {
                    _status = ServiceStatus.Error;
                }
                _logger.LogError(ex, "Error stopping service: {ServiceName}", ServiceName);
                throw;
            }
        }

        public virtual void OnServiceStart()
        {
            // Default implementation calls StartCore
            // Override in derived classes for custom synchronous startup logic
            StartCore();
        }

        public virtual void OnServiceStop()
        {
            // Default implementation calls StopCore
            // Override in derived classes for custom synchronous shutdown logic
            StopCore();
        }

        /// <summary>
        /// Override this method for asynchronous startup logic
        /// </summary>
        protected virtual Task StartAsyncCore(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this method for asynchronous shutdown logic
        /// </summary>
        protected virtual Task StopAsyncCore(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this method for synchronous startup logic
        /// </summary>
        protected virtual void StartCore()
        {
            // Default empty implementation
        }

        /// <summary>
        /// Override this method for synchronous shutdown logic
        /// </summary>
        protected virtual void StopCore()
        {
            // Default empty implementation
        }

        protected void SetStatus(ServiceStatus status)
        {
            lock (_statusLock)
            {
                _status = status;
            }
        }
    }

    /// <summary>
    /// Base implementation for toggleable services
    /// </summary>
    public abstract class ToggleableServiceBase : ServiceLifecycleBase, IToggleableService
    {
        private volatile bool _isEnabled = true;
        private readonly object _enabledLock = new object();

        public bool IsEnabled => _isEnabled;

        protected ToggleableServiceBase(ILogger logger) : base(logger)
        {
        }

        public virtual async Task EnableAsync(CancellationToken cancellationToken = default)
        {
            lock (_enabledLock)
            {
                if (_isEnabled)
                    return;
                
                _isEnabled = true;
            }

            _logger.LogInformation("Enabling service: {ServiceName}", ServiceName);

            if (Status == ServiceStatus.Stopped || Status == ServiceStatus.Disabled)
            {
                await OnServiceStartAsync(cancellationToken);
            }
        }

        public virtual async Task DisableAsync(CancellationToken cancellationToken = default)
        {
            lock (_enabledLock)
            {
                if (!_isEnabled)
                    return;
                
                _isEnabled = false;
            }

            _logger.LogInformation("Disabling service: {ServiceName}", ServiceName);

            if (Status == ServiceStatus.Running)
            {
                await OnServiceStopAsync(cancellationToken);
                SetStatus(ServiceStatus.Disabled);
            }
        }
    }

    /// <summary>
    /// Base implementation for tickable services
    /// </summary>
    public abstract class TickableServiceBase : ServiceLifecycleBase, ITickableService
    {
        public abstract int TickInterval { get; }

        protected TickableServiceBase(ILogger logger) : base(logger)
        {
        }

        public abstract void Tick();

        public virtual Task TickAsync(CancellationToken cancellationToken = default)
        {
            // Default implementation calls synchronous Tick
            Tick();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Interface for the service manager that coordinates all services
    /// </summary>
    public interface IServiceManager
    {
        /// <summary>
        /// Register a service with the manager
        /// </summary>
        void RegisterService(IServiceLifecycle service);

        /// <summary>
        /// Start all registered services in priority order
        /// </summary>
        Task StartAllServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop all registered services in reverse priority order
        /// </summary>
        Task StopAllServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a service by type
        /// </summary>
        T GetService<T>() where T : class, IServiceLifecycle;

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        bool IsServiceRegistered<T>() where T : class, IServiceLifecycle;
    }
} 