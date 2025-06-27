using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DOL.Database;
using DOL.GS.ServerProperties;

namespace DOL.GS.Infrastructure
{
    /// <summary>
    /// Centralized service registration configuration for GameServer
    /// Organizes service registration by category and priority
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Register all GameServer services in the correct order
        /// </summary>
        public static IServiceCollection AddGameServerServices(this IServiceCollection services)
        {
            // Critical infrastructure services (Priority: Critical)
            services.AddCriticalServices();

            // Core game services (Priority: Core)
            services.AddCoreGameServices();

            // Game mechanics services (Priority: GameMechanics)  
            services.AddGameMechanicsServices();

            // Content services (Priority: Content)
            services.AddContentServices();

            // Optional services (Priority: Optional)
            services.AddOptionalServices();

            return services;
        }

        #region Critical Services (Infrastructure)

        /// <summary>
        /// Register critical infrastructure services that must start first
        /// Database, networking, logging, service management
        /// </summary>
        private static IServiceCollection AddCriticalServices(this IServiceCollection services)
        {
            // Service management infrastructure
            services.AddServiceManager();
            
            // Legacy adapter for gradual migration
            services.AddLegacyGameServerAdapter();

            // Database services
            services.AddDatabaseServices();

            // Logging services
            services.AddLoggingServices();

            // Configuration services
            services.AddConfigurationServices();

            return services;
        }

        private static IServiceCollection AddDatabaseServices(this IServiceCollection services)
        {
            // Register database as a service (wrapping existing singleton)
            services.AddSingleton<IObjectDatabase>(provider => GameServer.Database);

            // Future: Add repository pattern services here
            // services.AddScoped<ICharacterRepository, CharacterRepository>();
            // services.AddScoped<IItemRepository, ItemRepository>();

            return services;
        }

        private static IServiceCollection AddLoggingServices(this IServiceCollection services)
        {
            // Logging is already configured in GameServerHostBuilder
            // Add game-specific loggers here if needed
            
            return services;
        }

        private static IServiceCollection AddConfigurationServices(this IServiceCollection services)
        {
            // Register server configuration
            services.AddSingleton<GameServerConfiguration>(provider => GameServer.Instance.Configuration);

            // Register server properties
            services.AddSingleton<IServerPropertiesProvider, ServerPropertiesProvider>();

            return services;
        }

        #endregion

        #region Core Game Services

        /// <summary>
        /// Register core game services that form the foundation of gameplay
        /// World management, player management, NPC management
        /// </summary>
        private static IServiceCollection AddCoreGameServices(this IServiceCollection services)
        {
            // World management services
            services.AddWorldServices();

            // Player management services  
            services.AddPlayerServices();

            // NPC management services
            services.AddNpcServices();

            // Scheduling services
            services.AddSchedulingServices();

            return services;
        }

        private static IServiceCollection AddWorldServices(this IServiceCollection services)
        {
            // Register world manager as a service (wrapping existing singleton)
            services.AddSingleton<WorldManager>(provider => GameServer.Instance.WorldManager);

            // Future: Add world-related services
            // services.AddSingleton<IWorldService, WorldService>();
            // services.AddSingleton<IRegionService, RegionService>();

            return services;
        }

        private static IServiceCollection AddPlayerServices(this IServiceCollection services)
        {
            // Register player manager as a service (wrapping existing singleton)
            services.AddSingleton<PlayerManager>(provider => GameServer.Instance.PlayerManager);

            // Future: Add player-related services
            // services.AddScoped<IPlayerService, PlayerService>();
            // services.AddScoped<ICharacterProgressionService, CharacterProgressionService>();

            return services;
        }

        private static IServiceCollection AddNpcServices(this IServiceCollection services)
        {
            // Register NPC manager as a service (wrapping existing singleton)
            services.AddSingleton<NpcManager>(provider => GameServer.Instance.NpcManager);

            // Future: Add NPC-related services
            // services.AddSingleton<INpcService, NpcService>();
            // services.AddSingleton<INpcTemplateService, NpcTemplateService>();

            return services;
        }

        private static IServiceCollection AddSchedulingServices(this IServiceCollection services)
        {
            // Register scheduler as a service (wrapping existing singleton)
            services.AddSingleton<Scheduler.SimpleScheduler>(provider => GameServer.Instance.Scheduler);

            // Future: Add scheduling services
            // services.AddSingleton<ITaskSchedulerService, TaskSchedulerService>();

            return services;
        }

