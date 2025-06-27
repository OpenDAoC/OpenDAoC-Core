using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DOL.Database;
using DOL.GS.Appeal;
using DOL.GS.Housing;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.GS.ServerRules;
using DOL.Language;
using DOL.Logging;
using DOL.GS.Scheduler;

namespace DOL.GS.Infrastructure
{
    /// <summary>
    /// Concrete implementation of the legacy GameServer adapter
    /// Wraps existing GameServer.Instance static calls for dependency injection migration
    /// </summary>
    public class LegacyGameServerAdapter : ILegacyServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LegacyGameServerAdapter> _logger;
        private readonly IStaticUsageTracker _usageTracker;
        private readonly LegacyAdapterConfiguration _configuration;

        public LegacyGameServerAdapter(
            IServiceProvider serviceProvider,
            ILogger<LegacyGameServerAdapter> logger,
            IStaticUsageTracker usageTracker = null,
            LegacyAdapterConfiguration configuration = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _usageTracker = usageTracker;
            _configuration = configuration ?? new LegacyAdapterConfiguration();
        }

        #region Core Properties

        public IObjectDatabase Database
        {
            get
            {
                TrackUsage("GameServer", "Database");
                return GameServer.Database;
            }
        }

        public GameServerConfiguration Configuration
        {
            get
            {
                TrackUsage("GameServer", "Configuration");
                return GameServer.Instance.Configuration;
            }
        }

        public EGameServerStatus ServerStatus
        {
            get
            {
                TrackUsage("GameServer", "ServerStatus");
                return GameServer.Instance.ServerStatus;
            }
        }

        public DateTime StartupTime
        {
            get
            {
                TrackUsage("GameServer", "StartupTime");
                return GameServer.Instance.StartupTime;
            }
        }

        public int TickCount
        {
            get
            {
                TrackUsage("GameServer", "TickCount");
                return GameServer.Instance.TickCount;
            }
        }

        #endregion

        #region Managers

        public WorldManager WorldManager
        {
            get
            {
                TrackUsage("GameServer", "WorldManager");
                return GameServer.Instance.WorldManager;
            }
        }

        public PlayerManager PlayerManager
        {
            get
            {
                TrackUsage("GameServer", "PlayerManager");
                return GameServer.Instance.PlayerManager;
            }
        }

        public NpcManager NpcManager
        {
            get
            {
                TrackUsage("GameServer", "NpcManager");
                return GameServer.Instance.NpcManager;
            }
        }

        public SimpleScheduler Scheduler
        {
            get
            {
                TrackUsage("GameServer", "Scheduler");
                return GameServer.Instance.Scheduler;
            }
        }

        public IKeepManager KeepManager
        {
            get
            {
                TrackUsage("GameServer", "KeepManager");
                return GameServer.KeepManager;
            }
        }

        public IServerRules ServerRules
        {
            get
            {
                TrackUsage("GameServer", "ServerRules");
                return GameServer.ServerRules;
            }
        }

        #endregion

        #region Logging Services

        public Logger Log
        {
            get
            {
                TrackUsage("GameServer", "Log");
                return GameServer.Instance.Log;
            }
        }

        public void LogGMAction(string text)
        {
            TrackUsage("GameServer", "LogGMAction");
            GameServer.Instance.LogGMAction(text);
        }

        public void LogCheatAction(string text)
        {
            TrackUsage("GameServer", "LogCheatAction");
            GameServer.Instance.LogCheatAction(text);
        }

        public void LogDualIPAction(string text)
        {
            TrackUsage("GameServer", "LogDualIPAction");
            GameServer.Instance.LogDualIPAction(text);
        }

        public void LogInventoryAction(string text)
        {
            TrackUsage("GameServer", "LogInventoryAction");
            GameServer.Instance.LogInventoryAction(text);
        }

        #endregion

        #region Server Control

        public bool Start()
        {
            TrackUsage("GameServer", "Start");
            return GameServer.Instance.Start();
        }

