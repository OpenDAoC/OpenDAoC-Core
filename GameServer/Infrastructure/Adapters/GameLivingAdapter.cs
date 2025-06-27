using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.Interfaces.Core;
using Microsoft.Extensions.Logging;

namespace DOL.GS.Infrastructure.Adapters
{
    /// <summary>
    /// Adapter to bridge legacy GameLiving with new interfaces
    /// Follows single responsibility principle and clean architecture
    /// </summary>
    public class GameLivingAdapter : GameObjectAdapter
    {
        protected readonly GameLiving _legacyLiving;
        protected new readonly ILogger<GameLivingAdapter> _logger;

        public GameLivingAdapter(GameLiving legacyLiving, ILogger<GameLivingAdapter> logger = null)
            : base(legacyLiving, logger as ILogger<GameObjectAdapter>)
        {
            _legacyLiving = legacyLiving ?? throw new ArgumentNullException(nameof(legacyLiving));
            _logger = logger;
        }

        #region ILiving Implementation

        public virtual int Level => _legacyLiving.Level;

        #endregion

        #region Enhanced Object Properties

        public virtual int Health => Math.Min((int)_legacyLiving.Health, (int)_legacyLiving.MaxHealth);
        public virtual int MaxHealth => (int)_legacyLiving.MaxHealth;
        public virtual bool IsAlive => _legacyLiving.IsAlive;
        public virtual bool InCombat => _legacyLiving.InCombat;

        protected override eObjectType GetObjectTypeFromLegacy()
        {
            // Override to provide more specific object type mapping for living objects
            return _legacyLiving switch
            {
                GamePlayer _ => eObjectType.GenericItem, // Note: eObjectType is for items, not creatures
                GameNPC _ => eObjectType.GenericItem,
                _ => eObjectType.GenericItem
            };
        }

        #endregion

        #region Legacy Access

        /// <summary>
        /// Provides access to underlying GameLiving for gradual migration
        /// </summary>
        public GameLiving LegacyLiving => _legacyLiving;

        #endregion
    }

    /// <summary>
    /// Specialized adapter for GamePlayer objects following clean architecture
    /// Provides player-specific functionality with proper type safety
    /// </summary>
    public class GamePlayerAdapter : GameLivingAdapter
    {
        private readonly GamePlayer _legacyPlayer;
        private new readonly ILogger<GamePlayerAdapter> _logger;

        public GamePlayerAdapter(GamePlayer legacyPlayer, ILogger<GamePlayerAdapter> logger = null)
            : base(legacyPlayer, logger as ILogger<GameLivingAdapter>)
        {
            _legacyPlayer = legacyPlayer ?? throw new ArgumentNullException(nameof(legacyPlayer));
            _logger = logger;
        }

        #region Player-Specific Properties

        public virtual string AccountName => _legacyPlayer.Client?.Account?.Name ?? string.Empty;
        public virtual int RealmPoints => (int)_legacyPlayer.RealmPoints; // Explicit cast from long to int
        public virtual byte RealmLevel => (byte)_legacyPlayer.RealmLevel; // Explicit cast from int to byte
        public virtual eRealm Realm => _legacyPlayer.Realm;

        #endregion

        #region Legacy Access

        public GamePlayer LegacyPlayer => _legacyPlayer;

        #endregion
    }

    /// <summary>
    /// Specialized adapter for GameNPC objects following clean architecture
    /// Provides NPC-specific functionality with proper type safety
    /// </summary>
    public class GameNPCAdapter : GameLivingAdapter
    {
        private readonly GameNPC _legacyNPC;
        private new readonly ILogger<GameNPCAdapter> _logger;

        public GameNPCAdapter(GameNPC legacyNPC, ILogger<GameNPCAdapter> logger = null)
            : base(legacyNPC, logger as ILogger<GameLivingAdapter>)
        {
            _legacyNPC = legacyNPC ?? throw new ArgumentNullException(nameof(legacyNPC));
            _logger = logger;
        }

        #region NPC-Specific Properties

        // Use safe property checks since IsMerchant might not exist on all GameNPC types
        public virtual bool IsMerchant => _legacyNPC.GetType().Name.Contains("Merchant") || 
                                         (_legacyNPC as object)?.GetType().GetProperty("IsMerchant")?.GetValue(_legacyNPC) as bool? == true;
        public virtual bool IsAggressive => _legacyNPC.IsAggressive;
        public virtual ushort Model => _legacyNPC.Model;
        public virtual byte Size => _legacyNPC.Size;

        #endregion

        #region Legacy Access

        public GameNPC LegacyNPC => _legacyNPC;

        #endregion
    }

    /// <summary>
    /// Extension methods for GameLiving adapter creation
    /// </summary>
    public static class GameLivingAdapterExtensions
    {
        public static GameLivingAdapter ToLivingAdapter(this GameLiving gameLiving, ILogger<GameLivingAdapter> logger = null)
        {
            return new GameLivingAdapter(gameLiving, logger);
        }

        public static GamePlayerAdapter ToPlayerAdapter(this GamePlayer gamePlayer, ILogger<GamePlayerAdapter> logger = null)
        {
            return new GamePlayerAdapter(gamePlayer, logger);
        }

        public static GameNPCAdapter ToNPCAdapter(this GameNPC gameNPC, ILogger<GameNPCAdapter> logger = null)
        {
            return new GameNPCAdapter(gameNPC, logger);
        }
    }
} 