        #endregion

        #region Game Mechanics Services

        /// <summary>
        /// Register game mechanics services that handle gameplay logic
        /// Combat, property calculations, effects, inventory
        /// </summary>
        private static IServiceCollection AddGameMechanicsServices(this IServiceCollection services)
        {
            // Combat services
            services.AddCombatServices();

            // Property calculation services
            services.AddPropertyServices();

            // Effect services
            services.AddEffectServices();

            // Item and inventory services
            services.AddItemServices();

            return services;
        }

        private static IServiceCollection AddCombatServices(this IServiceCollection services)
        {
            // Combat services will be implemented in Phase 2
            // services.AddSingleton<ICombatService, CombatService>();
            // services.AddSingleton<IDamageCalculator, DamageCalculator>();
            // services.AddSingleton<IAttackResolver, AttackResolver>();

            return services;
        }

        private static IServiceCollection AddPropertyServices(this IServiceCollection services)
        {
            // Property services will be implemented in Phase 2
            // services.AddSingleton<IPropertyService, PropertyService>();
            // services.AddSingleton<IPropertyCalculatorRegistry, PropertyCalculatorRegistry>();

            return services;
        }

        private static IServiceCollection AddEffectServices(this IServiceCollection services)
        {
            // Effect services will be implemented in Phase 2
            // services.AddSingleton<IEffectService, EffectService>();
            // services.AddSingleton<IEffectFactory, EffectFactory>();

            return services;
        }

        private static IServiceCollection AddItemServices(this IServiceCollection services)
        {
            // Item services will be implemented in Phase 2
            // services.AddSingleton<IItemService, ItemService>();
            // services.AddSingleton<IInventoryService, InventoryService>();

            return services;
        }

        #endregion

        #region Content Services

        /// <summary>
        /// Register content services that handle game content
        /// Quests, housing, guilds, keeps
        /// </summary>
        private static IServiceCollection AddContentServices(this IServiceCollection services)
        {
            // Quest services
            services.AddQuestServices();

            // Housing services
            services.AddHousingServices();

            // Guild services
            services.AddGuildServices();

            // Keep services
            services.AddKeepServices();

            return services;
        }

        private static IServiceCollection AddQuestServices(this IServiceCollection services)
        {
            // Quest services will be implemented later
            // services.AddSingleton<IQuestService, QuestService>();

            return services;
        }

        private static IServiceCollection AddHousingServices(this IServiceCollection services)
        {
            // Housing services will be implemented later
            // services.AddSingleton<IHousingService, HousingService>();

            return services;
        }

        private static IServiceCollection AddGuildServices(this IServiceCollection services)
        {
            // Guild services will be implemented later
            // services.AddSingleton<IGuildService, GuildService>();

            return services;
        }

        private static IServiceCollection AddKeepServices(this IServiceCollection services)
        {
            // Keep services will be implemented later
            // services.AddSingleton<IKeepService, KeepService>();

            return services;
        }

        #endregion

        #region Optional Services

        /// <summary>
        /// Register optional services that enhance functionality
        /// Metrics, debugging, monitoring
        /// </summary>
        private static IServiceCollection AddOptionalServices(this IServiceCollection services)
        {
            // Metrics services
            services.AddMetricsServices();

            // Debug services
            services.AddDebugServices();

            // Performance monitoring
            services.AddPerformanceMonitoringServices();

            return services;
        }

        private static IServiceCollection AddMetricsServices(this IServiceCollection services)
        {
            // Metrics services
            // services.AddSingleton<IMetricsService, MetricsService>();

            return services;
        }

        private static IServiceCollection AddDebugServices(this IServiceCollection services)
        {
            // Only add debug services in debug builds
            #if DEBUG
            // services.AddSingleton<IDebugService, DebugService>();
            #endif

            return services;
        }

        private static IServiceCollection AddPerformanceMonitoringServices(this IServiceCollection services)
        {
            // Performance monitoring services
            // services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();

            return services;
        }

        #endregion

        #region Service Discovery and Validation

        /// <summary>
        /// Auto-register services from assemblies using attributes
        /// </summary>
        public static IServiceCollection AddServicesFromAssembly(
            this IServiceCollection services, 
            Assembly assembly)
        {
            var serviceTypes = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => type.GetCustomAttributes<ServiceRegistrationAttribute>().Any());