        public void Stop()
        {
            TrackUsage("GameServer", "Stop");
            GameServer.Instance.Stop();
        }

        public void Open()
        {
            TrackUsage("GameServer", "Open");
            GameServer.Instance.Open();
        }

        public void Close()
        {
            TrackUsage("GameServer", "Close");
            GameServer.Instance.Close();
        }

        #endregion

        #region Save Operations

        public int SaveInterval
        {
            get
            {
                TrackUsage("GameServer", "SaveInterval_Get");
                return GameServer.Instance.SaveInterval;
            }
            set
            {
                TrackUsage("GameServer", "SaveInterval_Set");
                GameServer.Instance.SaveInterval = value;
            }
        }

        #endregion

        #region Network Operations

        public object GetUdpStatistics()
        {
            TrackUsage("GameServer", "GetUdpStatistics");
            // This would need to be implemented based on actual GameServer UDP statistics
            return null;
        }

        #endregion

        #region Legacy Service Provider Extensions

        public ILanguageManager LanguageManager
        {
            get
            {
                TrackUsage("LanguageMgr", "Instance");
                // Return adapter when implemented
                throw new NotImplementedException("LanguageManager adapter not yet implemented");
            }
        }

        public IHouseManager HouseManager
        {
            get
            {
                TrackUsage("HouseMgr", "Instance");
                // Return adapter when implemented
                throw new NotImplementedException("HouseManager adapter not yet implemented");
            }
        }

        public IQuestManager QuestManager
        {
            get
            {
                TrackUsage("QuestMgr", "Instance");
                // Return adapter when implemented
                throw new NotImplementedException("QuestManager adapter not yet implemented");
            }
        }

        public IAppealManager AppealManager
        {
            get
            {
                TrackUsage("AppealMgr", "Instance");
                // Return adapter when implemented
                throw new NotImplementedException("AppealManager adapter not yet implemented");
            }
        }

        #endregion

        #region Service Resolution (Bridge to DI)

        public T GetService<T>() where T : class
        {
            try
            {
                var service = _serviceProvider.GetService<T>();
                if (service != null)
                {
                    _logger.LogDebug("Resolved service {ServiceType} from DI container", typeof(T).Name);
                }
                return service;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve service {ServiceType} from DI container", typeof(T).Name);
                return null;
            }
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            service = GetService<T>();
            return service != null;
        }

        public IEnumerable<T> GetServices<T>() where T : class
        {
            try
            {
                var services = _serviceProvider.GetServices<T>();
                _logger.LogDebug("Resolved {Count} services of type {ServiceType}", 
                    services?.Count() ?? 0, typeof(T).Name);
                return services ?? Enumerable.Empty<T>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve services of type {ServiceType}", typeof(T).Name);
                return Enumerable.Empty<T>();
            }
        }

        #endregion

        #region Usage Tracking

        private void TrackUsage(string className, string memberName, [CallerMemberName] string callerMethod = "")
        {
            if (_configuration.EnableStaticUsageTracking && _usageTracker != null)
            {
                _usageTracker.TrackStaticUsage(className, memberName, callerMethod);
            }

            if (_configuration.LogStaticAccessWarnings)
            {
                _logger.LogDebug("Legacy static access: {ClassName}.{MemberName} from {CallerMethod}", 
                    className, memberName, callerMethod);
            }

            if (_configuration.ThrowOnDeprecatedAccess && 
                _usageTracker?.IsStaticMemberMigrated(className, memberName) == true)
            {
                throw new DeprecatedStaticAccessException(
                    $"{className}.{memberName}",
                    "Use dependency injection instead");
            }
        }

