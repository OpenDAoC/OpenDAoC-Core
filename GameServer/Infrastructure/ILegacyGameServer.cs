using System;
using System.Collections.Generic;
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
    /// Legacy adapter interface for gradual migration from GameServer.Instance static access
    /// This interface wraps existing GameServer functionality to enable dependency injection
    /// while maintaining backward compatibility during the transition period
    /// </summary>
    public interface ILegacyGameServer
    {
        #region Core Properties
        
        /// <summary>
        /// Gets the database instance
        /// </summary>
        IObjectDatabase Database { get; }

        /// <summary>
        /// Gets the server configuration
        /// </summary>
        GameServerConfiguration Configuration { get; }

        /// <summary>
        /// Gets the server status
        /// </summary>
        EGameServerStatus ServerStatus { get; }

        /// <summary>
        /// Gets the server start time
        /// </summary>
        DateTime StartupTime { get; }

        /// <summary>
        /// Gets the number of milliseconds elapsed since server start
        /// </summary>
        int TickCount { get; }

        #endregion

        #region Managers

        /// <summary>
        /// Gets the world manager
        /// </summary>
        WorldManager WorldManager { get; }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        PlayerManager PlayerManager { get; }

        /// <summary>
        /// Gets the NPC manager
        /// </summary>
        NpcManager NpcManager { get; }

        /// <summary>
        /// Gets the scheduler
        /// </summary>
        SimpleScheduler Scheduler { get; }

        /// <summary>
        /// Gets the keep manager
        /// </summary>
        IKeepManager KeepManager { get; }

        /// <summary>
        /// Gets the server rules
        /// </summary>
        IServerRules ServerRules { get; }

        #endregion

        #region Logging Services

        /// <summary>
        /// Gets the main server logger
        /// </summary>
        Logger Log { get; }

        /// <summary>
        /// Log a GM action
        /// </summary>
        void LogGMAction(string text);

        /// <summary>
        /// Log a cheat action
        /// </summary>
        void LogCheatAction(string text);

        /// <summary>
        /// Log a dual IP action
        /// </summary>
        void LogDualIPAction(string text);

        /// <summary>
        /// Log an inventory action
        /// </summary>
        void LogInventoryAction(string text);

        #endregion

        #region Server Control

        /// <summary>
        /// Start the server
        /// </summary>
        bool Start();

        /// <summary>
        /// Stop the server
        /// </summary>
        void Stop();

        /// <summary>
        /// Open the server for connections
        /// </summary>
        void Open();

        /// <summary>
        /// Close the server to new connections
        /// </summary>
        void Close();

        #endregion

        #region Save Operations

        /// <summary>
        /// Gets or sets the world save interval
        /// </summary>
        int SaveInterval { get; set; }

        #endregion

        #region Network Operations

        /// <summary>
        /// Get UDP statistics
        /// </summary>
        object GetUdpStatistics();

        #endregion
    }

    /// <summary>
    /// Extended interface for legacy services that may be accessed statically
    /// Provides access to commonly used services during migration
    /// </summary>
    public interface ILegacyServiceProvider : ILegacyGameServer
    {
        #region Common Services (for gradual migration)

        /// <summary>
        /// Gets the language manager (LanguageMgr equivalent)
        /// </summary>
        ILanguageManager LanguageManager { get; }

        /// <summary>
        /// Gets the house manager (HouseMgr equivalent)
        /// </summary>
        IHouseManager HouseManager { get; }

        /// <summary>
        /// Gets the quest manager (QuestMgr equivalent)
        /// </summary>
        IQuestManager QuestManager { get; }

        /// <summary>
        /// Gets the appeal manager (AppealMgr equivalent)
        /// </summary>
        IAppealManager AppealManager { get; }

        #endregion

        #region Service Resolution (Bridge to DI)

        /// <summary>
        /// Resolve a service by type (bridge to DI container)
        /// </summary>
        T GetService<T>() where T : class;

        /// <summary>
        /// Try to resolve a service by type
        /// </summary>
        bool TryGetService<T>(out T service) where T : class;

        /// <summary>
        /// Get all services of a specific type
        /// </summary>
        IEnumerable<T> GetServices<T>() where T : class;

        #endregion
    }

    /// <summary>
    /// Interface for wrapping commonly accessed static managers
    /// Used during migration to provide dependency injection access
    /// </summary>
    public interface ILegacyManagerProvider
    {
        #region Static Manager Wrappers

        /// <summary>
        /// Wraps WorldMgr static access
        /// </summary>
        IWorldManagerAdapter WorldMgrAdapter { get; }

        /// <summary>
        /// Wraps GuildMgr static access
        /// </summary>
        IGuildManagerAdapter GuildMgrAdapter { get; }

        /// <summary>
        /// Wraps RelicMgr static access
        /// </summary>
        IRelicManagerAdapter RelicMgrAdapter { get; }

        /// <summary>
        /// Wraps DoorMgr static access
        /// </summary>
        IDoorManagerAdapter DoorMgrAdapter { get; }

        /// <summary>
        /// Wraps AreaMgr static access
        /// </summary>
        IAreaManagerAdapter AreaMgrAdapter { get; }

        /// <summary>
        /// Wraps BoatMgr static access
        /// </summary>
        IBoatManagerAdapter BoatMgrAdapter { get; }

        /// <summary>
        /// Wraps CraftingMgr static access
        /// </summary>
        ICraftingManagerAdapter CraftingMgrAdapter { get; }

        /// <summary>
        /// Wraps FactionMgr static access
        /// </summary>
        IFactionManagerAdapter FactionMgrAdapter { get; }

        #endregion
    }

    /// <summary>
    /// Placeholder interfaces for manager adapters (to be implemented in FDIS-006)
    /// These will wrap the existing static manager classes
    /// </summary>
    public interface IWorldManagerAdapter { }
    public interface IGuildManagerAdapter { }
    public interface IRelicManagerAdapter { }
    public interface IDoorManagerAdapter { }
    public interface IAreaManagerAdapter { }
    public interface IBoatManagerAdapter { }
    public interface ICraftingManagerAdapter { }
    public interface IFactionManagerAdapter { }

    /// <summary>
    /// Placeholder interfaces for service adapters (to be implemented later)
    /// These will wrap existing service classes
    /// </summary>
    public interface ILanguageManager { }
    public interface IHouseManager { }
    public interface IQuestManager { }
    public interface IAppealManager { }

    /// <summary>
    /// Migration helper interface for tracking static usage
    /// Helps identify and track remaining static dependencies during migration
    /// </summary>
    public interface IStaticUsageTracker
    {
        /// <summary>
        /// Track usage of a static member
        /// </summary>
        void TrackStaticUsage(string className, string memberName, string callerMethod);

        /// <summary>
        /// Get usage statistics
        /// </summary>
        Dictionary<string, int> GetUsageStatistics();

        /// <summary>
        /// Check if a static member has been migrated
        /// </summary>
        bool IsStaticMemberMigrated(string className, string memberName);

        /// <summary>
        /// Mark a static member as migrated
        /// </summary>
        void MarkStaticMemberMigrated(string className, string memberName);
    }

    /// <summary>
    /// Configuration for legacy adapter behavior
    /// Allows fine-tuning of the migration process
    /// </summary>
    public class LegacyAdapterConfiguration
    {
        /// <summary>
        /// Enable static usage tracking
        /// </summary>
        public bool EnableStaticUsageTracking { get; set; } = false;

        /// <summary>
        /// Throw exceptions on deprecated static access
        /// </summary>
        public bool ThrowOnDeprecatedAccess { get; set; } = false;

        /// <summary>
        /// Log warnings for static access
        /// </summary>
        public bool LogStaticAccessWarnings { get; set; } = true;

        /// <summary>
        /// Maximum number of static access warnings per member
        /// </summary>
        public int MaxWarningsPerMember { get; set; } = 10;

        /// <summary>
        /// Enable performance metrics for legacy adapter calls
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = false;
    }

    /// <summary>
    /// Exception thrown when deprecated static access is attempted
    /// </summary>
    public class DeprecatedStaticAccessException : InvalidOperationException
    {
        public string StaticMember { get; }
        public string RecommendedAlternative { get; }

        public DeprecatedStaticAccessException(string staticMember, string recommendedAlternative)
            : base($"Static access to {staticMember} is deprecated. Use {recommendedAlternative} instead.")
        {
            StaticMember = staticMember;
            RecommendedAlternative = recommendedAlternative;
        }
    }
} 