            foreach (var serviceType in serviceTypes)
            {
                var attribute = serviceType.GetCustomAttribute<ServiceRegistrationAttribute>();
                var interfaceType = attribute.InterfaceType ?? serviceType;
                
                var serviceDescriptor = new ServiceDescriptor(
                    interfaceType, 
                    serviceType, 
                    attribute.Lifetime);
                    
                services.Add(serviceDescriptor);
            }

            return services;
        }

        /// <summary>
        /// Validate service registration configuration
        /// </summary>
        public static void ValidateServiceRegistration(this IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetService<ILogger<ServiceRegistrationOptions>>();

            try
            {
                // Validate critical services
                ValidateCriticalServices(serviceProvider, logger);

                // Validate core services  
                ValidateCoreServices(serviceProvider, logger);

                logger?.LogInformation("Service registration validation completed successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Service registration validation failed");
                throw;
            }
        }

        private static void ValidateCriticalServices(IServiceProvider serviceProvider, ILogger logger)
        {
            // Validate service manager
            var serviceManager = serviceProvider.GetService<IServiceManager>();
            if (serviceManager == null)
            {
                throw new InvalidOperationException("IServiceManager is not registered");
            }

            // Validate legacy adapter
            var legacyAdapter = serviceProvider.GetService<ILegacyGameServer>();
            if (legacyAdapter == null)
            {
                throw new InvalidOperationException("ILegacyGameServer is not registered");
            }

            logger?.LogDebug("Critical services validation passed");
        }

        private static void ValidateCoreServices(IServiceProvider serviceProvider, ILogger logger)
        {
            // Validate core managers are accessible
            var worldManager = serviceProvider.GetService<WorldManager>();
            var playerManager = serviceProvider.GetService<PlayerManager>();
            var npcManager = serviceProvider.GetService<NpcManager>();

            logger?.LogDebug("Core services validation passed");
        }

        #endregion
    }

    /// <summary>
    /// Attribute for marking services for automatic registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceRegistrationAttribute : Attribute
    {
        public Type InterfaceType { get; }
        public ServiceLifetime Lifetime { get; }

        public ServiceRegistrationAttribute(
            Type interfaceType = null, 
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            InterfaceType = interfaceType;
            Lifetime = lifetime;
        }
    }

    /// <summary>
    /// Configuration options for service registration
    /// </summary>
    public class ServiceRegistrationOptions
    {
        /// <summary>
        /// Enable service validation during startup
        /// </summary>
        public bool EnableServiceValidation { get; set; } = true;

        /// <summary>
        /// Enable automatic service discovery from assemblies
        /// </summary>
        public bool EnableAutoDiscovery { get; set; } = false;

        /// <summary>
        /// Assemblies to scan for services
        /// </summary>
        public Assembly[] AssembliesToScan { get; set; } = Array.Empty<Assembly>();

        /// <summary>
        /// Enable performance optimizations for service resolution
        /// </summary>
        public bool EnablePerformanceOptimizations { get; set; } = true;

        /// <summary>
        /// Enable development-time service debugging
        /// </summary>
        public bool EnableDevelopmentDebugging { get; set; } = false;
    }

    /// <summary>
    /// Server properties provider interface
    /// </summary>
    public interface IServerPropertiesProvider
    {
        T GetProperty<T>(string propertyName);
        bool TryGetProperty<T>(string propertyName, out T value);
        void SetProperty<T>(string propertyName, T value);
    }

    /// <summary>
    /// Server properties provider implementation
    /// </summary>
    public class ServerPropertiesProvider : IServerPropertiesProvider
    {
        public T GetProperty<T>(string propertyName)
        {
            // Use reflection to access the static Properties fields
            var field = typeof(Properties).GetField(propertyName?.ToUpper(), 
                BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            
            if (field != null)
            {
                var value = field.GetValue(null);
                if (value is T typedValue)
                    return typedValue;
                    
                // Attempt conversion
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default(T);
                }
            }
            
            return default(T);
        }

        public bool TryGetProperty<T>(string propertyName, out T value)
        {
            try
            {
                value = GetProperty<T>(propertyName);
                return true;
            }
            catch
            {
                value = default(T);
                return false;
            }
        }

        public void SetProperty<T>(string propertyName, T value)
        {
            // This would need to be implemented based on the Properties system
            // Properties.SetProperty(propertyName, value);
            throw new NotImplementedException("SetProperty not yet implemented");
        }
    }
} 