        #endregion
    }

    /// <summary>
    /// Thread-safe static usage tracker implementation for monitoring migration progress
    /// </summary>
    public class StaticUsageTracker : IStaticUsageTracker
    {
        private readonly ConcurrentDictionary<string, int> _usageCount = new();
        private readonly ConcurrentHashSet<string> _migratedMembers = new();
        private readonly LegacyAdapterConfiguration _configuration;

        public StaticUsageTracker(LegacyAdapterConfiguration configuration = null)
        {
            _configuration = configuration ?? new LegacyAdapterConfiguration();
        }

        public void TrackStaticUsage(string className, string memberName, string callerMethod)
        {
            var key = $"{className}.{memberName}";
            
            // Prevent memory leaks by limiting entries to a reasonable maximum
            const int MaxUsageEntries = 10000;
            if (_usageCount.Count < MaxUsageEntries)
            {
                _usageCount.AddOrUpdate(key, 1, (k, v) => v + 1);
            }
        }

        public Dictionary<string, int> GetUsageStatistics()
        {
            return new Dictionary<string, int>(_usageCount);
        }

        public bool IsStaticMemberMigrated(string className, string memberName)
        {
            var key = $"{className}.{memberName}";
            return _migratedMembers.Contains(key);
        }

        public void MarkStaticMemberMigrated(string className, string memberName)
        {
            var key = $"{className}.{memberName}";
            _migratedMembers.Add(key);
        }
    }

    /// <summary>
    /// Thread-safe HashSet implementation using ConcurrentDictionary
    /// </summary>
    public class ConcurrentHashSet<T>
    {
        private readonly ConcurrentDictionary<T, byte> _dictionary = new();

        public bool Add(T item) => _dictionary.TryAdd(item, 0);
        
        public bool Contains(T item) => _dictionary.ContainsKey(item);
        
        public bool Remove(T item) => _dictionary.TryRemove(item, out _);
        
        public void Clear() => _dictionary.Clear();
        
        public int Count => _dictionary.Count;
    }

    /// <summary>
    /// Extension methods for registering legacy adapter services
    /// </summary>
    public static class LegacyAdapterServiceExtensions
    {
        /// <summary>
        /// Add legacy adapter services to the DI container
        /// </summary>
        public static IServiceCollection AddLegacyGameServerAdapter(
            this IServiceCollection services,
            LegacyAdapterConfiguration configuration = null)
        {
            var config = configuration ?? new LegacyAdapterConfiguration();
            services.AddSingleton(config);
            services.AddSingleton<IStaticUsageTracker>(provider => 
                new StaticUsageTracker(provider.GetRequiredService<LegacyAdapterConfiguration>()));
            services.AddSingleton<ILegacyGameServer, LegacyGameServerAdapter>();
            services.AddSingleton<ILegacyServiceProvider, LegacyGameServerAdapter>();

            return services;
        }

        /// <summary>
        /// Add legacy adapter with custom configuration
        /// </summary>
        public static IServiceCollection AddLegacyGameServerAdapter(
            this IServiceCollection services,
            Action<LegacyAdapterConfiguration> configure)
        {
            var configuration = new LegacyAdapterConfiguration();
            configure?.Invoke(configuration);

            return services.AddLegacyGameServerAdapter(configuration);
        }

        /// <summary>
        /// Add legacy adapter with tracking enabled
        /// </summary>
        public static IServiceCollection AddLegacyGameServerAdapterWithTracking(
            this IServiceCollection services)
        {
            return services.AddLegacyGameServerAdapter(config =>
            {
                config.EnableStaticUsageTracking = true;
                config.LogStaticAccessWarnings = true;
                config.EnablePerformanceMetrics = true;
            });
        }

        /// <summary>
        /// Add legacy adapter with strict migration mode
        /// </summary>
        public static IServiceCollection AddLegacyGameServerAdapterStrict(
            this IServiceCollection services)
        {
            return services.AddLegacyGameServerAdapter(config =>
            {
                config.EnableStaticUsageTracking = true;
                config.ThrowOnDeprecatedAccess = true;
                config.LogStaticAccessWarnings = true;
            });
        }
    }
} 