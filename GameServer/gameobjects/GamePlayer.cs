using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Appeal;
using DOL.GS.Effects;
using DOL.GS.Housing;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.PacketHandler.Client.v168;
using DOL.GS.PlayerClass;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using DOL.GS.RealmAbilities;
using DOL.GS.ServerProperties;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.GS.Utils;
using DOL.Language;
using DOL.Logging;
using JNogueira.Discord.Webhook.Client;

namespace DOL.GS
{
    /// <summary>
    /// This class represents a player inside the game
    /// </summary>
    public class GamePlayer : GameLiving, IGameStaticItemOwner, IPooledList<GamePlayer>
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const int SECONDS_TO_QUIT_ON_LINKDEATH = 60;

        private readonly Lock _tradeLock = new();
        public new PlayerMovementComponent movementComponent;
        public new PlayerStyleComponent styleComponent;

        public override eGameObjectType GameObjectType => eGameObjectType.PLAYER;
        public double SpecLock { get; set; }
        public long NextWorldUpdate { get; set; }
        public Lock AwardLock { get; private set; } = new(); // Used by `AbstractServerRules` exclusively.

        public ECSGameTimer PredatorTimeoutTimer
        {
            get
            {
                if (m_predatortimer == null) m_predatortimer = new ECSGameTimer(this);
                return m_predatortimer;
            }
            set { m_predatortimer = value; }
        }

        protected ECSGameTimer m_predatortimer;

        #region Client/Character/VariousFlags

        /// <summary>
        /// This is our gameclient!
        /// </summary>
        protected readonly GameClient m_client;

        /// <summary>
        /// This holds the character this player is
        /// based on!
        /// (renamed and private, cause if derive is needed overwrite PlayerCharacter)
        /// </summary>
        protected DbCoreCharacter m_dbCharacter;

        /// <summary>
        /// The guild id this character belong to
        /// </summary>
        protected string m_guildId;

        /// <summary>
        /// Char spec points checked on load
        /// </summary>
        protected bool SpecPointsOk = true;

        /// <summary>
        /// Has this player entered the game, will be
        /// true after the first time the char enters
        /// the world
        /// </summary>
        protected bool m_enteredGame;

        /// <summary>
        /// Is this player being 'jumped' to a new location?
        /// </summary>
        public bool IsJumping { get; set; }

        /// <summary>
        /// true if the targetObject is visible
        /// </summary>
        protected bool m_targetInView;

        /// <summary>
        /// Property for the optional away from keyboard message.
        /// </summary>
        public static readonly string AFK_MESSAGE = "afk_message";

        /// <summary>
        /// Property for the optional away from keyboard message.
        /// </summary>
        public static readonly string QUICK_CAST_CHANGE_TICK = "quick_cast_change_tick";

        /// <summary>
        /// Last spell cast from a used item
        /// </summary>
        public static readonly string LAST_USED_ITEM_SPELL = "last_used_item_spell";

        /// <summary>
        /// Effectiveness of the rez sick that should be applied. This is set by rez spells just before rezzing.
        /// </summary>
        public static readonly string RESURRECT_REZ_SICK_EFFECTIVENESS = "RES_SICK_EFFECTIVENESS";

        /// <summary>
        /// Array that stores ML step completition
        /// </summary>
        private ArrayList m_mlSteps = new ArrayList();

        private bool m_gmStealthed = false;
        public bool GMStealthed { get { return m_gmStealthed; } set { m_gmStealthed = value; } }

        /// <summary>
        /// Can this living accept any item regardless of tradable or droppable?
        /// </summary>
        public override bool CanTradeAnyItem { get { return Client.Account.PrivLevel > (int)ePrivLevel.Player; }}

        /// <summary>
        /// Gets or sets the targetObject's visibility
        /// </summary>
        public override bool TargetInView
        {
            get
            {
                if (GetDistanceTo(TargetObject) <= TargetInViewAlwaysTrueMinRange)
                    return true;

                return m_targetInView;
            }
            set => m_targetInView = value;
        }

        public override int TargetInViewAlwaysTrueMinRange => (TargetObject is GamePlayer targetPlayer && targetPlayer.IsMoving) ? 100 : 64;

        private Dictionary<RandomDeckEvent, RandomDeck> _randomDecks = new();

        public override bool Chance(RandomDeckEvent deckEvent, int chancePercent)
        {
            return !Properties.OVERRIDE_DECK_RNG && _randomDecks.TryGetValue(deckEvent, out RandomDeck deck) ?
                deck.Draw() < chancePercent :
                base.Chance(deckEvent, chancePercent);
        }

        public override bool Chance(RandomDeckEvent deckEvent, double chancePercent)
        {
            return GetPseudoDouble(deckEvent) < chancePercent;
        }

        public override double GetPseudoDouble(RandomDeckEvent deckEvent)
        {
            return !Properties.OVERRIDE_DECK_RNG && _randomDecks.TryGetValue(deckEvent, out RandomDeck deck) ?
                (deck.Draw() + Util.RandomDouble()) / 100.0 :
                base.GetPseudoDouble(deckEvent);
        }

        public override double GetPseudoDoubleIncl(RandomDeckEvent deckEvent)
        {
            return !Properties.OVERRIDE_DECK_RNG && _randomDecks.TryGetValue(deckEvent, out RandomDeck deck) ?
                (deck.Draw() + Util.RandomDoubleIncl()) / 100.0 :
                base.GetPseudoDoubleIncl(deckEvent);
        }

        public void InitializeRandomDecks()
        {
            foreach (RandomDeckEvent deckEvent in Enum.GetValues<RandomDeckEvent>())
                _randomDecks[deckEvent] = new();
        }

        /// <summary>
        /// Holds the ground target visibility flag
        /// </summary>
        protected bool m_groundtargetInView;

        /// <summary>
        /// Gets or sets the GroundTargetObject's visibility
        /// </summary>
        public override bool GroundTargetInView
        {
            get { return m_groundtargetInView; }
            set { m_groundtargetInView = value; }
        }

        protected int m_OutOfClassROGPercent = 0;

        public int OutOfClassROGPercent
        {
            get { return m_OutOfClassROGPercent; }
            set { m_OutOfClassROGPercent = value; }
        }

        /// <summary>
        /// Player is in BG ?
        /// </summary>
        protected bool m_isInBG;
        public bool isInBG
        {
            get { return m_isInBG; }
            set { m_isInBG = value; }
        }

        protected bool m_usedetailedcombatlog = false;

        public bool UseDetailedCombatLog
        {
            get { return m_usedetailedcombatlog; }
            set { m_usedetailedcombatlog = value;}
        }

        public eXPLogState XPLogState
        {
            get { return m_xplogstate; }
            set { m_xplogstate = value; }
        }

        private eXPLogState m_xplogstate = 0;

        /// <summary>
        /// Current warmap page
        /// </summary>
        private volatile byte m_warmapPage = 1;
        public byte WarMapPage
        {
            get { return m_warmapPage; }
            set { m_warmapPage = value; }
        }

        /// <summary>
        /// Returns the GameClient of this Player
        /// </summary>
        public virtual GameClient Client
        {
            get { return m_client; }
        }

        /// <summary>
        /// Returns the PacketSender for this player
        /// </summary>
        public virtual IPacketLib Out
        {
            get { return Client.Out; }
        }

        /// <summary>
        /// The character the player is based on
        /// </summary>
        internal DbCoreCharacter DBCharacter
        {
            get { return m_dbCharacter; }
        }

        /// <summary>
        /// Has this player entered the game for the first
        /// time after logging on (not Zoning!)
        /// </summary>
        public bool EnteredGame
        {
            get { return m_enteredGame; }
            set { m_enteredGame = value; }
        }

        protected DateTime m_previousLoginDate = DateTime.MinValue;
        /// <summary>
        /// What was the last time this player logged in?
        /// </summary>
        public DateTime PreviousLoginDate
        {
            get { return m_previousLoginDate; }
            set { m_previousLoginDate = value; }
        }

        /// <summary>
        /// Gets or sets the anonymous flag for this player
        /// (delegate to property in PlayerCharacter)
        /// </summary>
        public bool IsAnonymous
        {
            get { return DBCharacter != null ? DBCharacter.IsAnonymous && (ServerProperties.Properties.ANON_MODIFIER != -1) : false; }
            set
            {
                var old = IsAnonymous;
                if (DBCharacter != null)
                    DBCharacter.IsAnonymous = value;

                if (old != IsAnonymous)
                    GameEventMgr.Notify(GamePlayerEvent.ChangeAnonymous, this);
            }
        }

        /// <summary>
        /// Whether or not the player can be attacked.
        /// </summary>
        public override bool IsAttackable { get { return (Client.Account.PrivLevel <= (uint)ePrivLevel.Player && base.IsAttackable); }}

        /// <summary>
        /// Can this player use cross realm items
        /// </summary>
        public virtual bool CanUseCrossRealmItems { get { return ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS; }}

        protected bool m_canUseSlashLevel = false;
        public bool CanUseSlashLevel { get { return m_canUseSlashLevel; }}

        /// <summary>
        /// if player uses debug before (to prevent hack client fly mode for players using debug and then turning it off)
        /// </summary>
        protected bool m_canFly;

        /// <summary>
        /// Is this player allowed to fly?
        /// This should only be set in debug command handler.  If player is flying but this flag is false then fly hack is detected
        /// </summary>
        public bool IsAllowedToFly
        {
            get { return m_canFly; }
            set { m_canFly = value; }
        }

        private bool m_statsAnon = false;

        /// <summary>
        /// Gets or sets the stats anon flag for the command /statsanon
        /// (delegate to property in PlayerCharacter)
        /// </summary>
        public bool StatsAnonFlag
        {
            get { return m_statsAnon; }
            set { m_statsAnon = value; }
        }

        protected bool m_lastDeathPvP;

        public bool LastDeathPvP
        {
            get { return m_lastDeathPvP; }
            set { m_lastDeathPvP = value; }
        }

        protected bool m_wasmovedbycorpsesummoner;

        public bool WasMovedByCorpseSummoner
        {
            get { return m_wasmovedbycorpsesummoner; }
            set { m_wasmovedbycorpsesummoner = value; }
        }

        public LosCheckHandler LosCheckHandler { get; }

        // Used by the client service exclusively.
        public PlayerObjectCache PlayerObjectCache { get; } = new();

        #region Database Accessor

        /// <summary>
        /// Gets or sets the Database ObjectId for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public string ObjectId
        {
            get { return DBCharacter != null ? DBCharacter.ObjectId : InternalID; }
            set { if (DBCharacter != null) DBCharacter.ObjectId = value; }
        }

        /// <summary>
        /// Gets or sets the show guild logins flag for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool ShowGuildLogins
        {
            get { return DBCharacter != null ? DBCharacter.ShowGuildLogins : false; }
            set { if (DBCharacter != null) DBCharacter.ShowGuildLogins = value; }
        }

        /// <summary>
        /// Gets or sets the gain XP flag for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool GainXP
        {
            get { return DBCharacter != null ? DBCharacter.GainXP : true; }
            set { if (DBCharacter != null) DBCharacter.GainXP = value; }
        }

        /// <summary>
        /// Gets or sets the gain RP flag for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool GainRP
        {
            get { return (DBCharacter != null ? DBCharacter.GainRP : true); }
            set { if (DBCharacter != null) DBCharacter.GainRP = value; }
        }

        /// <summary>
        /// Gets or sets the roleplay flag for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool RPFlag
        {
            get { return (DBCharacter != null ? DBCharacter.RPFlag : true); }
            set { if (DBCharacter != null) DBCharacter.RPFlag = value; }
        }

        /// <summary>
        /// Gets or sets the hardcore flag for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool HCFlag
        {
            get { return (DBCharacter != null ? DBCharacter.HCFlag : true); }
            set { if (DBCharacter != null) DBCharacter.HCFlag = value; }
        }

        /// <summary>
        /// Gets or sets the HideSpecializationAPI for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool HideSpecializationAPI
        {
            get { return (DBCharacter != null ? DBCharacter.HideSpecializationAPI : false); }
            set { if (DBCharacter != null) DBCharacter.HideSpecializationAPI = value; }
        }

        /// <summary>
        /// Gets or sets the hardcore flag for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool HCCompleted
        {
            get { return (DBCharacter != null ? DBCharacter.HCCompleted : true); }
            set { if (DBCharacter != null) DBCharacter.HCCompleted = value; }
        }

        /// <summary>
        /// gets or sets the guildnote for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public string GuildNote
        {
            get { return DBCharacter != null ? DBCharacter.GuildNote : String.Empty; }
            set { if (DBCharacter != null) DBCharacter.GuildNote = value; }
        }

        /// <summary>
        /// Gets or sets the autoloot flag for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool Autoloot
        {
            get { return DBCharacter != null ? DBCharacter.Autoloot : true; }
            set { if (DBCharacter != null) DBCharacter.Autoloot = value; }
        }

        /// <summary>
        /// Gets or sets the advisor flag for this player
        /// (delegate to property in PlayerCharacter)
        /// </summary>
        public bool Advisor
        {
            get { return DBCharacter != null ? DBCharacter.Advisor : false; }
            set { if (DBCharacter != null) DBCharacter.Advisor = value; }
        }

        /// <summary>
        /// Gets or sets the SerializedFriendsList for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public string[] SerializedFriendsList
        {
            get { return DBCharacter != null ? DBCharacter.SerializedFriendsList.Split(',').Select(name => name.Trim()).Where(name => !string.IsNullOrEmpty(name)).ToArray() : new string[0]; }
            set { if (DBCharacter != null) DBCharacter.SerializedFriendsList = string.Join(",", value.Select(name => name.Trim()).Where(name => !string.IsNullOrEmpty(name))); }
        }

        /// <summary>
        /// Gets or sets the NotDisplayedInHerald for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public byte NotDisplayedInHerald
        {
            get { return DBCharacter != null ? DBCharacter.NotDisplayedInHerald : (byte)0; }
            set { if (DBCharacter != null) DBCharacter.NotDisplayedInHerald = value; }
        }

        /// <summary>
        /// Gets or sets the LastFreeLevel for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int LastFreeLevel
        {
            get { return DBCharacter != null ? DBCharacter.LastFreeLevel : 0; }
            set { if (DBCharacter != null) DBCharacter.LastFreeLevel = value; }
        }

        /// <summary>
        /// Gets or sets the LastFreeLeveled for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public DateTime LastFreeLeveled
        {
            get { return DBCharacter != null ? DBCharacter.LastFreeLeveled : DateTime.MinValue; }
            set { if (DBCharacter != null) DBCharacter.LastFreeLeveled = value; }
        }

        /// <summary>
        /// Gets or sets the SerializedIgnoreList for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public string[] SerializedIgnoreList
        {
            get { return DBCharacter != null ? DBCharacter.SerializedIgnoreList.Split(',').Select(name => name.Trim()).Where(name => !string.IsNullOrEmpty(name)).ToArray() : new string[0]; }
            set { if (DBCharacter != null) DBCharacter.SerializedIgnoreList = string.Join(",", value.Select(name => name.Trim()).Where(name => !string.IsNullOrEmpty(name))); }
        }

        /// <summary>
        /// Gets or sets the UsedLevelCommand for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool UsedLevelCommand
        {
            get { return DBCharacter != null ? DBCharacter.UsedLevelCommand : false; }
            set { if (DBCharacter != null) DBCharacter.UsedLevelCommand = value; }
        }

        /// <summary>
        /// Gets or sets the BindHouseRegion for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindHouseRegion
        {
            get { return DBCharacter != null ? DBCharacter.BindHouseRegion : 0; }
            set { if (DBCharacter != null) DBCharacter.BindHouseRegion = value; }
        }

        /// <summary>
        /// Gets or sets the BindHouseXpos for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindHouseXpos
        {
            get { return DBCharacter != null ? DBCharacter.BindHouseXpos : 0; }
            set { if (DBCharacter != null) DBCharacter.BindHouseXpos = value; }
        }

        /// <summary>
        /// Gets or sets the BindHouseYpos for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindHouseYpos
        {
            get { return DBCharacter != null ? DBCharacter.BindHouseYpos : 0; }
            set { if (DBCharacter != null) DBCharacter.BindHouseYpos = value; }
        }

        /// <summary>
        /// Gets or sets BindHouseZpos for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindHouseZpos
        {
            get { return DBCharacter != null ? DBCharacter.BindHouseZpos : 0; }
            set { if (DBCharacter != null) DBCharacter.BindHouseZpos = value; }
        }

        /// <summary>
        /// Gets or sets the BindHouseHeading for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindHouseHeading
        {
            get { return DBCharacter != null ? DBCharacter.BindHouseHeading : 0; }
            set { if (DBCharacter != null) DBCharacter.BindHouseHeading = value; }
        }

        /// <summary>
        /// Gets or sets the CustomisationStep for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public byte CustomisationStep
        {
            get { return DBCharacter != null ? DBCharacter.CustomisationStep : (byte)0; }
            set { if (DBCharacter != null) DBCharacter.CustomisationStep = value; }
        }

        /// <summary>
        /// Gets or sets the IgnoreStatistics for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool IgnoreStatistics
        {
            get { return DBCharacter != null ? DBCharacter.IgnoreStatistics : false; }
            set { if (DBCharacter != null) DBCharacter.IgnoreStatistics = value; }
        }

        /// <summary>
        /// Gets or sets the DeathTime for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public long DeathTime
        {
            get { return DBCharacter != null ? DBCharacter.DeathTime : 0; }
            set { if (DBCharacter != null) DBCharacter.DeathTime = value; }
        }

        /// <summary>
        /// Gets or sets the ShowXFireInfo for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public bool ShowXFireInfo
        {
            get { return DBCharacter != null ? DBCharacter.ShowXFireInfo : false; }
            set { if (DBCharacter != null) DBCharacter.ShowXFireInfo = value; }
        }

        /// <summary>
        /// Gets or sets the BindRegion for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindRegion
        {
            get { return DBCharacter != null ? DBCharacter.BindRegion : 0; }
            set { if (DBCharacter != null) DBCharacter.BindRegion = value; }
        }

        /// <summary>
        /// Gets or sets the BindXpos for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindXpos
        {
            get { return DBCharacter != null ? DBCharacter.BindXpos : 0; }
            set { if (DBCharacter != null) DBCharacter.BindXpos = value; }
        }

        /// <summary>
        /// Gets or sets the BindYpos for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindYpos
        {
            get { return DBCharacter != null ? DBCharacter.BindYpos : 0; }
            set { if (DBCharacter != null) DBCharacter.BindYpos = value; }
        }

        /// <summary>
        /// Gets or sets the BindZpos for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindZpos
        {
            get { return DBCharacter != null ? DBCharacter.BindZpos : 0; }
            set { if (DBCharacter != null) DBCharacter.BindZpos = value; }
        }

        /// <summary>
        /// Gets or sets the BindHeading for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int BindHeading
        {
            get { return DBCharacter != null ? DBCharacter.BindHeading : 0; }
            set { if (DBCharacter != null) DBCharacter.BindHeading = value; }
        }

        /// <summary>
        /// Gets or sets the Database MaxEndurance for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public int DBMaxEndurance
        {
            get { return DBCharacter != null ? DBCharacter.MaxEndurance : 100; }
            set { if (DBCharacter != null) DBCharacter.MaxEndurance = value; }
        }

        /// <summary>
        /// Gets AccountName for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public string AccountName
        {
            get { return DBCharacter != null ? DBCharacter.AccountName : string.Empty; }
        }

        /// <summary>
        /// Gets CreationDate for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public DateTime CreationDate
        {
            get { return DBCharacter != null ? DBCharacter.CreationDate : DateTime.MinValue; }
        }

        /// <summary>
        /// Gets LastPlayed for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public DateTime LastPlayed
        {
            get { return DBCharacter != null ? DBCharacter.LastPlayed : DateTime.MinValue; }
        }

        /// <summary>
        /// Gets or sets the BindYpos for this player
        /// (delegate to property in DBCharacter)
        /// </summary>
        public byte DeathCount
        {
            get { return DBCharacter != null ? DBCharacter.DeathCount : (byte)0; }
            set { if (DBCharacter != null) DBCharacter.DeathCount = value; }
        }

        //public long PlayedTimeSinceLevel
        //{
        //    get { return DBCharacter != null ? DBCharacter.PlayedTimeSinceLevel : 0; }
        //    set { if (DBCharacter != null) DBCharacter.PlayedTimeSinceLevel = PlayedTimeSinceLevel; }
        //}

        /// <summary>
        /// Gets the last time this player leveled up
        /// </summary>
        public DateTime LastLevelUp
        {
            get { return DBCharacter != null ? DBCharacter.LastLevelUp : DateTime.MinValue; }
            set { if (DBCharacter != null) DBCharacter.LastLevelUp = value; }
        }
        #endregion

        #endregion

        #region Player Quitting

        public class QuitTimer : ECSGameTimerWrapperBase
        {
            private const int MAX_DURATION = 60000; // In milliseconds.
            private const int MIN_DURATION = 20000; // Must be inferior to MAX_DURATION.
            private static readonly int[] REMAINING_DURATIONS = [20, 15, 10, 5]; // Must be in descending order and not empty.

            private GamePlayer _owner;
            private Func<int> _onQuitTimerEnd;
            private int _remainingDurationsIndex = 1;
            private long _lastCombatTick;

            public QuitTimer(GamePlayer owner, Func<int> onQuitTimerEnd) : base(owner)
            {
                _owner = owner;
                _onQuitTimerEnd = onQuitTimerEnd;

                // Players can only quit instantaneously if they aren't in combat.
                // Don't bother starting the timer if we can quit instantaneously.
                if (_owner.Client.Account.PrivLevel > 1 || (ServerProperties.Properties.DISABLE_QUIT_TIMER && !_owner.Client.Player.InCombat))
                {
                    Quit();
                    return;
                }

                _lastCombatTick = GetLastCombatTick();
                int quitDuration = CalculateQuitDuration(_lastCombatTick);
                owner.Out.SendMessage(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GamePlayer.Quit.RecentlyInCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                owner.Out.SendMessage(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GamePlayer.Quit.YouWillQuit2", quitDuration), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                Start(CalculateFirstInterval(quitDuration));
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (!_owner.IsAlive || _owner.ObjectState is not eObjectState.Active)
                    return _onQuitTimerEnd();

                if (_owner.CraftTimer != null && _owner.CraftTimer.IsAlive)
                {
                    _owner.Out.SendMessage(LanguageMgr.GetTranslation(_owner.Client.Account.Language, "GamePlayer.Quit.CantQuitCrafting"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return _onQuitTimerEnd();
                }

                long newLastCombatTick = GetLastCombatTick();

                if (newLastCombatTick > _lastCombatTick)
                {
                    _remainingDurationsIndex = 1;
                    _lastCombatTick = newLastCombatTick;
                    return CalculateFirstInterval(CalculateQuitDuration(_lastCombatTick));
                }

                if (_remainingDurationsIndex == REMAINING_DURATIONS.Length)
                {
                    Quit();
                    return 0;
                }

                int currentRemainingDuration = REMAINING_DURATIONS[_remainingDurationsIndex];
                _owner.Out.SendMessage(LanguageMgr.GetTranslation(_owner.Client.Account.Language, "GamePlayer.Quit.YouWillQuit1", currentRemainingDuration), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return CalculateNextInterval();

                int CalculateNextInterval()
                {
                    _remainingDurationsIndex++;

                    if (_remainingDurationsIndex < REMAINING_DURATIONS.Length)
                        currentRemainingDuration -= REMAINING_DURATIONS[_remainingDurationsIndex];

                    return currentRemainingDuration * 1000;
                }
            }

            private long GetLastCombatTick()
            {
                return Math.Max(_owner.LastAttackedByEnemyTick, _owner.LastAttackTick);
            }

            private static int CalculateQuitDuration(long lastCombatTick)
            {
                int lastCombatTickOffset = MAX_DURATION - MIN_DURATION;

                if (GameLoop.GameLoopTime - lastCombatTick > lastCombatTickOffset)
                    lastCombatTick = GameLoop.GameLoopTime - lastCombatTickOffset;

                return Math.Max(0, (int) Math.Ceiling((MAX_DURATION - (GameLoop.GameLoopTime - lastCombatTick)) / 1000.0));
            }

            private static int CalculateFirstInterval(int quitDuration)
            {
                int result = REMAINING_DURATIONS[0];
                result = quitDuration - result;

                if (REMAINING_DURATIONS.Length > 1)
                {
                    result += REMAINING_DURATIONS[0];
                    result -= REMAINING_DURATIONS[1];
                }

                return result * 1000;
            }

            private void Quit()
            {
                if ((eCharacterClass) _owner.CharacterClass.ID is eCharacterClass.Necromancer && _owner.HasShadeModel)
                    _owner.Shade(false);

                _owner.Out.SendPlayerQuit(false);
                _owner.Quit(true);
                CraftingProgressMgr.FlushAndSaveInstance(_owner);
                _owner.SaveIntoDatabase();
                _onQuitTimerEnd();
            }
        }

        private int OnQuitTimerEnd()
        {
            _quitTimer = null;
            return 0;
        }

        protected QuitTimer _quitTimer;

        #endregion

        #region Player Linking Dead

        private LinkDeathTimer _linkDeathTimer;

        public bool IsLinkDeathTimerRunning => _linkDeathTimer?.IsAlive == true;

        public long LastPositionUpdatePacketReceivedTime
        {
            get => movementComponent.LastPositionUpdatePacketReceivedTime;
            set => movementComponent.LastPositionUpdatePacketReceivedTime = value;
        }

        public bool IsPositionUpdateFromPacketAllowed()
        {
            if (_linkDeathTimer == null)
                return true;

            LastPositionUpdatePacketReceivedTime = GameLoop.GameLoopTime;
            _linkDeathTimer.Stop();
            MoveTo(_linkDeathTimer.LocationAtLinkDeath);
            _linkDeathTimer = null;
            return false;
        }

        public void OnPositionUpdateFromPacket()
        {
            movementComponent.OnPositionUpdate();
        }

        public void OnHeadingPacketReceived()
        {
            movementComponent.OnHeadingUpdate();
        }

        public void OnLinkDeath()
        {
            CurrentSpeed = 0; // Stop player if he's running.
            LeaveHouse();

            if (_quitTimer != null)
            {
                _quitTimer.Stop();
                _quitTimer = null;
            }

            if (log.IsInfoEnabled)
                log.InfoFormat("Linkdead player {0}({1}) will quit in {2} seconds", Name, Client.Account.Name, SECONDS_TO_QUIT_ON_LINKDEATH);

            _linkDeathTimer = new(this); // Keep link-dead characters in game.
            TradeWindow?.CloseTrade();
            Group?.UpdateMember(this, false, false);

            // Hard LD only.
            if (Client.ClientState is GameClient.eClientState.Linkdead)
            {
                foreach (GamePlayer playerInRadius in GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    if (playerInRadius != this && GameServer.ServerRules.IsAllowedToUnderstand(this, playerInRadius))
                        playerInRadius.Out.SendMessage(LanguageMgr.GetTranslation(playerInRadius.Client.Account.Language, "GamePlayer.OnLinkdeath.Linkdead", Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }

                Notify(GamePlayerEvent.Linkdeath, this);
            }
        }

        /// <summary>
        /// Stop all timers, events and remove player from everywhere (group/guild/chat)
        /// </summary>
        protected virtual void CleanupOnDisconnect()
        {
            PlayerObjectCache.Clear();
            attackComponent.StopAttack();
            Stealth(false);

            if (IsOnHorse)
                IsOnHorse = false;

            GameEventMgr.RemoveAllHandlersForObject(m_inventory);

            if (_linkDeathTimer != null)
            {
                _linkDeathTimer.Stop();
                _linkDeathTimer = null;
            }

            if (CraftTimer != null)
            {
                CraftTimer.Stop();
                CraftTimer = null;
            }

            craftComponent?.StopCraft();

            if (QuestActionTimer != null)
            {
                QuestActionTimer.Stop();
                QuestActionTimer = null;
            }

            TradeWindow?.CloseTrade();
            Mission?.ExpireMission();
            TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY)?.RemovePlayer(this);

            if (ControlledBrain != null)
                CommandNpcRelease();

            SiegeWeapon?.ReleaseControl();

            if (InHouse)
                LeaveHouse();

            Group?.RemoveMember(this);
            TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY)?.RemoveBattlePlayer(this);
            m_guild?.RemoveOnlineMember(this);
            GroupMgr.RemovePlayerLooking(this);

            // Dinberg: this will eventually need to be changed so that it moves them to the location they TP'ed in.
            // DamienOphyr: Overwrite current position with Bind position in database, MoveTo() is inoperant
            if (CurrentRegion?.IsInstance == true)
            {
                DBCharacter.Region = BindRegion;
                DBCharacter.Xpos = BindXpos;
                DBCharacter.Ypos =  BindYpos;
                DBCharacter.Zpos = BindZpos;
                DBCharacter.Direction = BindHeading;
            }

            // Check for battleground caps.
            DbBattleground battleground = GameServer.KeepManager.GetBattleground(CurrentRegionID);
            if (battleground != null && (ePrivLevel) Client.Account.PrivLevel is ePrivLevel.Player)
            {
                if (Level > battleground.MaxLevel || RealmLevel >= battleground.MaxRealmLevel)
                    GameServer.KeepManager.ExitBattleground(this);
            }

            // Cancel all effects until saving of running effects is done.
            try
            {
                EffectHelper.SaveAllEffects(this);
                CancelAllConcentrationEffects();
                EffectList.CancelAll();
            }
            catch (Exception e)
            {
                log.ErrorFormat("Cannot cancel all effects - {0}", e);
            }

            if (Properties.ACTIVATE_TEMP_PROPERTIES_MANAGER_CHECKUP)
            {
                try
                {
                    List<string> registeredTempProp = null;

                    foreach (string property in TempProperties.GetAllProperties())
                    {
                        if (property == string.Empty)
                            continue;

                        int occurrences = 0;
                        registeredTempProp = Util.SplitCSV(Properties.TEMPPROPERTIES_TO_REGISTER).ToList();
                        occurrences = (from j in registeredTempProp where property.Contains(j) select j).Count();

                        if (occurrences == 0)
                            continue;

                        object propertyValue = TempProperties.GetProperty<object>(property);

                        if (propertyValue == null)
                            continue;

                        if (long.TryParse(propertyValue.ToString(), out long longresult))
                        {
                            if (Properties.ACTIVATE_TEMP_PROPERTIES_MANAGER_CHECKUP_DEBUG)
                                log.Debug("On Disconnection found and was saved: " + property + " with value: " + propertyValue.ToString() + " for player: " + Name);

                            TempPropertiesManager.TempPropContainerList.Add(new TempPropertiesManager.TempPropContainer(DBCharacter.ObjectId, property, propertyValue.ToString()));
                            TempProperties.RemoveProperty(property);
                        }
                        else if (Properties.ACTIVATE_TEMP_PROPERTIES_MANAGER_CHECKUP_DEBUG)
                            log.Debug("On Disconnection found but was not saved (not a long value): " + property + " with value: " + propertyValue.ToString() + " for player: " + Name);
                    }
                }
                catch (Exception e)
                {
                    log.Debug("Error in TempProproperties Manager when saving TempProp: " + e.ToString());
                }
            }

            if (log.IsDebugEnabled)
                log.DebugFormat("({0}) player.Delete()", Name);
        }

        /// <summary>
        /// This function saves the character and sends a message to all others
        /// that the player has quit the game!
        /// </summary>
        /// <param name="forced">true if Quit can not be prevented!</param>
        public virtual bool Quit(bool forced)
        {
            if (!forced)
            {
                if (!IsAlive)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Quit.CantQuitDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                if (Steed != null || IsOnHorse)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Quit.CantQuitMount"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                if (IsMoving && !Properties.DISABLE_QUIT_TIMER)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Quit.CantQuitStanding"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                if (CraftTimer != null && CraftTimer.IsAlive)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Quit.CantQuitCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (CurrentRegion.IsInstance)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Quit.CantQuitInInstance"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (Statistics != null)
                {
                    string stats = Statistics.GetStatisticsMessage();
                    if (stats != string.Empty)
                    {
                        Out.SendMessage(stats, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                }

                if (!IsSitting)
                    Sit(true);

                _quitTimer ??= new(this, OnQuitTimerEnd);
            }
            else
            {
                Notify(GamePlayerEvent.Quit, this);
                AuditMgr.AddAuditEntry(Client, AuditType.Character, AuditSubtype.CharacterLogout, "", Name);
                Delete();
            }

            return true;
        }

        public class LinkDeathTimer : ECSGameTimerWrapperBase
        {
            private GamePlayer _playerOwner;
            public GameLocation LocationAtLinkDeath { get; }

            public LinkDeathTimer(GameObject owner) : base(owner)
            {
                _playerOwner = owner as GamePlayer;
                LocationAtLinkDeath = new(string.Empty, _playerOwner.CurrentRegionID, _playerOwner.X, _playerOwner.Y, _playerOwner.Z, _playerOwner.Heading);
                Start(1000);
                OnTick(this);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (_playerOwner.ObjectState is eObjectState.Active)
                    _playerOwner.movementComponent.BroadcastPosition();

                if (!GameServiceUtils.ShouldTick(_playerOwner.Client.LinkDeathTime + SECONDS_TO_QUIT_ON_LINKDEATH * 1000))
                    return Interval;

                if (!_playerOwner.IsAlive)
                {
                    _playerOwner.Release(_playerOwner.ReleaseType, true);

                    if (log.IsInfoEnabled)
                        log.InfoFormat($"Linkdead player {_playerOwner.Name}({_playerOwner.Client.Account.Name}) was auto-released from death!");
                }

                try
                {
                    CraftingProgressMgr.FlushAndSaveInstance(_playerOwner);
                    _playerOwner.SaveIntoDatabase();
                }
                finally
                {
                    _playerOwner.Client.LinkDeathQuit();
                }

                return 0;
            }
        }

        #endregion

        #region Combat timer

        public override long LastAttackTickPvE
        {
            set
            {
                bool wasInCombat = InCombat;
                base.LastAttackTickPvE = value;

                if (!wasInCombat && InCombat)
                    Out.SendUpdateMaxSpeed();

                ResetInCombatTimer();
            }
        }

        public override long LastAttackTickPvP
        {
            set
            {
                bool wasInCombat = InCombat;
                base.LastAttackTickPvP = value;

                if (!wasInCombat && InCombat)
                    Out.SendUpdateMaxSpeed();

                ResetInCombatTimer();
            }
        }

        public override long LastAttackedByEnemyTickPvE
        {
            set
            {
                bool wasInCombat = InCombat;
                base.LastAttackedByEnemyTickPvE = value;

                if (!wasInCombat && InCombat)
                    Out.SendUpdateMaxSpeed();

                ResetInCombatTimer();
            }
        }

        public override long LastAttackedByEnemyTickPvP
        {
            set
            {
                bool wasInCombat = InCombat;
                base.LastAttackedByEnemyTickPvP = value;

                if (!wasInCombat && InCombat)
                    Out.SendUpdateMaxSpeed();

                ResetInCombatTimer();
            }
        }

        /// <summary>
        /// Expire Combat Timer Interval
        /// </summary>
        private static int COMBAT_TIMER_INTERVAL => 11000;

        /// <summary>
        /// Combat Timer
        /// </summary>
        private ECSGameTimer m_combatTimer;

        /// <summary>
        /// Reset and Restart Combat Timer
        /// </summary>
        protected virtual void ResetInCombatTimer()
        {
            m_combatTimer.Start(COMBAT_TIMER_INTERVAL);
        }
        #endregion

        #region release/bind/pray
        #region Binding
        /// <summary>
        /// Property that holds tick when the player bind last time
        /// </summary>
        public const string LAST_BIND_TICK = "LastBindTick";

        /// <summary>
        /// Min Allowed Interval Between Player Bind
        /// </summary>
        public virtual int BindAllowInterval { get { return 60000; }}

        /// <summary>
        /// Binds this player to the current location
        /// </summary>
        public void Bind()
        {
            if (CurrentRegion.IsInstance)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Bind.CantHere"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!IsAlive)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Bind.CantBindDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            //60 second rebind timer
            long lastBindTick = TempProperties.GetProperty<long>(LAST_BIND_TICK);
            long changeTime = CurrentRegion.Time - lastBindTick;
            if (Client.Account.PrivLevel <= (uint)ePrivLevel.Player && changeTime < BindAllowInterval)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Bind.MustWait", (1 + (BindAllowInterval - changeTime) / 1000)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            string description = string.Format("in {0}", this.GetBindSpotDescription());
            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Bind.LastBindPoint", description), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            bool bound = false;

            var bindarea = CurrentAreas.OfType<Area.BindArea>().FirstOrDefault(ar => GameServer.ServerRules.IsAllowedToBind(this, ar.BindPoint));
            if (bindarea != null)
            {
                bound = true;
                BindRegion = CurrentRegionID;
                BindHeading = Heading;
                BindXpos = X;
                BindYpos = Y;
                BindZpos = Z;
                if (DBCharacter != null)
                    GameServer.Database.SaveObject(DBCharacter);
            }

            //if we are not bound yet lets check if we are in a house where we can bind
            if (!bound && InHouse && CurrentHouse != null)
            {
                var house = CurrentHouse;
                bool canbindhere;
                try
                {
                    canbindhere = house.HousepointItems.Any(kv => ((GameObject)kv.Value.GameObject).GetName(0, false).EndsWith("bindstone", StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    canbindhere = false;
                }

                if (canbindhere)
                {
                    // make sure we can actually use the bindstone
                    if(!house.CanBindInHouse(this))
                    {
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Bind.CantHere"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }
                    else
                    {
                        bound = true;
                        double angle = house.Heading * ((Math.PI * 2) / 360); // angle*2pi/360;
                        int outsideX = (int)(house.X + (0 * Math.Cos(angle) + 500 * Math.Sin(angle)));
                        int outsideY = (int)(house.Y - (500 * Math.Cos(angle) - 0 * Math.Sin(angle)));
                        ushort outsideHeading = (ushort)((house.Heading < 180 ? house.Heading + 180 : house.Heading - 180) / 0.08789);
                        BindHouseRegion = CurrentRegionID;
                        BindHouseHeading = outsideHeading;
                        BindHouseXpos = outsideX;
                        BindHouseYpos = outsideY;
                        BindHouseZpos = house.Z;
                        if (DBCharacter != null)
                            GameServer.Database.SaveObject(DBCharacter);
                    }
                }
            }

            if (bound)
            {
                if (!IsMoving)
                {
                    eEmote bindEmote = eEmote.Bind;
                    switch (Realm)
                    {
                        case eRealm.Albion: bindEmote = eEmote.BindAlb; break;
                        case eRealm.Midgard: bindEmote = eEmote.BindMid; break;
                        case eRealm.Hibernia: bindEmote = eEmote.BindHib; break;
                    }

                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        if (player == null)
                            return;

                        if ((int)player.Client.Version < (int)GameClient.eClientVersion.Version187)
                            player.Out.SendEmoteAnimation(this, eEmote.Bind);
                        else
                            player.Out.SendEmoteAnimation(this, bindEmote);
                    }
                }

                TempProperties.SetProperty(LAST_BIND_TICK, CurrentRegion.Time);
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Bind.Bound"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Bind.CantHere"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
        #endregion

        #region Releasing
        /// <summary>
        /// tick when player is died
        /// </summary>
        public long DeathTick { get; private set; }

        /// <summary>
        /// choosed the player to release as soon as possible?
        /// </summary>
        protected bool m_automaticRelease = false;

        /// <summary>
        /// The release timer for this player
        /// </summary>
        protected ECSGameTimer m_releaseTimer;

        /// <summary>
        /// Stops release timer and closes timer window
        /// </summary>
        public void StopReleaseTimer()
        {
            Out.SendCloseTimerWindow();
            if (m_releaseTimer != null)
            {
                m_releaseTimer.Stop();
                m_releaseTimer = null;
            }
        }

        /// <summary>
        /// minimum time to wait before release is possible in seconds
        /// </summary>
        protected const int RELEASE_MINIMUM_WAIT = 10;

        /// <summary>
        /// max time before auto release in seconds
        /// </summary>
        protected const int RELEASE_TIME = 900;

        /// <summary>
        /// The property name that is set when relea
        /// sing to another region
        /// </summary>
        public const string RELEASING_PROPERTY = "releasing";

        /// <summary>
        /// The current release type
        /// </summary>
        protected eReleaseType m_releaseType = eReleaseType.Normal;

        /// <summary>
        /// Gets the player's current release type.
        /// </summary>
        public eReleaseType ReleaseType
        {
            get { return m_releaseType; }
        }

        /// <summary>
        /// Releases this player after death ... subtracts xp etc etc...
        /// </summary>
        /// <param name="releaseCommand">The type of release used for this player</param>
        /// <param name="forced">if true, will skip duel check and timer</param>
        public virtual void Release(eReleaseType releaseCommand, bool forced)
        {
            DbCoreCharacter character = DBCharacter;

            if (character == null)
                return;

            if (IsAlive)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.NotDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (releaseCommand is eReleaseType.House)
            {
                if (character.BindHouseRegion < 1)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.NoValidBindpoint"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    releaseCommand = eReleaseType.Bind;
                }
            }
            else if (releaseCommand is eReleaseType.Normal)
            {
                // Check for NF or battleground (RvR release).
                if (CurrentRegionID is 163)
                    releaseCommand = eReleaseType.NewFrontiers;
                else
                {
                    DbBattleground battleground = GameServer.KeepManager.GetBattleground(CurrentRegionID);

                    // Battlegrounds caps.
                    if (Properties.BG_RELEASE_TO_PORTAL_KEEP && battleground != null && Level <= battleground.MaxLevel && RealmLevel <= battleground.MaxRealmLevel)
                        releaseCommand = eReleaseType.Battleground;
                    else
                        releaseCommand = eReleaseType.Bind;
                }
            }

            if (!forced)
            {
                if (m_releaseType is eReleaseType.Duel)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.CantReleaseDuel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                m_releaseType = releaseCommand;
                long diff = DeathTick - GameLoop.GameLoopTime + RELEASE_MINIMUM_WAIT * 1000;

                if (diff >= 1000)
                {
                    if (m_automaticRelease)
                    {
                        m_automaticRelease = false;
                        m_releaseType = eReleaseType.Normal;
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.NoLongerReleaseAuto", diff / 1000), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    m_automaticRelease = true;

                    switch (releaseCommand)
                    {
                        default:
                        {
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.WillReleaseAuto", diff / 1000), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        case eReleaseType.City:
                        {
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.WillReleaseAutoCity", diff / 1000), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        case eReleaseType.NewFrontiers:
                        case eReleaseType.Battleground:
                        {
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.ReleaseToPortalKeep", diff / 1000), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        case eReleaseType.House:
                        {
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.ReleaseToHouse", diff / 1000), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                    }
                }
            }
            else
                m_releaseType = releaseCommand;

            int relX = 0;
            int relY = 0;
            int relZ = 0;
            ushort relRegion = 0;
            ushort relHeading = 0;

            switch (m_releaseType)
            {
                case eReleaseType.Duel:
                {
                    relRegion = (ushort) character.Region;
                    relX = character.Xpos;
                    relY = character.Ypos;
                    relZ = character.Zpos;
                    relHeading = 2048;
                    break;
                }
                case eReleaseType.House:
                {
                    relRegion = (ushort) BindHouseRegion;
                    relX = BindHouseXpos;
                    relY = BindHouseYpos;
                    relZ = BindHouseZpos;
                    relHeading = (ushort) BindHouseHeading;
                    break;
                }
                case eReleaseType.City:
                {
                    switch (Realm)
                    {
                        case eRealm.Albion:
                        {
                            relRegion = 10; // City of Camelot.
                            relX = 36240;
                            relY = 29695;
                            relZ = 7985;
                            relHeading = 4095;
                            break;
                        }
                        case eRealm.Midgard:
                        {
                            relRegion = 101; // Jordheim.
                            relX = 30094;
                            relY = 27589;
                            relZ = 8763;
                            relHeading = 3468;
                            break;
                        }
                        case eRealm.Hibernia:
                        {
                            relRegion = 201; // Tir Na Nog.
                            relX = 34149;
                            relY = 32063;
                            relZ = 8047;
                            relHeading = 1025;
                            break;
                        }
                        default:
                        {
                            ValidateAndGetBind(out relRegion, out relX, out relY, out relZ, out relHeading);
                            break;
                        }
                    }

                    break;
                }
                case eReleaseType.NewFrontiers:
                {
                    // `GetBorderKeepLocation` works with NF only.
                    if (GameServer.KeepManager.GetBorderKeepLocation((byte) Realm * 2 - 1, out relX, out relY, out relZ, out relHeading))
                    {
                        relRegion = CurrentRegion.ID;
                        break;
                    }

                    // Fall back to bind.
                    ValidateAndGetBind(out relRegion, out relX, out relY, out relZ, out relHeading);
                    break;
                }
                case eReleaseType.Battleground:
                {
                    bool foundPortalKeep = false;
                    DbBattleground battleground = GameServer.KeepManager.GetBattleground(CurrentRegionID);

                    if (battleground != null && Properties.BG_RELEASE_TO_PORTAL_KEEP)
                    {
                        // Use the friendly portal keep in the current region. There should be only one.
                        foreach (AbstractGameKeep keep in GameServer.KeepManager.GetKeepsOfRegion(CurrentRegionID))
                        {
                            if (!keep.IsPortalKeep || keep.OriginalRealm != Realm)
                                continue;

                            relRegion = keep.CurrentRegion.ID;
                            relX = keep.X;
                            relY = keep.Y;
                            relZ = keep.Z;
                            foundPortalKeep = true;
                            break;
                        }
                    }

                    // Fall back to bind.
                    if (!foundPortalKeep)
                        ValidateAndGetBind(out relRegion, out relX, out relY, out relZ, out relHeading);

                    break;
                }
                default:
                {
                    // Tutorial.
                    if (!Properties.DISABLE_TUTORIAL && BindRegion == 27)
                    {
                        switch (Realm)
                        {
                            case eRealm.Albion:
                            {
                                relRegion = 1; // Cotswold.
                                relX = 8192 + 553251;
                                relY = 8192 + 502936;
                                relZ = 2280;
                                break;
                            }
                            case eRealm.Midgard:
                            {
                                relRegion = 100; // Mularn.
                                relX = 8192 + 795621;
                                relY = 8192 + 719590;
                                relZ = 4680;
                                break;
                            }
                            case eRealm.Hibernia:
                            {
                                relRegion = 200; // Mag Mell.
                                relX = 8192 + 338652;
                                relY = 8192 + 482335;
                                relZ = 5200;
                                break;
                            }
                            default:
                            {
                                ValidateAndGetBind(out relRegion, out relX, out relY, out relZ, out relHeading);
                                break;
                            }
                        }
                    }
                    else
                        ValidateAndGetBind(out relRegion, out relX, out relY, out relZ, out relHeading);

                    break;
                }
            }

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.YouRelease"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);
            Out.SendCloseTimerWindow();

            if (m_releaseTimer != null)
            {
                m_releaseTimer.Stop();
                m_releaseTimer = null;
            }

            if (Level >= Properties.PVE_EXP_LOSS_LEVEL && !HCFlag)
            {
                // Actual lost exp, needed for 2nd stage deaths.
                long lostExp = Experience;
                long lastDeathExpLoss = TempProperties.GetProperty<long>(DEATH_EXP_LOSS_PROPERTY);
                TempProperties.RemoveProperty(DEATH_EXP_LOSS_PROPERTY);

                GainExperience(eXPSource.Other, -lastDeathExpLoss);
                lostExp -= Experience;

                if (lostExp > 0)
                {
                    // Find old gravestone of player and remove it.
                    if (character.HasGravestone)
                    {
                        Region reg = WorldMgr.GetRegion((ushort) character.GravestoneRegion);

                        if (reg != null)
                        {
                            GameGravestone oldGrave = reg.FindGraveStone(this);
                            oldGrave?.Delete();
                        }

                        character.HasGravestone = false;
                    }

                    GameGravestone gravestone = new GameGravestone(this, lostExp);
                    gravestone.AddToWorld();
                    character.GravestoneRegion = gravestone.CurrentRegionID;
                    character.HasGravestone = true;
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.GraveErected"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.ReturnToPray"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);
                }
            }

            if (Level >= Properties.PVE_CON_LOSS_LEVEL)
            {
                int deathConLoss = TempProperties.GetProperty<int>(DEATH_CONSTITUTION_LOSS_PROPERTY);

                if (deathConLoss > 0)
                {
                    TotalConstitutionLostAtDeath += deathConLoss;
                    Out.SendCharStatsUpdate();
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.LostConstitution"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);
                }
            }

            Health = MaxHealth;
            Endurance = MaxEndurance;
            Mana = MaxMana;
            StartPowerRegeneration();
            StartEnduranceRegeneration();
            LastDeathPvP = false;
            UpdatePlayerStatus();

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.SurroundingChange"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);

            int oldRegion = CurrentRegionID;

            // If region is 0, assume no valid release location was found.
            if (relRegion == 0)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Could not find valid release location for player {this})");
            }
            else
                MoveTo(relRegion, relX, relY, relZ, relHeading);

            Out.SendPlayerRevive(this);
            Out.SendUpdatePoints();

            if (oldRegion != CurrentRegionID)
                TempProperties.SetProperty(RELEASING_PROPERTY, true);
            else
            {
                Notify(GamePlayerEvent.Revive, this);
                Notify(GamePlayerEvent.Released, this);
            }

            TempProperties.RemoveProperty(DEATH_CONSTITUTION_LOSS_PROPERTY);

            void ValidateAndGetBind(out ushort relRegion, out int relX, out int relY, out int relZ, out ushort relHeading)
            {
                ValidateBind();
                relRegion = (ushort) BindRegion;
                relX = BindXpos;
                relY = BindYpos;
                relZ = BindZpos;
                relHeading = (ushort) BindHeading;
            }
        }

        /// <summary>
        /// helper state var for different release phases
        /// </summary>
        private byte m_releasePhase = 0;

        /// <summary>
        /// callback every second to control realtime release
        /// </summary>
        /// <param name="callingTimer"></param>
        /// <returns></returns>
        protected virtual int ReleaseTimerCallback(ECSGameTimer callingTimer)
        {
            if (IsAlive)
                return 0;
            long diffToRelease = GameLoop.GameLoopTime - DeathTick;
            if (m_automaticRelease && diffToRelease > RELEASE_MINIMUM_WAIT * 1000)
            {
                Release(m_releaseType, true);
                return 0;
            }
            diffToRelease = (RELEASE_TIME * 1000 - diffToRelease) / 1000;
            if (diffToRelease <= 0)
            {
                Release(m_releaseType, true);
                return 0;
            }
            if (m_releasePhase <= 1 && diffToRelease <= 10 && diffToRelease >= 8)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.WillReleaseIn", 10), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                m_releasePhase = 2;
            }
            if (m_releasePhase == 0 && diffToRelease <= 30 && diffToRelease >= 28)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Release.WillReleaseIn", 30), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                m_releasePhase = 1;
            }
            return 1000;
        }

        /// <summary>
        /// The current death type
        /// </summary>
        protected eDeathType m_deathtype;

        /// <summary>
        /// Gets the player's current death type.
        /// </summary>
        public eDeathType DeathType
        {
            get { return m_deathtype; }
            set { m_deathtype = value; }
        }
        /// <summary>
        /// Called when player revive
        /// </summary>
        public virtual void OnRevive(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;
            bool applyRezSick = true;

            // Used by spells like Perfect Recovery
            if (TempProperties.GetAllProperties().Contains(RESURRECT_REZ_SICK_EFFECTIVENESS) && TempProperties.GetProperty<double>(RESURRECT_REZ_SICK_EFFECTIVENESS) == 0)
            {
                applyRezSick = false;
                TempProperties.RemoveProperty(RESURRECT_REZ_SICK_EFFECTIVENESS);
            }
            else if (player.Level < ServerProperties.Properties.RESS_SICKNESS_LEVEL)
            {
                applyRezSick = false;
            }

            if (player.IsUnderwater && player.CanBreathUnderWater == false)
                player.UpdateWaterBreathState(eWaterBreath.Holding);
            //We need two different sickness spells because RvR sickness is not curable by Healer NPC -Unty
            if (applyRezSick)
                switch (DeathType)
                {
                    case eDeathType.RvR:
                        SpellLine rvrsick = SkillBase.GetSpellLine(GlobalSpellsLines.Realm_Spells);
                        if (rvrsick == null) return;
                        Spell rvrillness = SkillBase.FindSpell(8181, rvrsick);
                        //player.CastSpell(rvrillness, rvrsick);
                        CastSpell(rvrillness, rvrsick);
                        break;
                    case eDeathType.PvP: //PvP sickness is the same as PvE sickness - Curable
                    case eDeathType.PvE:
                        SpellLine pvesick = SkillBase.GetSpellLine(GlobalSpellsLines.Realm_Spells);
                        if (pvesick == null) return;
                        Spell pveillness = SkillBase.FindSpell(2435, pvesick);
                        //player.CastSpell(pveillness, pvesick);
                        CastSpell(pveillness, pvesick);
                        break;
                }

            GameEventMgr.RemoveHandler(this, GamePlayerEvent.Revive, new DOLEventHandler(OnRevive));
            m_deathtype = eDeathType.None;
            LastDeathPvP = false;
            UpdatePlayerStatus();
            Out.SendPlayerRevive(this);
        }

        /// <summary>
        /// Property that saves experience lost on last death
        /// </summary>
        public const string DEATH_EXP_LOSS_PROPERTY = "death_exp_loss";
        /// <summary>
        /// Property that saves condition lost on last death
        /// </summary>
        public const string DEATH_CONSTITUTION_LOSS_PROPERTY = "death_con_loss";
        #endregion

        #region Praying
        /// <summary>
        /// The timer that will be started when the player wants to pray
        /// </summary>
        private ECSGameTimer m_prayAction;

        /// <summary>
        /// Gets the praying-state of this living
        /// </summary>
        public virtual bool IsPraying => m_prayAction?.IsAlive == true;

        /// <summary>
        /// Prays on a gravestone for XP!
        /// </summary>
        public virtual void Pray()
        {
            string cantPrayMessage = null;
            GameGravestone gravestone = TargetObject as GameGravestone;

            if (!IsAlive)
                cantPrayMessage = "GamePlayer.Pray.CantPrayNow";
            else if (IsRiding)
                cantPrayMessage = "GamePlayer.Pray.CantPrayRiding";
            else if (gravestone == null)
                cantPrayMessage = "GamePlayer.Pray.NeedTarget";
            else if (!gravestone.InternalID.Equals(InternalID))
                cantPrayMessage = "GamePlayer.Pray.SelectGrave";
            else if (!IsWithinRadius(gravestone, 2000))
                cantPrayMessage = "GamePlayer.Pray.MustGetCloser";
            else if (IsMoving)
                cantPrayMessage = "GamePlayer.Pray.MustStandingStill";
            else if (IsPraying)
                cantPrayMessage = "GamePlayer.Pray.AlreadyPraying";

            if (cantPrayMessage != null)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, cantPrayMessage), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            m_prayAction = new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(_ =>
            {
                if (gravestone.XPValue > 0)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Pray.GainBack"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    GainExperience(eXPSource.Praying, gravestone.XPValue);
                }

                gravestone.XPValue = 0;
                gravestone.Delete();
                m_prayAction = null;
                return 0;
            }), 5000);

            Sit(true);
            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Pray.Begin"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player == null)
                    continue;

                player.Out.SendEmoteAnimation(this, eEmote.Pray);
            }
        }

        /// <summary>
        /// Stop praying; used when player changes target
        /// </summary>
        public void PrayTimerStop()
        {
            if (!IsPraying)
                return;
            m_prayAction.Stop();
            m_prayAction = null;
        }
        #endregion

        #endregion

        #region Name/LastName/GuildName/Model

        /// <summary>
        /// The lastname of this player
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual string LastName
        {
            get => DBCharacter != null ? DBCharacter.LastName : string.Empty;
            set
            {
                if (DBCharacter == null)
                    return;

                DBCharacter.LastName = value;

                // Update last name for all players if client is playing.
                if (ObjectState == eObjectState.Active)
                {
                    Out.SendUpdatePlayer();

                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        if (player != this)
                        {
                            player.Out.SendObjectRemove(this);
                            player.Out.SendPlayerCreate(this);
                            player.Out.SendLivingEquipmentUpdate(this);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the guildname of this player
        /// (delegate to PlayerCharacter)
        /// </summary>
        public override string GuildName
        {
            get
            {
                if (m_guild == null)
                    return string.Empty;

                return m_guild.Name;
            }
            set
            { }
        }

        /// <summary>
        /// Gets or sets the name of the player
        /// (delegate to PlayerCharacter)
        /// </summary>
        public override string Name
        {
            get => DBCharacter != null ? DBCharacter.Name : base.Name;
            set
            {
                string oldname = base.Name;
                base.Name = value;

                if (DBCharacter != null)
                    DBCharacter.Name = value;

                if (oldname != value)
                {
                    // Update name for all players if client is playing.
                    if (ObjectState == eObjectState.Active)
                    {
                        Out.SendUpdatePlayer();

                        if (Group != null)
                            Out.SendGroupWindowUpdate();

                        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            if (player != this)
                            {
                                player.Out.SendObjectRemove(this);
                                player.Out.SendPlayerCreate(this);
                                player.Out.SendLivingEquipmentUpdate(this);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets or gets the model of the player. If the player is
        /// active in the world, the modelchange will be visible
        /// (delegate to PlayerCharacter)
        /// </summary>
        /// <remarks>
        /// The model of a GamePlayer is a 16-bit unsigned integer.
        /// The leftmost 3 bits are related to hair color.
        /// The next 2 bits are for the size: 01 = short, 10 = average, 11 = tall (00 appears to be average as well)
        /// The remaining 11 bits are for the model (see monsters.csv in gamedata.mpk)
        /// </remarks>
        public override ushort Model
        {
            get
            {
                return base.Model;
            }
            set
            {
                if (base.Model != value)
                {
                    base.Model = value;

                    // Only GM's can persist model changes - Tolakram
                    if (Client.Account.PrivLevel > (int)ePrivLevel.Player && DBCharacter != null && DBCharacter.CurrentModel != base.Model)
                    {
                        DBCharacter.CurrentModel = base.Model;
                    }

                    if (ObjectState == eObjectState.Active)
                    {
                        Notify(GamePlayerEvent.ModelChanged, this);

                        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            if (player == null) continue;
                            player.Out.SendModelChange(this, Model);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Male or Female (from DBCharacter)
        /// Note: DB Gender is 0=male, 1=female while enum is 0=neutral, 1=male, 2=female
        /// </summary>
        public override eGender Gender
        {
            get
            {
                if (DBCharacter.Gender == 0)
                {
                    return eGender.Male;
                }

                return eGender.Female;
            }
            set
            {
            }
        }

        public eSize Size
        {
            get
            {
                ushort size = (ushort)( Model & (ushort)eSize.Tall );

                switch ( size )
                {
                    case 0x800: return eSize.Short;
                    case 0x1800: return eSize.Tall;
                    default: return eSize.Average;
                }
            }

            set
            {
                if ( value != Size )
                {
                    ushort modelID = (ushort)( Model & 0x7FF );

                    Model = (ushort)( modelID | (ushort)value );
                }
            }
        }

        #endregion

        #region Stats

        /// <summary>
        /// Holds if the player can gain a FreeLevel
        /// </summary>
        public virtual byte FreeLevelState
        {
            get
            {
                int freelevel_days = 7;
                switch (Realm)
                {
                    case eRealm.Albion:
                        if (ServerProperties.Properties.FREELEVEL_DAYS_ALBION == -1)
                            return 1;
                        else
                            freelevel_days = ServerProperties.Properties.FREELEVEL_DAYS_ALBION;
                        break;
                    case eRealm.Midgard:
                        if (ServerProperties.Properties.FREELEVEL_DAYS_MIDGARD == -1)
                            return 1;
                        else
                            freelevel_days = ServerProperties.Properties.FREELEVEL_DAYS_MIDGARD;
                        break;
                    case eRealm.Hibernia:
                        if (ServerProperties.Properties.FREELEVEL_DAYS_HIBERNIA == -1)
                            return 1;
                        else
                            freelevel_days = ServerProperties.Properties.FREELEVEL_DAYS_HIBERNIA;
                        break;
                }

                //flag 1 = above level, 2 = elligable, 3= time until, 4 = level and time until, 5 = level until
                if (Level >= 48)
                    return 1;

                TimeSpan t = new TimeSpan((long)(DateTime.Now.Ticks - LastFreeLeveled.Ticks));
                if (t.Days >= freelevel_days)
                {
                    if (Level >= LastFreeLevel + 2)
                        return 2;
                    else return 5;
                }
                else
                {
                    if (Level >= LastFreeLevel + 2)
                        return 3;
                    else return 4;
                }
            }
        }

        /// <summary>
        /// Gets/sets the player efficacy percent
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual int TotalConstitutionLostAtDeath
        {
            get { return DBCharacter != null ? DBCharacter.ConLostAtDeath : 0; }
            set { if (DBCharacter != null) DBCharacter.ConLostAtDeath = value; }
        }

        /// <summary>
        /// Change a stat value
        /// (delegate to PlayerCharacter)
        /// </summary>
        /// <param name="stat">The stat to change</param>
        /// <param name="val">The new value</param>
        public override void ChangeBaseStat(eStat stat, short val)
        {
            int oldstat = GetBaseStat(stat);
            base.ChangeBaseStat(stat, val);
            int newstat = GetBaseStat(stat);
            DbCoreCharacter character = DBCharacter; // to call it only once, if in future there will be some special code to get the character
            // Graveen: always positive and not null. This allows /player stats to substract values safely
            if (newstat < 1) newstat = 1;
            if (character != null && oldstat != newstat)
            {
                switch (stat)
                {
                    case eStat.STR: character.Strength = newstat; break;
                    case eStat.DEX: character.Dexterity = newstat; break;
                    case eStat.CON: character.Constitution = newstat; break;
                    case eStat.QUI: character.Quickness = newstat; break;
                    case eStat.INT: character.Intelligence = newstat; break;
                    case eStat.PIE: character.Piety = newstat; break;
                    case eStat.EMP: character.Empathy = newstat; break;
                    case eStat.CHR: character.Charisma = newstat; break;
                }
            }
        }

        /// <summary>
        /// Gets player's constitution
        /// </summary>
        public int Constitution
        {
            get { return GetModified(eProperty.Constitution); }
        }

        /// <summary>
        /// Gets player's dexterity
        /// </summary>
        public int Dexterity
        {
            get { return GetModified(eProperty.Dexterity); }
        }

        /// <summary>
        /// Gets player's strength
        /// </summary>
        public int Strength
        {
            get { return GetModified(eProperty.Strength); }
        }

        /// <summary>
        /// Gets player's quickness
        /// </summary>
        public int Quickness
        {
            get { return GetModified(eProperty.Quickness); }
        }

        /// <summary>
        /// Gets player's intelligence
        /// </summary>
        public int Intelligence
        {
            get { return GetModified(eProperty.Intelligence); }
        }

        /// <summary>
        /// Gets player's piety
        /// </summary>
        public int Piety
        {
            get { return GetModified(eProperty.Piety); }
        }

        /// <summary>
        /// Gets player's empathy
        /// </summary>
        public int Empathy
        {
            get { return GetModified(eProperty.Empathy); }
        }

        /// <summary>
        /// Gets player's charisma
        /// </summary>
        public int Charisma
        {
            get { return GetModified(eProperty.Charisma); }
        }

        protected PlayerStatistics m_statistics = null;

        /// <summary>
        /// Get the statistics for this player
        /// </summary>
        public virtual PlayerStatistics Statistics
        {
            get { return m_statistics; }
        }

        /// <summary>
        /// Create played statistics for this player
        /// </summary>
        public virtual void CreateStatistics()
        {
            m_statistics = new PlayerStatistics(this);
        }

        /// <summary>
        /// Formats this players statistics.
        /// </summary>
        /// <returns>List of strings.</returns>
        public virtual IList<string> FormatStatistics()
        {
            return GameServer.ServerRules.FormatPlayerStatistics(this);
        }

        #endregion

        #region Health/Mana/Endurance/Regeneration

        private int GetHealthAndPowerRegenerationInterval()
        {
            // From Uthgard.
            // 6s normal, 3s sitting, 14s combat, 10s sitting combat.
            // There is no elegant formula for this. Sitting + in-combat might have been caused by rounding errors on Live.
            bool inCombat = InCombat;
            bool isSitting = IsSitting;
            int interval = 6 - (isSitting ? 3 : 0) + (inCombat ? 8 : 0) - (isSitting && inCombat ? 1 : 0);
            return interval * 1000;
        }

        protected override int GetHealthRegenerationInterval()
        {
            return GetHealthAndPowerRegenerationInterval();
        }

        protected override int GetPowerRegenerationInterval()
        {
            return GetHealthAndPowerRegenerationInterval();
        }

        protected override int GetEnduranceRegenerationInterval()
        {
            return 1000;
        }

        public override void StartPowerRegeneration()
        {
            if (m_health == 0 || ObjectState is not eObjectState.Active)
                return;

            if (m_powerRegenerationTimer == null)
                m_powerRegenerationTimer = new(this, new ECSGameTimer.ECSTimerCallback(PowerRegenerationTimerCallback));
            else if (m_powerRegenerationTimer.IsAlive)
                return;

            m_powerRegenerationTimer.Start(GetPowerRegenerationInterval());
        }

        public override void StartEnduranceRegeneration()
        {
            if (m_health == 0 || ObjectState is not eObjectState.Active)
                return;

            if (m_enduRegenerationTimer == null)
                m_enduRegenerationTimer = new(this, new ECSGameTimer.ECSTimerCallback(EnduranceRegenerationTimerCallback));
            else if (m_enduRegenerationTimer.IsAlive)
                return;

            m_enduRegenerationTimer.Start(GetEnduranceRegenerationInterval());
        }

        protected override int HealthRegenerationTimerCallback(ECSGameTimer callingTimer)
        {
            int maxHealth = MaxHealth;

            if (Health >= maxHealth)
            {
                Health = maxHealth;

                lock (XpGainersLock)
                {
                    m_xpGainers.Clear();
                }

                return 0;
            }

            ChangeHealth(this, eHealthChangeType.Regenerate, GetModified(eProperty.HealthRegenerationAmount));
            return GetHealthRegenerationInterval();
        }

        protected override int EnduranceRegenerationTimerCallback(ECSGameTimer selfRegenerationTimer)
        {
            int maxEndurance = MaxEndurance;
            bool sprinting = IsSprinting;

            if (Endurance >= maxEndurance)
            {
                Endurance = maxEndurance;

                if (!sprinting)
                    return 0;
            }

            int regen = GetModified(eProperty.EnduranceRegenerationAmount);
            int endChant = GetModified(eProperty.FatigueConsumption);
            ECSGameEffect charge = EffectListService.GetEffectOnTarget(this, eEffect.Charge);
            int longWind = 5;

            if (sprinting && IsMoving)
            {
                if (charge is null)
                {
                    AtlasOF_LongWindAbility raLongWind = GetAbility<AtlasOF_LongWindAbility>();

                    if (raLongWind != null)
                        longWind -= raLongWind.GetAmountForLevel(CalculateSkillLevel(raLongWind)) * 5 / 100;

                    regen -= longWind;

                    if (endChant > 1)
                        regen = (int) Math.Ceiling(regen * endChant * 0.01);

                    if (Endurance + regen > maxEndurance - longWind)
                        regen -= Endurance + regen - (maxEndurance - longWind);
                }
            }

            if (regen != 0)
                ChangeEndurance(this, eEnduranceChangeType.Regenerate, regen);

            if (sprinting)
            {
                if (Endurance - 5 <= 0)
                    Sprint(false);
            }

            return GetEnduranceRegenerationInterval();
        }

        /// <summary>
        /// Gets/sets the object health
        /// </summary>
        public override int Health
        {
            get => DBCharacter != null ? DBCharacter.Health : base.Health;
            set
            {
                int oldPercent = HealthPercent;
                base.Health = value;

                if (DBCharacter != null)
                    DBCharacter.Health = base.Health; // Base clamps between 0 and max value.

                if (oldPercent != HealthPercent)
                {
                    Group?.UpdateMember(this, false, false);
                    UpdatePlayerStatus();
                }
            }
        }

        /// <summary>
        /// Calculates the maximum health for a specific playerlevel and constitution
        /// </summary>
        /// <param name="level">The level of the player</param>
        /// <param name="constitution">The constitution of the player</param>
        /// <returns></returns>
        public virtual int CalculateMaxHealth(int level, int constitution)
        {
            constitution -= 50;
            if (constitution < 0) constitution *= 2;

            // hp1 : from level
            // hp2 : from constitution
            // hp3 : from champions level
            // hp4 : from artifacts such Spear of Kings charge
            int hp1 = CharacterClass.BaseHP * level;
            int hp2 = hp1 * constitution / 10000;
            int hp3 = 0;
            if (ChampionLevel >= 1)
                hp3 = ServerProperties.Properties.HPS_PER_CHAMPIONLEVEL * ChampionLevel;
            double hp4 = 20 + hp1 / 50 + hp2 + hp3;
            if (GetModified(eProperty.ExtraHP) > 0)
                hp4 += Math.Round(hp4 * (double)GetModified(eProperty.ExtraHP) / 100);

            return Math.Max(1, (int)hp4);
        }

        public override byte HealthPercentGroupWindow => CharacterClass.HealthPercentGroupWindow;

        /// <summary>
        /// Calculate max mana for this player based on level and mana stat level
        /// </summary>
        public virtual int CalculateMaxMana(int level, int manaStat)
        {
            int maxPower = 0;

            // Special handling for Vampiirs:
            /* There is no stat that affects the Vampiir's power pool or the damage done by its power based spells.
             * The Vampiir is not a focus based class like, say, an Enchanter.
             * The Vampiir is a lot more cut and dried than the typical casting class.
             * EDIT, 12/13/04 - I was told today that this answer is not entirely accurate.
             * While there is no stat that affects the damage dealt (in the way that intelligence or piety affects how much damage a more traditional caster can do),
             * the Vampiir's power pool capacity is intended to be increased as the Vampiir's strength increases.
             *
             * This means that strength ONLY affects a Vampiir's mana pool
             * 
             * http://www.camelotherald.com/more/1913.shtml
             * Strength affects the amount of damage done by spells in all of the Vampiir's spell lines.
             * The amount of said affecting was recently increased slightly (fixing a bug), and that minor increase will go live in 1.74 next week.
             * 
             * Strength ALSO affects the size of the power pool for a Vampiir sort of.
             * Your INNATE strength (the number of attribute points your character has for strength) has no effect at all.
             * Extra points added through ITEMS, however, does increase the size of your power pool.
             */

            // Since 1.62, Augmented Acuity is supposed to increase Nightshade's power pool, but without increasing their actual stat.
            // This isn't implemented currently.

            if (CharacterClass.ManaStat is not eStat.UNDEFINED || (eCharacterClass) CharacterClass.ID is eCharacterClass.Vampiir)
                maxPower = Math.Max(5, level * 5 + (manaStat - 50));
            else if (Champion && ChampionLevel > 0)
                maxPower = 100; // This is a guess, need feedback.

            return Math.Max(0, maxPower);
        }

        public override int Mana
        {
            get => DBCharacter != null ? DBCharacter.Mana : base.Mana;
            set
            {
                int oldPercent = ManaPercent;
                base.Mana = value;

                if (DBCharacter != null)
                    DBCharacter.Mana = base.Mana; // Base clamps between 0 and max value.

                if (oldPercent != ManaPercent)
                {
                    Group?.UpdateMember(this, false, false);
                    UpdatePlayerStatus();
                }
            }
        }

        public override int MaxMana => base.MaxMana;

        public override int Endurance
        {
            get => DBCharacter != null ? DBCharacter.Endurance : base.Endurance;
            set
            {
                int oldPercent = EndurancePercent;
                base.Endurance = value;

                if (DBCharacter != null)
                    DBCharacter.Endurance = base.Endurance; // Base clamps between 0 and max value.

                if (oldPercent != EndurancePercent)
                {
                    Group?.UpdateMember(this, false, false);
                    UpdatePlayerStatus();
                }
            }
        }

        public override int MaxEndurance => base.MaxEndurance;
        public override int Concentration => MaxConcentration - effectListComponent.UsedConcentration;
        public override int MaxConcentration => GetModified(eProperty.MaxConcentration);

        #region Calculate Fall Damage

        /// <summary>
        /// Calculates fall damage taking fall damage reduction bonuses into account
        /// </summary>
        /// <returns></returns>
        public virtual double CalcFallDamage(int fallDamagePercent)
        {
            if (fallDamagePercent <= 0)
                return 0;

            int safeFallLevel = GetAbilityLevel(Abilities.SafeFall);
            int mythSafeFall = GetModified(eProperty.MythicalSafeFall);

            if (mythSafeFall > 0 & mythSafeFall < fallDamagePercent)
            {
                Client.Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "PlayerPositionUpdateHandler.MythSafeFall"), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                fallDamagePercent = mythSafeFall;
            }
            if (safeFallLevel > 0 & mythSafeFall == 0)
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "PlayerPositionUpdateHandler.SafeFall"), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);

            Endurance -= MaxEndurance * fallDamagePercent / 100;
            double damage = (0.01 * fallDamagePercent * (MaxHealth - 1));

            // [Freya] Nidel: CloudSong falling damage reduction
            GameSpellEffect cloudSongFall = SpellHandler.FindEffectOnTarget(this, "CloudsongFall");
            if (cloudSongFall != null)
                damage -= (damage * cloudSongFall.Spell.Value) * 0.01;

            //Mattress: SafeFall property for Mythirians, the value of the MythicalSafeFall property represents the percent damage taken in a fall.
            if (mythSafeFall != 0 && damage > mythSafeFall)
                damage = ((MaxHealth - 1) * (mythSafeFall * 0.01));

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "PlayerPositionUpdateHandler.FallingDamage"), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "PlayerPositionUpdateHandler.FallPercent", fallDamagePercent), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
            Out.SendMessage("You lose endurance.", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
            TakeDamage(null, eDamageType.Falling, (int)damage, 0);

            //Update the player's health to all other players around
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                Out.SendCombatAnimation(null, Client.Player, 0, 0, 0, 0, 0, HealthPercent);

            return damage;
        }

        #endregion

        #endregion

        #region Class/Race

        /// <summary>
        /// Gets/sets the player's race name
        /// </summary>
        public virtual string RaceName
        {
            get { return this.RaceToTranslatedName(Race, Gender); }
        }

        /// <summary>
        /// Gets or sets this player's race id
        /// (delegate to DBCharacter)
        /// </summary>
        public override short Race
        {
            get { return (short)(DBCharacter != null ? DBCharacter.Race : 0); }
            set { if (DBCharacter != null) DBCharacter.Race = value; }
        }

        /// <summary>
        /// Players class
        /// </summary>
        protected ICharacterClass m_characterClass;

        /// <summary>
        /// Gets the player's character class
        /// </summary>
        public virtual ICharacterClass CharacterClass
        {
            get { return m_characterClass; }
        }

        /// <summary>
        /// Set the character class to a specific one
        /// </summary>
        /// <param name="id">id of the character class</param>
        /// <returns>success</returns>
        public virtual bool SetCharacterClass(int id)
        {
            ICharacterClass cl = ScriptMgr.FindCharacterClass(id);

            if (cl == null)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("No CharacterClass with ID {0} found", id);
                return false;
            }

            m_characterClass = cl;
            m_characterClass.Init(this);

            DBCharacter.Class = m_characterClass.ID;

            if (Group != null)
            {
                Group.UpdateMember(this, false, true);
            }
            return true;
        }

        /// <summary>
        /// Hold all player face custom attibutes
        /// </summary>
        protected byte[] m_customFaceAttributes = new byte[(int)eCharFacePart._Last + 1];

        /// <summary>
        /// Get the character face attribute you want
        /// </summary>
        /// <param name="part">face part</param>
        /// <returns>attribute</returns>
        public byte GetFaceAttribute(eCharFacePart part)
        {
            return m_customFaceAttributes[(int)part];
        }

        #endregion

        #region Spells/Skills/Abilities/Effects

        /// <summary>
        /// Holds the player choosen list of Realm Abilities.
        /// </summary>
        protected readonly ReaderWriterList<RealmAbility> m_realmAbilities = new ReaderWriterList<RealmAbility>();

        /// <summary>
        /// Holds the player specializable skills and style lines
        /// (KeyName -> Specialization)
        /// </summary>
        protected readonly Dictionary<string, Specialization> m_specialization = new Dictionary<string, Specialization>();
        protected readonly Lock _specializationLock = new();

        /// <summary>
        /// Holds the Spell lines the player can use
        /// </summary>
        protected readonly List<SpellLine> m_spellLines = new List<SpellLine>();

        /// <summary>
        /// Object to use when locking the SpellLines list
        /// </summary>
        protected readonly Lock _spellLinesListLock = new();

        /// <summary>
        /// Temporary Stats Boni
        /// </summary>
        protected readonly int[] m_statBonus = new int[8];

        /// <summary>
        /// Temporary Stats Boni in percent
        /// </summary>
        protected readonly int[] m_statBonusPercent = new int[8];

        /// <summary>
        /// Gets/Sets amount of full skill respecs
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual int RespecAmountAllSkill
        {
            get { return DBCharacter != null ? DBCharacter.RespecAmountAllSkill : 0; }
            set { if (DBCharacter != null) DBCharacter.RespecAmountAllSkill = value; }
        }

        /// <summary>
        /// Gets/Sets amount of single-line respecs
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual int RespecAmountSingleSkill
        {
            get { return DBCharacter != null ? DBCharacter.RespecAmountSingleSkill : 0; }
            set { if (DBCharacter != null) DBCharacter.RespecAmountSingleSkill = value; }
        }

        /// <summary>
        /// Gets/Sets amount of realm skill respecs
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual int RespecAmountRealmSkill
        {
            get { return DBCharacter != null ? DBCharacter.RespecAmountRealmSkill : 0; }
            set { if (DBCharacter != null) DBCharacter.RespecAmountRealmSkill = value; }
        }

        /// <summary>
        /// Gets/Sets amount of DOL respecs
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual int RespecAmountDOL
        {
            get { return DBCharacter != null ? DBCharacter.RespecAmountDOL : 0; }
            set { if (DBCharacter != null) DBCharacter.RespecAmountDOL = value; }
        }

        /// <summary>
        /// Gets/Sets level respec usage flag
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual bool IsLevelRespecUsed
        {
            get { return DBCharacter != null ? DBCharacter.IsLevelRespecUsed : true; }
            set { if (DBCharacter != null) DBCharacter.IsLevelRespecUsed = value; }
        }


        protected static readonly int[] m_numRespecsCanBuyOnLevel =
        {
            1,1,1,1,1, //1-5
            2,2,2,2,2,2,2, //6-12
            3,3,3,3, //13-16
            4,4,4,4,4,4, //17-22
            5,5,5,5,5, //23-27
            6,6,6,6,6,6, //28-33
            7,7,7,7,7, //34-38
            8,8,8,8,8,8, //39-44
            9,9,9,9,9, //45-49
            10 //50
        };


        /// <summary>
        /// Can this player buy a respec?
        /// </summary>
        public virtual bool CanBuyRespec
        {
            get
            {
                return (RespecBought < m_numRespecsCanBuyOnLevel[Level - 1]);
            }
        }

        /// <summary>
        /// Gets/Sets amount of bought respecs
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual int RespecBought
        {
            get { return DBCharacter != null ? DBCharacter.RespecBought : 0; }
            set { if (DBCharacter != null) DBCharacter.RespecBought = value; }
        }


        protected static readonly int[] m_respecCost =
        {
            1,2,3, //13
            2,5,9, //14
            3,9,17, //15
            6,16,30, //16
            10,26,48,75, //17
            16,40,72,112, //18
            22,56,102,159, //19
            31,78,140,218, //20
            41,103,187,291, //21
            54,135,243,378, //22
            68,171,308,480,652, //23
            85,214,385,600,814, //24
            105,263,474,738,1001, //25
            128,320,576,896,1216, //26
            153,383,690,1074,1458, //27
            182,455,820,1275,1731,2278, //28
            214,535,964,1500,2036,2679, //29
            250,625,1125,1750,2375,3125, //30
            289,723,1302,2025,2749,3617, //31
            332,831,1497,2329,3161,4159, //32
            380,950,1710,2661,3612,4752, //33
            432,1080,1944,3024,4104,5400,6696, //34
            488,1220,2197,3417,4638,6103,7568, //35
            549,1373,2471,3844,5217,6865,8513, //36
            615,1537,2767,4305,5843,7688,9533, //37
            686,1715,3087,4802,6517,8575,10633, //38
            762,1905,3429,5335,7240,9526,11813,14099, //39
            843,2109,3796,5906,8015,10546,13078,15609, //40
            930,2327,4189,6516,8844,11637,14430,17222, //41
            1024,2560,4608,7168,9728,1280,15872,18944, //42
            1123,2807,5053,7861,10668,14037,17406,20776, //43
            1228,3070,5527,8597,11668,15353,19037,22722, //44
            1339,3349,6029,9378,12725,16748,20767,24787,28806, //45
            1458,3645,6561,10206,13851,18225,22599,26973,31347, //46
            1582,3957,7123,11080,15037,19786,24535,29283,34032, //47
            1714,4286,7716,12003,16290,21434,26578,31722,36867, //48
            1853,4634,8341,12976,17610,23171,28732,34293,39854, //49
            2000,5000,9000,14000,19000,25000,31000,37000,43000,50000 //50
        };


        /// <summary>
        /// How much does this player have to pay for a respec?
        /// </summary>
        public virtual long RespecCost
        {
            get
            {
                if (Level <= 12) //1-12
                    return m_respecCost[0];

                if (CanBuyRespec)
                {
                    int t = 0;
                    for (int i = 13; i < Level; i++)
                    {
                        t += m_numRespecsCanBuyOnLevel[i - 1];
                    }

                    return m_respecCost[t + RespecBought];
                }

                return -1;
            }
        }

        /// <summary>
        /// give player a new Specialization or improve existing one
        /// </summary>
        /// <param name="skill"></param>
        public void AddSpecialization(Specialization skill)
        {
            AddSpecialization(skill, true);
        }

        /// <summary>
        /// give player a new Specialization or improve existing one
        /// </summary>
        /// <param name="skill"></param>
        protected virtual void AddSpecialization(Specialization skill, bool notify)
        {
            if (skill == null)
                return;

            lock (_specializationLock)
            {
                if (m_specialization.TryGetValue(skill.KeyName, out Specialization specialization))
                {
                    specialization.Level = skill.Level;
                    return;
                }

                m_specialization.Add(skill.KeyName, skill);

                if (notify)
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.AddSpecialisation.YouLearn", skill.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        /// <summary>
        /// Removes the existing specialization from the player
        /// </summary>
        /// <param name="specKeyName">The spec keyname to remove</param>
        /// <returns>true if removed</returns>
        public virtual bool RemoveSpecialization(string specKeyName)
        {
            Specialization playerSpec = null;

            lock (_specializationLock)
            {
                if (!m_specialization.TryGetValue(specKeyName, out playerSpec))
                    return false;

                m_specialization.Remove(specKeyName);
            }

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.RemoveSpecialization.YouLose", playerSpec.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            return true;
        }

        /// <summary>
        /// Removes the existing spellline from the player, the line instance should be called with GamePlayer.GetSpellLine ONLY and NEVER SkillBase.GetSpellLine!!!!!
        /// </summary>
        /// <param name="line">The spell line to remove</param>
        /// <returns>true if removed</returns>
        protected virtual bool RemoveSpellLine(SpellLine line)
        {
            lock (_spellLinesListLock)
            {
                if (!m_spellLines.Contains(line))
                {
                    return false;
                }

                m_spellLines.Remove(line);
            }

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.RemoveSpellLine.YouLose", line.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            return true;
        }

        /// <summary>
        /// Removes the existing specialization from the player
        /// </summary>
        /// <param name="lineKeyName">The spell line keyname to remove</param>
        /// <returns>true if removed</returns>
        public virtual bool RemoveSpellLine(string lineKeyName)
        {
            SpellLine line = GetSpellLine(lineKeyName);
            if (line == null)
                return false;

            return RemoveSpellLine(line);
        }

        /// <summary>
        /// Reset this player to level 1, respec all skills, remove all spec points, and reset stats
        /// </summary>
        public virtual void Reset()
        {
            byte originalLevel = Level;
            Level = 1;
            Experience = 0;
            RespecAllLines();

            if (Level < originalLevel && originalLevel > 5)
            {
                for (int i = 6; i <= originalLevel; i++)
                {
                    if (CharacterClass.PrimaryStat != eStat.UNDEFINED)
                    {
                        ChangeBaseStat(CharacterClass.PrimaryStat, -1);
                    }
                    if (CharacterClass.SecondaryStat != eStat.UNDEFINED && ((i - 6) % 2 == 0))
                    {
                        ChangeBaseStat(CharacterClass.SecondaryStat, -1);
                    }
                    if (CharacterClass.TertiaryStat != eStat.UNDEFINED && ((i - 6) % 3 == 0))
                    {
                        ChangeBaseStat(CharacterClass.TertiaryStat, -1);
                    }
                }
            }

            CharacterClass.OnLevelUp(this, originalLevel);
        }

        public virtual bool RespecAll()
        {
            if(RespecAllLines())
            {
                // Wipe skills and styles.
                RespecAmountAllSkill--; // Decriment players respecs available.
                if (Level == 5)
                    IsLevelRespecUsed = true;

                return true;
            }

            return false;
        }

        public virtual bool RespecDOL()
        {
            if(RespecAllLines()) // Wipe skills and styles.
            {
                RespecAmountDOL--; // Decriment players respecs available.
                return true;
            }

            return false;
        }

        public virtual int RespecSingle(Specialization specLine)
        {
            int specPoints = RespecSingleLine(specLine); // Wipe skills and styles.
            if (!ServerProperties.Properties.FREE_RESPEC)
                RespecAmountSingleSkill--; // Decriment players respecs available.
            if (Level == 20 || Level == 40)
            {
                IsLevelRespecUsed = true;
            }
            return specPoints;
        }

        public virtual bool RespecRealm(bool useRespecPoint = true)
        {
            bool any = m_realmAbilities.Count > 0;

            foreach (Ability ab in m_realmAbilities)
                RemoveAbility(ab.KeyName);

            m_realmAbilities.Clear();
            if (!ServerProperties.Properties.FREE_RESPEC && useRespecPoint)
                RespecAmountRealmSkill--;
            return any;
        }

        protected virtual bool RespecAllLines()
        {
            bool ok = false;
            IList<Specialization> specList = GetSpecList().Where(e => e.Trainable).ToList();
            foreach (Specialization cspec in specList)
            {
                if (cspec.Level < 2)
                    continue;
                RespecSingleLine(cspec);
                ok = true;
            }
            return ok;
        }

        /// <summary>
        /// Respec single line
        /// </summary>
        /// <param name="specLine">spec line being respec'd</param>
        /// <returns>Amount of points spent in that line</returns>
        protected virtual int RespecSingleLine(Specialization specLine)
        {
            int specPoints = (specLine.Level * (specLine.Level + 1) - 2) / 2;
            // Graveen - autotrain 1.87
            specPoints -= GetAutoTrainPoints(specLine, 0);

            //setting directly the autotrain points in the spec
            if (GetAutoTrainPoints(specLine, 4) == 1 && Level >= 8)
            {
                specLine.Level = (int)Math.Floor((double)Level / 4);
            }
            else specLine.Level = 1;

            return specPoints;
        }

        /// <summary>
        /// Send this players trainer window
        /// </summary>
        public virtual void SendTrainerWindow()
        {
            Out.SendTrainerWindow();
        }

        /// <summary>
        /// returns a list with all specializations
        /// in the order they were added
        /// </summary>
        /// <returns>list of Spec's</returns>
        public virtual IList<Specialization> GetSpecList()
        {
            List<Specialization> list;

            lock (_specializationLock)
            {
                // sort by Level and ID to simulate "addition" order... (try to sort your DB if you want to change this !)
                list = m_specialization.Select(item => item.Value).OrderBy(it => it.LevelRequired).ThenBy(it => it.ID).ToList();
            }

            return list;
        }

        /// <summary>
        /// returns a list with all non trainable skills without styles
        /// This is a copy of Ability until any unhandled Skill subclass needs to go in there...
        /// </summary>
        /// <returns>list of Skill's</returns>
        public virtual IList GetNonTrainableSkillList()
        {
            return GetAllAbilities();
        }

        /// <summary>
        /// Retrives a specific specialization by name
        /// </summary>
        /// <param name="name">the name of the specialization line</param>
        /// <returns>found specialization or null</returns>
        public virtual Specialization GetSpecializationByName(string name)
        {
            Specialization spec = null;

            lock (_specializationLock)
            {
                foreach (KeyValuePair<string, Specialization> entry in m_specialization)
                {
                    if (entry.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        spec = entry.Value;
                        break;
                    }
                }
            }

            return spec;
        }

        /// <summary>
        /// The best armor level this player can use.
        /// </summary>
        public virtual int BestArmorLevel
        {
            get
            {
                int bestLevel = -1;
                bestLevel = Math.Max(bestLevel, GetAbilityLevel(Abilities.AlbArmor));
                bestLevel = Math.Max(bestLevel, GetAbilityLevel(Abilities.HibArmor));
                bestLevel = Math.Max(bestLevel, GetAbilityLevel(Abilities.MidArmor));
                return bestLevel;
            }
        }

        #region Abilities

        /// <summary>
        /// Adds a new Ability to the player
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="sendUpdates"></param>
        public override void AddAbility(Ability ability, bool sendUpdates)
        {
            if (ability == null)
                return;

            base.AddAbility(ability, sendUpdates);
        }

        /// <summary>
        /// Adds a Realm Ability to the player
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="sendUpdates"></param>
        public virtual void AddRealmAbility(RealmAbility ability, bool sendUpdates)
        {
            if (ability == null)
                return;

            m_realmAbilities.FreezeWhile(list => {
                int index = list.FindIndex(ab => ab.ID == ability.ID);
                if (index > -1)
                {
                    list[index].Level = ability.Level;
                }
                else
                {
                    list.Add(ability);
                }
            });
        }

        #endregion Abilities

        public virtual void RemoveAllAbilities()
        {
            lock (_abilitiesLock)
            {
                m_abilities.Clear();
            }
        }

        public virtual void RemoveAllSpecs()
        {
            lock (_specializationLock)
            {
                m_specialization.Clear();
            }
        }

        public virtual void RemoveAllSpellLines()
        {
            lock (_spellLinesListLock)
            {
                m_spellLines.Clear();
            }
        }

        /// <summary>
        /// Retrieve this player Realm Abilities.
        /// </summary>
        /// <returns></returns>
        public virtual List<RealmAbility> GetRealmAbilities()
        {
            return m_realmAbilities.ToList();
        }

        /// <summary>
        /// Asks for existance of specific specialization
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public virtual bool HasSpecialization(string keyName)
        {
            bool hasit = false;

            lock (_specializationLock)
            {
                hasit = m_specialization.ContainsKey(keyName);
            }

            return hasit;
        }

        /// <summary>
        /// returns the level of a specialization
        /// if 0 is returned, the spec is non existent on player
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public override int GetBaseSpecLevel(string keyName)
        {
            Specialization spec = null;
            int level = 0;

            lock (_specializationLock)
            {
                if (m_specialization.TryGetValue(keyName, out spec))
                    level = m_specialization[keyName].Level;
            }

            return level;
        }

        /// <summary>
        /// returns the level of a specialization + bonuses from RR and Items
        /// if 0 is returned, the spec is non existent on the player
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public override int GetModifiedSpecLevel(string keyName)
        {
            if (keyName.StartsWith(GlobalSpellsLines.Champion_Lines_StartWith))
                return 50;

            if (keyName.StartsWith(GlobalSpellsLines.Realm_Spells))
                return Level;

            Specialization spec = null;
            int level = 0;
            lock (_specializationLock)
            {
                if (!m_specialization.TryGetValue(keyName, out spec))
                {
                    if (keyName == GlobalSpellsLines.Combat_Styles_Effect)
                    {
                        if (CharacterClass.ID == (int)eCharacterClass.Reaver || CharacterClass.ID == (int)eCharacterClass.Heretic)
                            level = GetModifiedSpecLevel(Specs.Flexible);
                        if (CharacterClass.ID == (int)eCharacterClass.Valewalker)
                            level = GetModifiedSpecLevel(Specs.Scythe);
                        if (CharacterClass.ID == (int)eCharacterClass.Savage)
                            level = GetModifiedSpecLevel(Specs.Savagery);
                    }

                    level = 0;
                }
            }

            if (spec != null)
            {
                level = spec.Level;
                // TODO: should be all in calculator later, right now
                // needs specKey -> eProperty conversion to find calculator and then
                // needs eProperty -> specKey conversion to find how much points player has spent
                eProperty skillProp = SkillBase.SpecToSkill(keyName);
                if (skillProp != eProperty.Undefined)
                    level += GetModified(skillProp);
            }

            return level;
        }

        /// <summary>
        /// Adds a spell line to the player
        /// </summary>
        /// <param name="line"></param>
        public virtual void AddSpellLine(SpellLine line)
        {
            AddSpellLine(line, true);
        }

        /// <summary>
        /// Adds a spell line to the player
        /// </summary>
        /// <param name="line"></param>
        public virtual void AddSpellLine(SpellLine line, bool notify)
        {
            if (line == null)
                return;

            SpellLine oldline = GetSpellLine(line.KeyName);
            if (oldline == null)
            {
                lock (_spellLinesListLock)
                {
                    m_spellLines.Add(line);
                }

                if (notify)
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.AddSpellLine.YouLearn", line.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
            {
                // message to player
                if (notify && oldline.Level < line.Level)
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UpdateSpellLine.GainPower", line.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                oldline.Level = line.Level;
            }
        }

        /// <summary>
        /// return a list of spell lines in the order they were added
        /// this is a copy only.
        /// </summary>
        /// <returns></returns>
        public virtual List<SpellLine> GetSpellLines()
        {
            List<SpellLine> list = new List<SpellLine>();
            lock (_spellLinesListLock)
            {
                list = new List<SpellLine>(m_spellLines);
            }

            return list;
        }

        /// <summary>
        /// find a spell line on player and return them
        /// </summary>
        /// <param name="keyname"></param>
        /// <returns></returns>
        public virtual SpellLine GetSpellLine(string keyname)
        {
            lock (_spellLinesListLock)
            {
                foreach (SpellLine line in m_spellLines)
                {
                    if (line.KeyName == keyname)
                        return line;
                }
            }
            return null;
        }

        /// <summary>
        /// Skill cache, maintained for network order on "skill use" request...
        /// Second item is for "Parent" Skill if applicable
        /// </summary>
        protected ReaderWriterList<Tuple<Skill, Skill>> m_usableSkills = new ReaderWriterList<Tuple<Skill, Skill>>();

        /// <summary>
        /// List Cast cache, maintained for network order on "spell use" request...
        /// Second item is for "Parent" SpellLine if applicable
        /// </summary>
        protected ReaderWriterList<Tuple<SpellLine, List<Skill>>> m_usableListSpells = new ReaderWriterList<Tuple<SpellLine, List<Skill>>>();

        /// <summary>
        /// Get All Usable Spell for a list Caster.
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public virtual List<Tuple<SpellLine, List<Skill>>> GetAllUsableListSpells(bool update = false)
        {
            if (!update)
            {
                if (m_usableListSpells.Count > 0)
                    return [.. m_usableListSpells];
            }

            List<Tuple<SpellLine, List<Skill>>> results = new List<Tuple<SpellLine, List<Skill>>>();

            // lock during all update, even if replace only take place at end...
            m_usableListSpells.FreezeWhile(innerList => {

                List<Tuple<SpellLine, List<Skill>>> finalbase = new List<Tuple<SpellLine, List<Skill>>>();
                List<Tuple<SpellLine, List<Skill>>> finalspec = new List<Tuple<SpellLine, List<Skill>>>();

                // Add Lists spells ordered.
                foreach (Specialization spec in GetSpecList().Where(item => !item.HybridSpellList))
                {
                    var spells = spec.GetLinesSpellsForLiving(this);

                    foreach (SpellLine sl in spec.GetSpellLinesForLiving(this))
                    {
                        List<Tuple<SpellLine, List<Skill>>> working;
                        if (sl.IsBaseLine)
                        {
                            working = finalbase;
                        }
                        else
                        {
                            working = finalspec;
                        }

                        List<Skill> sps = new List<Skill>();
                        SpellLine key = spells.Keys.FirstOrDefault(el => el.ID == sl.ID);

                        if (key != null && spells.TryGetValue(key, out List<Skill> spellsInLine))
                        {
                            foreach (Skill sp in spellsInLine)
                                sps.Add(sp);
                        }

                        working.Add(new Tuple<SpellLine, List<Skill>>(sl, sps));
                    }
                }

                // Linq isn't used, we need to keep order ! (SelectMany, GroupBy, ToDictionary can't be used !)
                innerList.Clear();
                foreach (var tp in finalbase)
                {
                    innerList.Add(tp);
                    results.Add(tp);
                }

                foreach (var tp in finalspec)
                {
                    innerList.Add(tp);
                    results.Add(tp);
                }
            });

            return results;
        }

        /// <summary>
        /// Get All Player Usable Skill Ordered in Network Order (usefull to check for useskill)
        /// This doesn't get player's List Cast Specs...
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public virtual List<Tuple<Skill, Skill>> GetAllUsableSkills(bool update = false)
        {
            List<Tuple<Skill, Skill>> results = [];

            if (!update)
            {
                if (m_usableSkills.Count > 0)
                    results = new List<Tuple<Skill, Skill>>(m_usableSkills);

                // return results if cache is valid.
                if (results.Count > 0)
                    return results;
            }

            // need to lock for all update.
            m_usableSkills.FreezeWhile(innerList => {

                IList<Specialization> specs = GetSpecList();
                List<Tuple<Skill, Skill>> copylist = new List<Tuple<Skill, Skill>>(innerList);

                // Add Spec
                foreach (Specialization spec in specs.Where(item => item.Trainable))
                {
                    int index = innerList.FindIndex(e => (e.Item1 is Specialization specialization) && specialization.ID == spec.ID);

                    if (index < 0)
                    {
                        // Specs must be appended to spec list
                        innerList.Insert(innerList.Count(e => e.Item1 is Specialization), new Tuple<Skill, Skill>(spec, spec));
                    }
                    else
                    {
                        copylist.Remove(innerList[index]);
                        // Replace...
                        innerList[index] = new Tuple<Skill, Skill>(spec, spec);
                    }
                }

                // Add Abilities (Realm ability should be a custom spec)
                // Abilities order should be saved to db and loaded each time
                foreach (Specialization spec in specs)
                {
                    foreach (Ability abv in spec.GetAbilitiesForLiving(this))
                    {
                        // We need the Instantiated Ability Object for Displaying Correctly According to Player "Activation" Method (if Available)
                        Ability ab = GetAbility(abv.KeyName);

                        if (ab == null)
                            ab = abv;

                        int index = innerList.FindIndex(k => (k.Item1 is Ability ability) && ability.ID == ab.ID);

                        if (index < 0)
                        {
                            // add
                            innerList.Add(new Tuple<Skill, Skill>(ab, spec));
                        }
                        else
                        {
                            copylist.Remove(innerList[index]);
                            // replace
                            innerList[index] = new Tuple<Skill, Skill>(ab, spec);
                        }
                    }
                }

                // Add Hybrid spells
                foreach (Specialization spec in specs.Where(item => item.HybridSpellList))
                {
                    foreach (KeyValuePair<SpellLine, List<Skill>> sl in spec.GetLinesSpellsForLiving(this))
                    {
                        int index = -1;

                        foreach (Spell sp in sl.Value.Where(it => (it is Spell) && !((Spell)it).NeedInstrument).Cast<Spell>())
                        {
                            if (index < innerList.Count)
                                index = innerList.FindIndex(index + 1, e => (e.Item2 is SpellLine spellLine) && spellLine.ID == sl.Key.ID && (e.Item1 is Spell spell) && !spell.NeedInstrument);

                            if (index < 0 || index >= innerList.Count)
                            {
                                // add
                                innerList.Add(new Tuple<Skill, Skill>(sp, sl.Key));
                                // disable replace
                                index = innerList.Count;
                            }
                            else
                            {
                                copylist.Remove(innerList[index]);
                                // replace
                                innerList[index] = new Tuple<Skill, Skill>(sp, sl.Key);
                            }
                        }
                    }
                }

                // Add Songs
                int songIndex = -1;
                foreach (Specialization spec in specs.Where(item => item.HybridSpellList))
                {
                    foreach(KeyValuePair<SpellLine, List<Skill>> sl in spec.GetLinesSpellsForLiving(this))
                    {
                        foreach (Spell sp in sl.Value.Where(it => (it is Spell) && ((Spell)it).NeedInstrument).Cast<Spell>())
                        {
                            if (songIndex < innerList.Count)
                                songIndex = innerList.FindIndex(songIndex + 1, e => (e.Item1 is Spell) && ((Spell)e.Item1).NeedInstrument);

                            if (songIndex < 0 || songIndex >= innerList.Count)
                            {
                                // add
                                innerList.Add(new Tuple<Skill, Skill>(sp, sl.Key));
                                // disable replace
                                songIndex = innerList.Count;
                            }
                            else
                            {
                                copylist.Remove(innerList[songIndex]);
                                // replace
                                innerList[songIndex] = new Tuple<Skill, Skill>(sp, sl.Key);
                            }
                        }
                    }
                }

                // Add Styles
                foreach (Specialization spec in specs)
                {
                    foreach(Style st in spec.GetStylesForLiving(this))
                    {
                        int index = innerList.FindIndex(e => (e.Item1 is Style) && e.Item1.ID == st.ID);
                        if (index < 0)
                        {
                            // add
                            innerList.Add(new Tuple<Skill, Skill>(st, spec));
                        }
                        else
                        {
                            copylist.Remove(innerList[index]);
                            // replace
                            innerList[index] = new Tuple<Skill, Skill>(st, spec);
                        }
                    }
                }

                // clean all not re-enabled skills
                foreach (Tuple<Skill, Skill> item in copylist)
                {
                    innerList.Remove(item);
                }

                foreach (Tuple<Skill, Skill> el in innerList)
                    results.Add(el);
            });

            return results;
        }

        /// <summary>
        /// updates the list of available skills (dependent on caracter specs)
        /// </summary>
        /// <param name="sendMessages">sends "you learn" messages if true</param>
        public virtual void RefreshSpecDependantSkills(bool sendMessages)
        {
            // refresh specs
            LoadClassSpecializations(sendMessages);

            // lock specialization while refreshing...
            lock (_specializationLock)
            {
                foreach (Specialization spec in m_specialization.Values)
                {
                    // check for new Abilities
                    foreach (Ability ab in spec.GetAbilitiesForLiving(this))
                    {
                        if (!HasAbility(ab.KeyName) || GetAbility(ab.KeyName).Level < ab.Level)
                            AddAbility(ab, sendMessages);
                    }

                    // check for new Styles
                    foreach (Style st in spec.GetStylesForLiving(this))
                    {
                        styleComponent.AddStyle(st, sendMessages);
                    }

                    // check for new SpellLine
                    foreach (SpellLine sl in spec.GetSpellLinesForLiving(this))
                    {
                        AddSpellLine(sl, sendMessages);
                    }
                }
            }
        }

        /// <summary>
        /// effectiveness of the player (resurrection illness)
        /// Effectiveness is used in physical/magic damage (exept dot), in weapon skill and max concentration formula
        /// </summary>
        protected double m_playereffectiveness = 1.0;

        /// <summary>
        /// get / set the player's effectiveness.
        /// Effectiveness is used in physical/magic damage (exept dot), in weapon skill and max concentration
        /// </summary>
        public override double Effectiveness
        {
            get { return m_playereffectiveness; }
            set { m_playereffectiveness = value; }
        }

        /// <summary>
        /// Creates new effects list for this living.
        /// </summary>
        /// <returns>New effects list instance</returns>
        protected override GameEffectList CreateEffectsList()
        {
            return new GameEffectPlayerList(this);
        }

        #endregion

        #region Realm-/Region-/Bount-/Skillpoints...

        /// <summary>
        /// Gets/sets player bounty points
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual long BountyPoints
        {
            get { return DBCharacter != null ? DBCharacter.BountyPoints : 0; }
            set { if (DBCharacter != null) DBCharacter.BountyPoints = value; }
        }

        /// <summary>
        /// Gets/sets player realm points
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual long RealmPoints
        {
            get { return DBCharacter != null ? DBCharacter.RealmPoints : 0; }
            set { if (DBCharacter != null) DBCharacter.RealmPoints = value; }
        }

        /// <summary>
        /// Gets/sets player skill specialty points
        /// </summary>
        public virtual int SkillSpecialtyPoints
        {
            get { return VerifySpecPoints(); }
        }

        /// <summary>
        /// Gets/sets player realm specialty points
        /// </summary>
        public virtual int RealmSpecialtyPoints
        {
            get { return GameServer.ServerRules.GetPlayerRealmPointsTotal(this) 
                         - GetRealmAbilities().Where(ab => !(ab is RR5RealmAbility))
                             .Sum(ab => Enumerable.Range(0, ab.Level).Sum(i => ab.CostForUpgrade(i))); }
        }

        /// <summary>
        /// Gets/sets player realm rank
        /// </summary>
        public virtual int RealmLevel
        {
            get { return DBCharacter != null ? DBCharacter.RealmLevel : 0; }
            set
            {
                if (DBCharacter != null)
                    DBCharacter.RealmLevel = value;
                CharacterClass.OnRealmLevelUp(this);
            }
        }

        /// <summary>
        /// Returns the translated realm rank title of the player.
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public virtual string RealmRankTitle(string language)
        {
            string translationId = string.Empty;

            if (Realm != eRealm.None && Realm != eRealm.Door)
            {
                int RR = 0;

                if (RealmLevel > 1)
                    RR = RealmLevel / 10 + 1;

                string realm = string.Empty;
                if (Realm == eRealm.Albion)
                    realm = "Albion";
                else if (Realm == eRealm.Midgard)
                    realm = "Midgard";
                else
                    realm = "Hibernia";

                string gender = Gender == eGender.Female ? "Female" : "Male";

                translationId = string.Format("{0}.RR{1}.{2}", realm, RR, gender);
            }
            else
            {
                translationId = "UnknownRealm";
            }

            string translation;
            if (!LanguageMgr.TryGetTranslation(out translation, language, string.Format("GamePlayer.RealmTitle.{0}", translationId)))
                translation = RealmTitle;

            return translation;
        }

        /// <summary>
        /// Gets player realm rank name
        /// sirru mod 20.11.06
        /// </summary>
        public virtual string RealmTitle
        {
            get
            {
                if (Realm == eRealm.None)
                    return "Unknown Realm";

                try
                {
                    return GlobalConstants.REALM_RANK_NAMES[(int)Realm - 1, (int)Gender - 1, (RealmLevel / 10)];
                }
                catch
                {
                    return "Unknown Rank"; // why aren't all the realm ranks defined above?
                }
            }
        }

        /// <summary>
        /// Called when this player gains realm points
        /// </summary>
        /// <param name="amount">The amount of realm points gained</param>
        public override void GainRealmPoints(long amount)
        {
            GainRealmPoints(amount, true, true);
        }

        /// <summary>
        /// Called when this living gains realm points
        /// </summary>
        /// <param name="amount">The amount of realm points gained</param>
        public void GainRealmPoints(long amount, bool modify)
        {
            GainRealmPoints(amount, modify, true);
        }

        /// <summary>
        /// Called when this player gains realm points
        /// </summary>
        public void GainRealmPoints(long amount, bool modify, bool sendMessage)
        {
            GainRealmPoints(amount, modify, true, true);
        }

        /// <summary>
        /// Called when this player gains realm points
        /// </summary>
        /// <param name="amount">The amount of realm points gained</param>
        /// <param name="modify">Should we apply the rp modifer</param>
        /// <param name="sendMessage">Wether to send a message like "You have gained N realmpoints"</param>
        /// <param name="notify"></param>
        public virtual void GainRealmPoints(long amount, bool modify, bool sendMessage, bool notify)
        {
            if (!GainRP)
                return;

            if (modify)
            {
                //rp rate modifier
                double modifier = ServerProperties.Properties.RP_RATE;
                if (modifier != -1)
                    amount = (long)(amount * modifier);

                //[StephenxPimente]: Zone Bonus Support
                if (ServerProperties.Properties.ENABLE_ZONE_BONUSES)
                {
                    int zoneBonus = (((int)amount * ZoneBonus.GetRPBonus(this)) / 100);
                    if (zoneBonus > 0)
                    {
                        Out.SendMessage(ZoneBonus.GetBonusMessage(this, (int)(zoneBonus * ServerProperties.Properties.RP_RATE), ZoneBonusType.Rp),
                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        GainRealmPoints((long)(zoneBonus * ServerProperties.Properties.RP_RATE), false, false, false);
                    }
                }

                //[Freya] Nidel: ToA Rp Bonus
                long rpBonus = GetModified(eProperty.RealmPoints);
                if (rpBonus > 0)
                {
                    amount += (amount * rpBonus) / 100;
                }
            }

            if (notify)
                base.GainRealmPoints(amount);

            RealmPoints += amount;
            m_statistics.AddToTotalRealmPointsEarned((uint) amount);

            if (m_guild != null && Client.Account.PrivLevel == 1)
                m_guild.RealmPoints += amount;

            if (sendMessage == true && amount > 0)
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainRealmPoints.YouGet", amount.ToString()), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            while (RealmPoints >= CalculateRPsFromRealmLevel(RealmLevel + 1) && RealmLevel < ( REALMPOINTS_FOR_LEVEL.Length - 1 ) )
            {
                RealmLevel++;
                Out.SendUpdatePlayer();
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainRealmPoints.GainedLevel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                if (RealmLevel % 10 == 0)
                {
                    Out.SendUpdatePlayerSkills(true);
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainRealmPoints.GainedRank"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainRealmPoints.ReachedRank", (RealmLevel / 10) + 1), eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainRealmPoints.NewRealmTitle", RealmRankTitle(Client.Account.Language)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainRealmPoints.GainBonus", RealmLevel / 10), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    foreach (GamePlayer plr in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        plr.Out.SendLivingDataUpdate(this, true);
                    Notify(GamePlayerEvent.RRLevelUp, this);
                }
                else
                    Notify(GamePlayerEvent.RLLevelUp, this);
                if (GameServer.ServerRules.CanGenerateNews(this) && ((RealmLevel >= 40 && RealmLevel % 10 == 0) || RealmLevel >= 60))
                {
                    string newsmessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainRealmPoints.ReachedRankNews", Name, RealmLevel + 10, LastPositionUpdateZone.Description);
                    NewsMgr.CreateNews(newsmessage, this.Realm, eNewsType.RvRLocal, true);
                }
            }

            Out.SendUpdatePoints();
        }

        /// <summary>
        /// Called when this living buy something with realm points
        /// </summary>
        /// <param name="amount">The amount of realm points loosed</param>
        public bool RemoveBountyPoints(long amount)
        {
            return RemoveBountyPoints(amount, null);
        }
        /// <summary>
        /// Called when this living buy something with realm points
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool RemoveBountyPoints(long amount, string str)
        {
            return RemoveBountyPoints(amount, str, eChatType.CT_Say, eChatLoc.CL_SystemWindow);
        }
        /// <summary>
        /// Called when this living buy something with realm points
        /// </summary>
        /// <param name="amount">The amount of realm points loosed</param>
        /// <param name="loc">The chat location</param>
        /// <param name="str">The message</param>
        /// <param name="type">The chat type</param>
        public virtual bool RemoveBountyPoints(long amount, string str, eChatType type, eChatLoc loc)
        {
            if (BountyPoints < amount)
                return false;
            BountyPoints -= amount;
            Out.SendUpdatePoints();
            if (str != null && amount != 0)
                Out.SendMessage(str, type, loc);
            return true;
        }

        /// <summary>
        /// Player gains bounty points
        /// </summary>
        /// <param name="amount">The amount of bounty points</param>
        public override void GainBountyPoints(long amount)
        {
            GainBountyPoints(amount, true, true);
        }

        /// <summary>
        /// Player gains bounty points
        /// </summary>
        /// <param name="amount">The amount of bounty points</param>
        public void GainBountyPoints(long amount, bool modify)
        {
            GainBountyPoints(amount, modify, true);
        }

        /// <summary>
        /// Called when player gains bounty points
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="modify"></param>
        /// <param name="sendMessage"></param>
        public void GainBountyPoints(long amount, bool modify, bool sendMessage)
        {
            GainBountyPoints(amount, modify, true, true);
        }


        /// <summary>
        /// Called when player gains bounty points
        /// </summary>
        /// <param name="amount">The amount of bounty points gained</param>
        /// <param name="multiply">Should this amount be multiplied by the BP Rate</param>
        /// <param name="sendMessage">Wether to send a message like "You have gained N bountypoints"</param>
        public virtual void GainBountyPoints(long amount, bool modify, bool sendMessage, bool notify)
        {
            if (modify)
            {
                //bp rate modifier
                double modifier = ServerProperties.Properties.BP_RATE;
                if (modifier != -1)
                    amount = (long)(amount * modifier);

                //[StephenxPimente]: Zone Bonus Support
                if (ServerProperties.Properties.ENABLE_ZONE_BONUSES)
                {
                    int zoneBonus = (((int)amount * ZoneBonus.GetBPBonus(this)) / 100);
                    if (zoneBonus > 0)
                    {
                        Out.SendMessage(ZoneBonus.GetBonusMessage(this, (int)(zoneBonus * ServerProperties.Properties.BP_RATE), ZoneBonusType.Bp),
                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        GainBountyPoints((long)(zoneBonus * ServerProperties.Properties.BP_RATE), false, false, false);
                    }
                }

                //[Freya] Nidel: ToA Bp Bonus
                long bpBonus = GetModified(eProperty.BountyPoints);

                if (bpBonus > 0)
                {
                    amount += (amount * bpBonus) / 100;
                }
            }

            if (notify)
                base.GainBountyPoints(amount);

            BountyPoints += amount;

            if (m_guild != null && Client.Account.PrivLevel == 1)
                m_guild.BountyPoints += amount;

            if(sendMessage == true)
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainBountyPoints.YouGet", amount.ToString()), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            Out.SendUpdatePoints();
        }

        /// <summary>
        /// Holds realm points needed for special realm level
        /// </summary>
        public static readonly long[] REALMPOINTS_FOR_LEVEL =
        {
            0,	// for level 0
            0,	// for level 1
            25,	// for level 2
            125,	// for level 3
            350,	// for level 4
            750,	// for level 5
            1375,	// for level 6
            2275,	// for level 7
            3500,	// for level 8
            5100,	// for level 9
            7125,	// for level 10
            9625,	// for level 11
            12650,	// for level 12
            16250,	// for level 13
            20475,	// for level 14
            25375,	// for level 15
            31000,	// for level 16
            37400,	// for level 17
            44625,	// for level 18
            52725,	// for level 19
            61750,	// for level 20
            71750,	// for level 21
            82775,	// for level 22
            94875,	// for level 23
            108100,	// for level 24
            122500,	// for level 25
            138125,	// for level 26
            155025,	// for level 27
            173250,	// for level 28
            192850,	// for level 29
            213875,	// for level 30
            236375,	// for level 31
            260400,	// for level 32
            286000,	// for level 33
            313225,	// for level 34
            342125,	// for level 35
            372750,	// for level 36
            405150,	// for level 37
            439375,	// for level 38
            475475,	// for level 39
            513500,	// for level 40
            553500,	// for level 41
            595525,	// for level 42
            639625,	// for level 43
            685850,	// for level 44
            734250,	// for level 45
            784875,	// for level 46
            837775,	// for level 47
            893000,	// for level 48
            950600,	// for level 49
            1010625,	// for level 50
            1073125,	// for level 51
            1138150,	// for level 52
            1205750,	// for level 53
            1275975,	// for level 54
            1348875,	// for level 55
            1424500,	// for level 56
            1502900,	// for level 57
            1584125,	// for level 58
            1668225,	// for level 59
            1755250,	// for level 60
            1845250,	// for level 61
            1938275,	// for level 62
            2034375,	// for level 63
            2133600,	// for level 64
            2236000,	// for level 65
            2341625,	// for level 66
            2450525,	// for level 67
            2562750,	// for level 68
            2678350,	// for level 69
            2797375,	// for level 70
            2919875,	// for level 71
            3045900,	// for level 72
            3175500,	// for level 73
            3308725,	// for level 74
            3445625,	// for level 75
            3586250,	// for level 76
            3730650,	// for level 77
            3878875,	// for level 78
            4030975,	// for level 79
            4187000,	// for level 80
            4347000,	// for level 81
            4511025,	// for level 82
            4679125,	// for level 83
            4851350,	// for level 84
            5027750,	// for level 85
            5208375,	// for level 86
            5393275,	// for level 87
            5582500,	// for level 88
            5776100,	// for level 89
            5974125,	// for level 90
            6176625,	// for level 91
            6383650,	// for level 92
            6595250,	// for level 93
            6811475,	// for level 94
            7032375,	// for level 95
            7258000,	// for level 96
            7488400,	// for level 97
            7723625,	// for level 98
            7963725,	// for level 99
            8208750,	// for level 100
            9111713,	// for level 101
            10114001,	// for level 102
            11226541,	// for level 103
            12461460,	// for level 104
            13832221,	// for level 105
            15353765,	// for level 106
            17042680,	// for level 107
            18917374,	// for level 108
            20998286,	// for level 109
            23308097,	// for level 110
            25871988,	// for level 111
            28717906,	// for level 112
            31876876,	// for level 113
            35383333,	// for level 114
            39275499,	// for level 115
            43595804,	// for level 116
            48391343,	// for level 117
            53714390,	// for level 118
            59622973,	// for level 119
            66181501,	// for level 120
            73461466,	// for level 121
            81542227,	// for level 122
            90511872,	// for level 123
            100468178,	// for level 124
            111519678,	// for level 125
            123786843,	// for level 126
            137403395,	// for level 127
            152517769,	// for level 128
            169294723,	// for level 129
            187917143,	// for level 130

        };

        /// <summary>
        /// Calculates amount of RealmPoints needed for special realm level
        /// </summary>
        /// <param name="realmLevel">realm level</param>
        /// <returns>amount of realm points</returns>
        protected virtual long CalculateRPsFromRealmLevel(int realmLevel)
        {
            if (realmLevel < REALMPOINTS_FOR_LEVEL.Length)
                return REALMPOINTS_FOR_LEVEL[realmLevel];

            // thanks to Linulo from http://daoc.foren.4players.de/viewtopic.php?t=40839&postdays=0&postorder=asc&start=0
            return (long)(25.0 / 3.0 * (realmLevel * realmLevel * realmLevel) - 25.0 / 2.0 * (realmLevel * realmLevel) + 25.0 / 6.0 * realmLevel);
        }

        /// <summary>
        /// Calculates realm level from realm points. SLOW.
        /// </summary>
        /// <param name="realmPoints">amount of realm points</param>
        /// <returns>realm level: RR5L3 = 43, RR1L2 = 2</returns>
        protected virtual int CalculateRealmLevelFromRPs(long realmPoints)
        {
            if (realmPoints == 0)
                return 0;

            int i;

            for (i = REALMPOINTS_FOR_LEVEL.Length - 1; i > 0; i--)
            {
                if (REALMPOINTS_FOR_LEVEL[i] <= realmPoints)
                    break;
            }

            return i;
        }

        /// <summary>
        /// Realm point value of this player
        /// </summary>
        public override int RealmPointsValue
        {
            get
            {
                // Pre-1.81 formula: https://camelotherald.fandom.com/wiki/Patch_Notes:_Version_1.81
                // 25 at RR1, level 25.
                // 225 at RR1, level 35, 245 at RR3, level 35.
                // 900 at RR1, level 50. 990 at RR10, level 50.
                int modifiedLevel = Level - 20;
                return Math.Max(1, modifiedLevel * modifiedLevel) + RealmLevel;
            }
        }

        /// <summary>
        /// Bounty point value of this player
        /// </summary>
        public override int BountyPointsValue
        {
            // TODO: correct formula!
            get { return (int)(1 + Level * 0.6); }
        }

        /// <summary>
        /// Returns the amount of experience this player is worth
        /// </summary>
        public override long ExperienceValue
        {
            get
            {
                return base.ExperienceValue * 4;
            }
        }

        public static readonly int[] prcRestore =
        {
            // http://www.silicondragon.com/Gaming/DAoC/Misc/XPs.htm
            1,//0
            3,//1
            6,//2
            10,//3
            15,//4
            21,//5
            33,//6
            53,//7
            82,//8
            125,//9
            188,//10
            278,//11
            352,//12
            443,//13
            553,//14
            688,//15
            851,//16
            1048,//17
            1288,//18
            1578,//19
            1926,//20
            2347,//21
            2721,//22
            3146,//23
            3633,//24
            4187,//25
            4820,//26
            5537,//27
            6356,//28
            7281,//29
            8337,//30
            9532,//31 - from logs
            10886,//32 - from logs
            12421,//33 - from logs
            14161,//34
            16131,//35
            18360,//36 - recheck
            19965,//37 - guessed
            21857,//38
            23821,//39
            25928,//40 - guessed
            28244,//41
            30731,//42
            33411,//43
            36308,//44
            39438,//45
            42812,//46
            46454,//47
            50385,//48
            54625,//49
            59195,//50
        };

        /// <summary>
        /// Money value of this player
        /// </summary>
        public override long MoneyValue => 3 * prcRestore[Level < prcRestore.Length ? Level : prcRestore.Length - 1];

        #endregion

        #region Level/Experience

        public const byte MAX_LEVEL = 50;

        /// <summary>
        /// How much experience is needed for a given level?
        /// </summary>
        public virtual long GetExperienceNeededForLevel(int level)
        {
            if (level > MAX_LEVEL)
                return GetExperienceAmountForLevel(MAX_LEVEL);

            if (level <= 0)
                return GetExperienceAmountForLevel(0);

            return GetExperienceAmountForLevel(level - 1);
        }

        /// <summary>
        /// How Much Experience Needed For Level
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static long GetExperienceAmountForLevel(int level)
        {
            try
            {
                return XPForLevel[level];
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// A table that holds the required XP/Level
        /// This must include a final entry for MaxLevel + 1
        /// </summary>
        private static readonly long[] XPForLevel =
        {
            0, // xp to level 1
            50, // xp to level 2
            250, // xp to level 3
            850, // xp to level 4
            2300, // xp to level 5
            6350, // xp to level 6
            15950, // xp to level 7
            37950, // xp to level 8
            88950, // xp to level 9
            203950, // xp to level 10
            459950, // xp to level 11
            839950, // xp to level 12
            1399950, // xp to level 13
            2199950, // xp to level 14
            3399950, // xp to level 15
            5199950, // xp to level 16
            7899950, // xp to level 17
            11799950, // xp to level 18
            17499950, // xp to level 19
            25899950, // xp to level 20
            38199950, // xp to level 21
            54699950, // xp to level 22
            76999950, // xp to level 23
            106999950, // xp to level 24
            146999950, // xp to level 25
            199999950, // xp to level 26
            269999950, // xp to level 27
            359999950, // xp to level 28
            479999950, // xp to level 29
            639999950, // xp to level 30
            849999950, // xp to level 31
            1119999950, // xp to level 32
            1469999950, // xp to level 33
            1929999950, // xp to level 34
            2529999950, // xp to level 35
            3319999950, // xp to level 36
            4299999950, // xp to level 37
            5499999950, // xp to level 38
            6899999950, // xp to level 39
            8599999950, // xp to level 40
            12899999950, // xp to level 41
            20699999950, // xp to level 42
            29999999950, // xp to level 43
            40799999950, // xp to level 44
            53999999950, // xp to level 45
            69599999950, // xp to level 46
            88499999950, // xp to level 47
            110999999950, // xp to level 48
            137999999950, // xp to level 49
            169999999950, // xp to level 50
            999999999950, // xp to level 51
        };

        /// <summary>
        /// Gets or sets the current xp of this player
        /// </summary>
        public virtual long Experience
        {
            get { return DBCharacter != null ? DBCharacter.Experience : 0; }
            set
            {
                if (DBCharacter != null)
                    DBCharacter.Experience = value;
            }
        }

        /// <summary>
        /// Returns the xp that are needed for the next level
        /// </summary>
        public virtual long ExperienceForNextLevel
        {
            get
            {
                return GetExperienceNeededForLevel(Level + 1);
            }
        }

        /// <summary>
        /// Returns the xp that were needed for the current level
        /// </summary>
        public virtual long ExperienceForCurrentLevel
        {
            get
            {
                return GetExperienceNeededForLevel(Level);
            }
        }

        /// <summary>
        /// Returns the xp that is needed for the second stage of current level
        /// </summary>
        public virtual long ExperienceForCurrentLevelSecondStage
        {
            get { return 1 + ExperienceForCurrentLevel + (ExperienceForNextLevel - ExperienceForCurrentLevel) / 2; }
        }

        /// <summary>
        /// Returns how far into the level we have progressed
        /// A value between 0 and 1000 (1 bubble = 100)
        /// </summary>
        public virtual ushort LevelPermill
        {
            get
            {
                //No progress if we haven't even reached current level!
                if (Experience < ExperienceForCurrentLevel)
                    return 0;
                //No progess after maximum level
                if (Level > MAX_LEVEL)
                    return 0;
                return (ushort)(1000 * (Experience - ExperienceForCurrentLevel) / (ExperienceForNextLevel - ExperienceForCurrentLevel));
            }
        }

        public void ForceGainExperience(long expTotal)
        {
            if (IsLevelSecondStage)
            {
                if (Experience + expTotal < ExperienceForCurrentLevelSecondStage)
                {
                    expTotal = ExperienceForCurrentLevelSecondStage - Experience;
                }
            }
            else if (Experience + expTotal < ExperienceForCurrentLevel)
            {
                expTotal = ExperienceForCurrentLevel - Experience;
            }

            Experience += expTotal;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainExperience.YouGet", expTotal.ToString("N0", System.Globalization.NumberFormatInfo.InvariantInfo)), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            if (expTotal >= 0)
            {
                //Level up
                if (Level >= 5 && !CharacterClass.HasAdvancedFromBaseClass())
                {
                    if (expTotal > 0)
                    {
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainExperience.CannotRaise"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainExperience.TalkToTrainer"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    }
                }
                else if (Level >= 40 && Level < MAX_LEVEL && !IsLevelSecondStage && Experience >= ExperienceForCurrentLevelSecondStage)
                {
                    OnLevelSecondStage();
                    Notify(GamePlayerEvent.LevelSecondStage, this);
                }
                else if (Level < MAX_LEVEL && Experience >= ExperienceForNextLevel)
                {
                    Level++;
                }
            }
            Out.SendUpdatePoints();
        }

        /// <summary>
        /// Called whenever this player gains experience
        /// </summary>
        public override void GainExperience(GainedExperienceEventArgs arguments, bool notify = true)
        {
            long expTotal = arguments.ExpTotal;

            if (!GainXP && expTotal > 0)
                return;

            if (HCFlag && this.Group != null)
            {
                foreach (var player in this.Group.GetPlayersInTheGroup())
                {
                    if (player.Level > this.Level + 5)
                        expTotal = 0;
                }
                if(expTotal == 0)
                    this.Out.SendMessage("This kill was not hardcore enough to gain experience.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            long baseXp = arguments.ExpBase;

            if (arguments.AllowMultiply)
            {
                // Recalculate base experience.
                expTotal -= baseXp;

                if (Properties.ENABLE_ZONE_BONUSES)
                {
                    long zoneBonus = baseXp * ZoneBonus.GetXPBonus(this) / 100;

                    if (zoneBonus > 0)
                    {
                        zoneBonus = (long) (zoneBonus * Properties.XP_RATE);
                        Out.SendMessage(ZoneBonus.GetBonusMessage(this, (int) zoneBonus, ZoneBonusType.Xp), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        GainExperience(new(zoneBonus, 0, 0, 0, 0, 0, false, false, eXPSource.Other), false);
                    }
                }

                if (CurrentRegion.IsRvR || CurrentZone.IsRvR)
                    baseXp = (long) (baseXp * Properties.RvR_XP_RATE);
                else
                    baseXp = (long) (baseXp * Properties.XP_RATE);

                long xpBonus = GetModified(eProperty.XpPoints);

                if (xpBonus != 0)
                    baseXp += baseXp * xpBonus / 100;

                expTotal += baseXp;
            }

            // Get Champion Experience too
            // GainChampionExperience(expTotal);

            if (IsLevelSecondStage)
            {
                if (Experience + expTotal < ExperienceForCurrentLevelSecondStage)
                {
                    expTotal = ExperienceForCurrentLevelSecondStage - Experience;
                }
            }
            else if (Experience + expTotal < ExperienceForCurrentLevel)
            {
                expTotal = ExperienceForCurrentLevel - Experience;
            }

            if (arguments.SendMessage && expTotal > 0)
            {
                System.Globalization.NumberFormatInfo format = System.Globalization.NumberFormatInfo.InvariantInfo;
                string totalExpStr = expTotal.ToString("N0", format);
                string expCampBonusStr = string.Empty;
                string expGroupBonusStr = string.Empty;
                string expGuildBonusStr = string.Empty;
                string expBafBonusStr = string.Empty;
                string expOutpostBonusStr = string.Empty;

                if (arguments.ExpCampBonus > 0)
                    expCampBonusStr = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainExperience.CampBonus", arguments.ExpCampBonus.ToString("N0", format)) + " ";

                if (arguments.ExpGroupBonus > 0)
                    expGroupBonusStr = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainExperience.GroupBonus", arguments.ExpGroupBonus.ToString("N0", format)) + " ";

                if (arguments.ExpGuildBonus > 0)
                    expGuildBonusStr = "("+ arguments.ExpGuildBonus.ToString("N0", format) + " guild bonus) ";

                if (arguments.ExpBafBonus > 0)
                    expBafBonusStr = "("+ arguments.ExpBafBonus.ToString("N0", format) + " baf bonus) ";

                if (arguments.ExpOutpostBonus > 0)
                    expOutpostBonusStr = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainExperience.OutpostBonus", arguments.ExpOutpostBonus.ToString("N0", format)) + " ";

                string message = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainExperience.YouGet", totalExpStr) + " " + expCampBonusStr + expGroupBonusStr + expGuildBonusStr + expBafBonusStr + expOutpostBonusStr;
                Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            Experience += expTotal;

            if (expTotal >= 0)
            {
                //Level up
                if (Level >= 5 && !CharacterClass.HasAdvancedFromBaseClass())
                {
                    if (expTotal > 0)
                    {
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainExperience.CannotRaise"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainExperience.TalkToTrainer"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    }
                }
                else if (Level >= 40 && Level < MAX_LEVEL && !IsLevelSecondStage && Experience >= ExperienceForCurrentLevelSecondStage)
                {
                    OnLevelSecondStage();
                    Notify(GamePlayerEvent.LevelSecondStage, this);
                }
                else if (Level < MAX_LEVEL && Experience >= ExperienceForNextLevel)
                {
                    Level++;
                }
            }

            Out.SendUpdatePoints();
        }

        /// <summary>
        /// Gets or sets the level of the player
        /// (delegate to PlayerCharacter)
        /// </summary>
        public override byte Level
        {
            get { return DBCharacter != null ? (byte)DBCharacter.Level : base.Level; }
            set
            {
                int oldLevel = Level;

                // `base.Level` ends up having to call the getter (in `SendLivingDataUpdate`).
                // Which, in the case of `GamePlayer`, accesses `DBCharacter`.
                // For this reason, `DBCharacter.Level` must be set first.
                if (DBCharacter != null)
                    DBCharacter.Level = value;

                base.Level = value;

                if (oldLevel > 0)
                {
                    if (value > oldLevel)
                    {
                        OnLevelUp(oldLevel);
                        Notify(GamePlayerEvent.LevelUp, this);
                    }
                    else
                    {
                        //update the mob colours
                        Out.SendLevelUpSound();
                    }
                }
            }
        }

        /// <summary>
        /// What is the base, unmodified level of this character.
        /// </summary>
        public override byte BaseLevel
        {
            get { return DBCharacter != null ? (byte)DBCharacter.Level : base.BaseLevel; }
        }

        /// <summary>
        /// What level is displayed to another player
        /// </summary>
        public override byte GetDisplayLevel(GamePlayer player)
        {
            return Math.Min((byte)50, Level);
        }

        /// <summary>
        /// Is this player in second stage of current level
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual bool IsLevelSecondStage
        {
            get { return DBCharacter != null ? DBCharacter.IsLevelSecondStage : false; }
            set { if (DBCharacter != null) DBCharacter.IsLevelSecondStage = value; }
        }

        /// <summary>
        /// Called when this player levels
        /// </summary>
        /// <param name="previouslevel"></param>
        public virtual void OnLevelUp(int previouslevel)
        {
            IsLevelSecondStage = false;
            int isHardcore = 0;
            int realm = 0;

            if (HCFlag)
                isHardcore = 1;

            switch (Realm)
            {
                case eRealm._FirstPlayerRealm:
                    realm = 1;
                    break;
                case eRealm.Midgard:
                    realm = 2;
                    break;
                case eRealm._LastPlayerRealm:
                    realm = 3;
                    break;
                default:
                    realm = 0;
                    break;
            }

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.YouRaise", Level), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.YouAchieved", Level), eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
            Out.SendPlayerFreeLevelUpdate();

            if (FreeLevelState == 2)
            {
                Out.SendDialogBox(eDialogCode.SimpleWarning, 0, 0, 0, 0, eDialogType.Ok, true, LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.FreeLevelEligible"));
            }

            if (Level == 20)
            {
                // Creates a TimeXLevel to track the levelling time to 20
                if (Client.Account.PrivLevel == 1)
                {
                    TimeSpan playedTime = TimeSpan.FromSeconds(this.PlayedTime);
                    DbTimeXLevel MaxLevelTime = new DbTimeXLevel();
                    MaxLevelTime.Character_ID = this.ObjectId;
                    MaxLevelTime.Character_Name = this.Name;
                    MaxLevelTime.Character_Realm = realm;
                    MaxLevelTime.Character_Class = ((eCharacterClass)this.CharacterClass.ID).ToString();
                    MaxLevelTime.Character_Level = this.Level;
                    MaxLevelTime.Hardcore = isHardcore;
                    MaxLevelTime.TimeToLevel = playedTime.Days + "d " + playedTime.Hours + "h " + playedTime.Minutes + "m ";
                    MaxLevelTime.SecondsToLevel = PlayedTime;
                    MaxLevelTime.HoursToLevel = PlayedTime / 60 / 60;
                    GameServer.Database.AddObject(MaxLevelTime);
                }
            }

            if (Level == 30)
            {
                // Creates a TimeXLevel to track the levelling time to 30
                if (Client.Account.PrivLevel == 1)
                {
                    TimeSpan playedTime = TimeSpan.FromSeconds(this.PlayedTime);
                    DbTimeXLevel MaxLevelTime = new DbTimeXLevel();
                    MaxLevelTime.Character_ID = this.ObjectId;
                    MaxLevelTime.Character_Name = this.Name;
                    MaxLevelTime.Character_Realm = realm;
                    MaxLevelTime.Character_Class = ((eCharacterClass)this.CharacterClass.ID).ToString();
                    MaxLevelTime.Character_Level = this.Level;
                    MaxLevelTime.Hardcore = isHardcore;
                    MaxLevelTime.TimeToLevel = playedTime.Days + "d " + playedTime.Hours + "h " + playedTime.Minutes + "m ";
                    MaxLevelTime.SecondsToLevel = PlayedTime;
                    MaxLevelTime.HoursToLevel = PlayedTime / 60 / 60;
                    GameServer.Database.AddObject(MaxLevelTime);
                }
            }

            if (Level == 40)
            {
                // Creates a TimeXLevel to track the levelling time to 40
                if (Client.Account.PrivLevel == 1)
                {
                    TimeSpan playedTime = TimeSpan.FromSeconds(this.PlayedTime);
                    DbTimeXLevel MaxLevelTime = new DbTimeXLevel();
                    MaxLevelTime.Character_ID = this.ObjectId;
                    MaxLevelTime.Character_Name = this.Name;
                    MaxLevelTime.Character_Realm = realm;
                    MaxLevelTime.Character_Class = ((eCharacterClass)this.CharacterClass.ID).ToString();
                    MaxLevelTime.Character_Level = this.Level;
                    MaxLevelTime.Hardcore = isHardcore;
                    MaxLevelTime.TimeToLevel = playedTime.Days + "d " + playedTime.Hours + "h " + playedTime.Minutes + "m ";
                    MaxLevelTime.SecondsToLevel = PlayedTime;
                    MaxLevelTime.HoursToLevel = PlayedTime / 60 / 60;
                    GameServer.Database.AddObject(MaxLevelTime);
                }
            }

            if (Level == 45)
            {
                // Creates a TimeXLevel to track the levelling time to 45
                if (Client.Account.PrivLevel == 1)
                {
                    TimeSpan playedTime = TimeSpan.FromSeconds(this.PlayedTime);
                    DbTimeXLevel MaxLevelTime = new DbTimeXLevel();
                    MaxLevelTime.Character_ID = this.ObjectId;
                    MaxLevelTime.Character_Name = this.Name;
                    MaxLevelTime.Character_Realm = realm;
                    MaxLevelTime.Character_Class = ((eCharacterClass)this.CharacterClass.ID).ToString();
                    MaxLevelTime.Character_Level = this.Level;
                    MaxLevelTime.Hardcore = isHardcore;
                    MaxLevelTime.TimeToLevel = playedTime.Days + "d " + playedTime.Hours + "h " + playedTime.Minutes + "m ";
                    MaxLevelTime.SecondsToLevel = PlayedTime;
                    MaxLevelTime.HoursToLevel = PlayedTime / 60 / 60;
                    GameServer.Database.AddObject(MaxLevelTime);
                }
            }

            if (Level == 50)
            {
                // Check if player has completed the Hardcore Challenge
                if (HCFlag)
                {
                    HCFlag = false;
                    HCCompleted = true;
                    Out.SendMessage("You have reached Level 50! Your Hardcore flag has been disabled.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    AtlasROGManager.GenerateReward(this, 5000);
                }

                // Creates a TimeXLevel to track the levelling time to 50

                if (Client.Account.PrivLevel == 1)
                {
                    TimeSpan playedTime = TimeSpan.FromSeconds(this.PlayedTime);
                    DbTimeXLevel MaxLevelTime = new DbTimeXLevel();
                    MaxLevelTime.Character_ID = this.ObjectId;
                    MaxLevelTime.Character_Name = this.Name;
                    MaxLevelTime.Character_Realm = realm;
                    MaxLevelTime.Character_Class = ((eCharacterClass)this.CharacterClass.ID).ToString();
                    MaxLevelTime.Character_Level = this.Level;
                    MaxLevelTime.Hardcore = isHardcore;
                    MaxLevelTime.TimeToLevel = playedTime.Days + "d " + playedTime.Hours + "h " + playedTime.Minutes + "m ";
                    MaxLevelTime.SecondsToLevel = PlayedTime;
                    MaxLevelTime.HoursToLevel = PlayedTime / 60 / 60;
                    GameServer.Database.AddObject(MaxLevelTime);
                }

            }

            if (Level == MAX_LEVEL)
            {
                if (GameServer.ServerRules.CanGenerateNews(this))
                {
                    string newsmessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.Reached", Name, Level, LastPositionUpdateZone.Description);
                    NewsMgr.CreateNews(newsmessage, Realm, eNewsType.PvE, true);
                }
            }

            //level 20 changes realm title and gives 1 realm skill point
            if (Level == 20)
                GainRealmPoints(0);

            // Adjust stats
            bool statsChanged = false;
            // stat increases start at level 6
            if (Level > 5)
            {
                for (int i = Level; i > Math.Max(previouslevel, 5); i--)
                {
                    if (CharacterClass.PrimaryStat != eStat.UNDEFINED)
                    {
                        ChangeBaseStat(CharacterClass.PrimaryStat, 1);
                        statsChanged = true;
                    }
                    if (CharacterClass.SecondaryStat != eStat.UNDEFINED && ((i - 6) % 2 == 0))
                    { // base level to start adding stats is 6
                        ChangeBaseStat(CharacterClass.SecondaryStat, 1);
                        statsChanged = true;
                    }
                    if (CharacterClass.TertiaryStat != eStat.UNDEFINED && ((i - 6) % 3 == 0))
                    { // base level to start adding stats is 6
                        ChangeBaseStat(CharacterClass.TertiaryStat, 1);
                        statsChanged = true;
                    }
                }
            }

            if (statsChanged)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.StatRaise"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            CharacterClass.OnLevelUp(this, previouslevel);
            GameServer.ServerRules.OnPlayerLevelUp(this, previouslevel);
            RefreshSpecDependantSkills(true);

            // Echostorm - Code for display of new title on level up
            // Get old and current rank titles
            string currenttitle = CharacterClass.GetTitle(this, Level);

            // check for difference
            if (CharacterClass.GetTitle(this, previouslevel) != currenttitle)
            {
                // Inform player of new title.
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.AttainedRank", currenttitle), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            // spec points
            int specpoints = 0;
            for (int i = Level; i > previouslevel; i--)
            {
                if (i <= 5) specpoints += i; //start levels
                else specpoints += CharacterClass.SpecPointsMultiplier * i / 10; //spec levels
            }
            if (specpoints > 0)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.YouGetSpec", specpoints), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            // old hp
            int oldhp = CalculateMaxHealth(previouslevel, GetBaseStat(eStat.CON));

            // old power
            int oldpow = 0;
            if (CharacterClass.ManaStat != eStat.UNDEFINED)
            {
                oldpow = CalculateMaxMana(previouslevel, GetBaseStat(CharacterClass.ManaStat));
            }

            // hp upgrade
            int newhp = CalculateMaxHealth(Level, GetBaseStat(eStat.CON));
            if (oldhp > 0 && oldhp < newhp)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.HitsRaise", (newhp - oldhp)), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            // power upgrade
            if (CharacterClass.ManaStat != eStat.UNDEFINED)
            {
                int newpow = CalculateMaxMana(Level, GetBaseStat(CharacterClass.ManaStat));
                if (newpow > 0 && oldpow < newpow)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.PowerRaise", (newpow - oldpow)), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }
            }

            if (IsAlive)
            {
                // workaround for starting regeneration
                StartHealthRegeneration();
                StartPowerRegeneration();
            }

            DeathCount = 0;

            if (Group != null)
            {
                Group.UpdateGroupWindow();
            }
            Out.SendUpdatePlayer(); // Update player level
            Out.SendCharStatsUpdate(); // Update Stats and MaxHitpoints
            Out.SendCharResistsUpdate();
            Out.SendUpdatePlayerSkills(true);
            Out.SendUpdatePoints();
            UpdatePlayerStatus();

            // not sure what package this is, but it triggers the mob color update
            Out.SendLevelUpSound();

            // update color on levelup
            if (ObjectState == eObjectState.Active)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player == null) continue;
                    player.Out.SendEmoteAnimation(this, eEmote.LvlUp);
                }
            }

            // Reset taskDone per level.
            if (GameTask != null)
            {
                GameTask.TasksDone = 0;
                GameTask.SaveIntoDatabase();
            }

            DBCharacter.PlayedTimeSinceLevel = 0;

            // save player to database
            SaveIntoDatabase();
        }

        /// <summary>
        /// Called when this player reaches second stage of the current level
        /// </summary>
        public virtual void OnLevelSecondStage()
        {
            IsLevelSecondStage = true;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.SecondStage", Level), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            // spec points
            int specpoints = CharacterClass.SpecPointsMultiplier * Level / 20;
            if (specpoints > 0)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnLevelUp.YouGetSpec", specpoints), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            //death penalty reset on mini-ding
            DeathCount = 0;

            if (Group != null)
            {
                Group.UpdateGroupWindow();
            }
            Out.SendUpdatePlayer(); // Update player level
            Out.SendCharStatsUpdate(); // Update Stats and MaxHitpoints
            Out.SendUpdatePlayerSkills(true);
            Out.SendUpdatePoints();
            UpdatePlayerStatus();
            // save player to database
            SaveIntoDatabase();

            Out.SendLevelUpSound(); // level animation
            if (ObjectState == eObjectState.Active)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player == null) continue;
                    player.Out.SendEmoteAnimation(this, eEmote.LvlUp);
                }
            }
        }

        /// <summary>
        /// Calculate the Autotrain points.
        /// </summary>
        /// <param name="spec">Specialization</param>
        /// <param name="mode">various AT related calculations (amount of points, level of AT...)</param>
        public virtual int GetAutoTrainPoints(Specialization spec, int Mode)
        {
            int max_autotrain = Level / 4;
            if (max_autotrain == 0) max_autotrain = 1;

            foreach (string autotrainKey in CharacterClass.GetAutotrainableSkills())
            {
                if (autotrainKey == spec.KeyName)
                {
                    switch (Mode)
                    {
                        case 0:// return sum of all AT points in the spec
                        {
                            int pts_to_refund = Math.Min(max_autotrain, spec.Level);
                            return ((pts_to_refund * (pts_to_refund + 1) - 2) / 2);
                        }
                        case 1: // return max AT level + message
                        {
                            if (Level % 4 == 0)
                                if (spec.Level >= max_autotrain)
                                    return max_autotrain;
                                else
                                    Out.SendDialogBox(eDialogCode.SimpleWarning, 0, 0, 0, 0, eDialogType.Ok, true, LanguageMgr.GetTranslation(Client.Account.Language, "PlayerClass.OnLevelUp.Autotrain", spec.Name, max_autotrain));
                            return 0;
                        }
                        case 2: // return next free points due to AT change on levelup
                        {
                            if (spec.Level < max_autotrain)
                                return (spec.Level + 1);
                            else
                                return 0;
                        }
                        case 3: // return sum of all free AT points
                        {
                            if (spec.Level < max_autotrain)
                                return (((max_autotrain * (max_autotrain + 1) - 2) / 2) - ((spec.Level * (spec.Level + 1) - 2) / 2));
                            else
                                return ((max_autotrain * (max_autotrain + 1) - 2) / 2);
                        }
                        case 4: // spec is autotrainable
                        {
                            return 1;
                        }
                    }
                }
            }
            return 0;
        }
        #endregion

        #region Combat

        /// <summary>
        /// Gets/Sets safety flag
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual bool SafetyFlag
        {
            get { return DBCharacter != null ? DBCharacter.SafetyFlag : false; }
            set { if (DBCharacter != null) DBCharacter.SafetyFlag = value; }
        }

        /// <summary>
        /// Sets/gets the living's cloak hood state
        /// (delegate to PlayerCharacter)
        /// </summary>
        public override bool IsCloakHoodUp
        {
            get { return DBCharacter != null ? DBCharacter.IsCloakHoodUp : base.IsCloakHoodUp; }
            set
            {
                //base.IsCloakHoodUp = value; // only needed if some special code will be added in base-property in future
                DBCharacter.IsCloakHoodUp = value;

                Out.SendInventoryItemsUpdate(null);
                UpdateEquipmentAppearance();

                if (value)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.IsCloakHoodUp.NowWear"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.IsCloakHoodUp.NoLongerWear"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
        }

        /// <summary>
        /// Sets/gets the living's cloak visible state
        /// (delegate to PlayerCharacter)
        /// </summary>
        public override bool IsCloakInvisible
        {
            get
            {
                return DBCharacter != null ? DBCharacter.IsCloakInvisible : base.IsCloakInvisible;
            }
            set
            {
                DBCharacter.IsCloakInvisible = value;

                Out.SendInventoryItemsUpdate(null);
                UpdateEquipmentAppearance();

                if (value)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.IsCloakInvisible.Invisible"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.IsCloakInvisible.Visible"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
        }

        /// <summary>
        /// Sets/gets the living's helm visible state
        /// (delegate to PlayerCharacter)
        /// </summary>
        public override bool IsHelmInvisible
        {
            get
            {
                return DBCharacter != null ? DBCharacter.IsHelmInvisible : base.IsHelmInvisible;
            }
            set
            {
                DBCharacter.IsHelmInvisible = value;

                Out.SendInventoryItemsUpdate(null);
                UpdateEquipmentAppearance();

                if (value)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.IsHelmInvisible.Invisible"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.IsHelmInvisible.Visible"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
        }

        /// <summary>
        /// Gets or sets the players SpellQueue option
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual bool SpellQueue
        {
            get { return DBCharacter != null ? DBCharacter.SpellQueue : false; }
            set { if (DBCharacter != null) DBCharacter.SpellQueue = value; }
        }

        /// <summary>
        /// Switches the active weapon to another one
        /// </summary>
        /// <param name="slot">the new eActiveWeaponSlot</param>
        public override void SwitchWeapon(eActiveWeaponSlot slot)
        {
            if (attackComponent.AttackState)
                attackComponent.StopAttack();

            if (effectListComponent.ContainsEffectForEffectType(eEffect.Volley))
            {
                AtlasOF_VolleyECSEffect volley = (AtlasOF_VolleyECSEffect) EffectListService.GetEffectOnTarget(this, eEffect.Volley);
                volley?.OnPlayerSwitchedWeapon();
            }

            if (CurrentSpellHandler != null)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.SwitchWeapon.SpellCancelled"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                StopCurrentSpellcast();
            }

            foreach (Spell spell in ActivePulseSpells.Values)
            {
                if (spell.InstrumentRequirement != 0)
                {
                    ECSPulseEffect effect = EffectListService.GetPulseEffectOnTarget(this, spell);

                    if (effect != null)
                    {
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.SwitchWeapon.SpellCancelled"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                        effect.Stop();
                    }
                }
            }

            DbInventoryItem[] oldActiveSlots = new DbInventoryItem[4];
            DbInventoryItem[] newActiveSlots = new DbInventoryItem[4];
            DbInventoryItem rightHandSlot = Inventory.GetItem(eInventorySlot.RightHandWeapon);
            DbInventoryItem leftHandSlot = Inventory.GetItem(eInventorySlot.LeftHandWeapon);
            DbInventoryItem twoHandSlot = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
            DbInventoryItem distanceSlot = Inventory.GetItem(eInventorySlot.DistanceWeapon);

            // save old active weapons
            // simple active slot logic:
            // 0=right hand, 1=left hand, 2=two-hand, 3=range, F=none
            switch (VisibleActiveWeaponSlots & 0x0F)
            {
                case 0: oldActiveSlots[0] = rightHandSlot; break;
                case 2: oldActiveSlots[2] = twoHandSlot; break;
                case 3: oldActiveSlots[3] = distanceSlot; break;
            }

            if ((VisibleActiveWeaponSlots & 0xF0) == 0x10)
                oldActiveSlots[1] = leftHandSlot;

            base.SwitchWeapon(slot);

            // save new active slots
            switch (VisibleActiveWeaponSlots & 0x0F)
            {
                case 0: newActiveSlots[0] = rightHandSlot; break;
                case 2: newActiveSlots[2] = twoHandSlot; break;
                case 3: newActiveSlots[3] = distanceSlot; break;
            }

            if ((VisibleActiveWeaponSlots & 0xF0) == 0x10)
                newActiveSlots[1] = leftHandSlot;

            // unequip changed items
            for (int i = 0; i < 4; i++)
            {
                if (oldActiveSlots[i] != null && newActiveSlots[i] == null)
                    OnItemUnequipped(oldActiveSlots[i], (eInventorySlot) oldActiveSlots[i].SlotPosition);
            }

            // equip new active items
            for (int i = 0; i < 4; i++)
            {
                if (newActiveSlots[i] != null && oldActiveSlots[i] == null)
                    OnItemEquipped(newActiveSlots[i], (eInventorySlot) newActiveSlots[i].SlotPosition);
            }

            if (ObjectState == eObjectState.Active)
            {
                //Send new wield info, no items updated
                Out.SendInventorySlotsUpdate(null);
                // Update active weapon appearence (has to be done with all
                // equipment in the packet else player is naked)
                UpdateEquipmentAppearance();
            }
        }

        /// <summary>
        /// Switches the active quiver slot to another one
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="forced"></param>
        public virtual void SwitchQuiver(eActiveQuiverSlot slot, bool forced)
        {
            if (slot != eActiveQuiverSlot.None)
            {
                eInventorySlot updatedSlot = eInventorySlot.Invalid;
                if ((slot & eActiveQuiverSlot.Fourth) > 0)
                    updatedSlot = eInventorySlot.FourthQuiver;
                else if ((slot & eActiveQuiverSlot.Third) > 0)
                    updatedSlot = eInventorySlot.ThirdQuiver;
                else if ((slot & eActiveQuiverSlot.Second) > 0)
                    updatedSlot = eInventorySlot.SecondQuiver;
                else if ((slot & eActiveQuiverSlot.First) > 0)
                    updatedSlot = eInventorySlot.FirstQuiver;

                if (Inventory.GetItem(updatedSlot) != null && (rangeAttackComponent.ActiveQuiverSlot != slot || forced))
                {
                    rangeAttackComponent.ActiveQuiverSlot = slot;
                    //GamePlayer.SwitchQuiver.ShootWith:		You will shoot with: {0}.
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.SwitchQuiver.ShootWith", Inventory.GetItem(updatedSlot).GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    rangeAttackComponent.ActiveQuiverSlot = eActiveQuiverSlot.None;
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.SwitchQuiver.NoMoreAmmo"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                Out.SendInventorySlotsUpdate([updatedSlot]);
            }
            else
            {
                if (Inventory.GetItem(eInventorySlot.FirstQuiver) != null)
                    SwitchQuiver(eActiveQuiverSlot.First, true);
                else if (Inventory.GetItem(eInventorySlot.SecondQuiver) != null)
                    SwitchQuiver(eActiveQuiverSlot.Second, true);
                else if (Inventory.GetItem(eInventorySlot.ThirdQuiver) != null)
                    SwitchQuiver(eActiveQuiverSlot.Third, true);
                else if (Inventory.GetItem(eInventorySlot.FourthQuiver) != null)
                    SwitchQuiver(eActiveQuiverSlot.Fourth, true);
                else
                {
                    rangeAttackComponent.ActiveQuiverSlot = eActiveQuiverSlot.None;
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.SwitchQuiver.NotUseQuiver"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                Out.SendInventorySlotsUpdate(null);
            }
        }

        public override void OnAttackEnemy(AttackData ad)
        {
            LastPlayerActivityTime = GameLoop.GameLoopTime;
            base.OnAttackEnemy(ad);
        }

        /// <summary>
        /// This method is called at the end of the attack sequence to
        /// notify objects if they have been attacked/hit by an attack
        /// </summary>
        /// <param name="ad">information about the attack</param>
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsOnHorse && ad.IsHit)
                IsOnHorse = false;

            base.OnAttackedByEnemy(ad);

            if (ControlledBrain != null && ControlledBrain is ControlledMobBrain brain)
                brain.OnOwnerAttacked(ad);

            switch (ad.AttackResult)
            {
                // is done in game living because of guard
                //case eAttackResult.Blocked : Out.SendMessage(ad.Attacker.GetName(0, true) + " attacks you and you block the blow!", eChatType.CT_Missed, eChatLoc.CL_SystemWindow); break;
                case eAttackResult.Parried:
                    if (ad.Attacker is GameNPC)
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Parry", ad.Attacker.GetName(0, true, Client.Account.Language, (ad.Attacker as GameNPC))) + " (" + /*GetParryChance()*/ad.ParryChance.ToString("0.0") + "%)", eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    else
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Parry", ad.Attacker.GetName(0, true)) + " (" + /*GetParryChance()*/ad.ParryChance.ToString("0.0") + "%)", eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    break;
                case eAttackResult.Evaded:
                    if (ad.Attacker is GameNPC)
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Evade", ad.Attacker.GetName(0, true, Client.Account.Language, (ad.Attacker as GameNPC))) + " (" + /*GetEvadeChance()*/ad.EvadeChance.ToString("0.0") + "%)", eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    else
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Evade", ad.Attacker.GetName(0, true)) + " (" + /*GetEvadeChance()*/ad.EvadeChance.ToString("0.0") + "%)", eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    break;
                case eAttackResult.Fumbled:
                    if (ad.Attacker is GameNPC)
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Fumbled", ad.Attacker.GetName(0, true, Client.Account.Language, (ad.Attacker as GameNPC))), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    else
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Fumbled", ad.Attacker.GetName(0, true)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    break;
                case eAttackResult.Missed:
                    if (ad.AttackType == AttackData.eAttackType.Spell)
                        break;
                    if (ad.Attacker is GameNPC)
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Missed", ad.Attacker.GetName(0, true, Client.Account.Language, (ad.Attacker as GameNPC))) + " (" + Math.Min(ad.MissChance, 100).ToString("0.0") + "%)", eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    else
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Missed", ad.Attacker.GetName(0, true)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    break;
                case eAttackResult.HitStyle:
                case eAttackResult.HitUnstyled:
                {
                    // If attacked by a non-damaging spell, we should not show damage numbers.
                    // We need to check the damage on the spell here, not in the AD, since this could in theory be a damaging spell that had its damage modified to 0.
                    if (ad.AttackType is AttackData.eAttackType.Spell && ad.SpellHandler.Spell?.Damage == 0)
                        break;

                    string hitLocName = null;
                    switch (ad.ArmorHitLocation)
                    {
                        //GamePlayer.Attack.Location.Feet:	feet
                        // LanguageMgr.GetTranslation(Client.Account.Language, "", ad.Attacker.GetName(0, true))
                        case eArmorSlot.TORSO: hitLocName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Location.Torso"); break;
                        case eArmorSlot.ARMS: hitLocName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Location.Arm"); break;
                        case eArmorSlot.HEAD: hitLocName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Location.Head"); break;
                        case eArmorSlot.LEGS: hitLocName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Location.Leg"); break;
                        case eArmorSlot.HAND: hitLocName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Location.Hand"); break;
                        case eArmorSlot.FEET: hitLocName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.Location.Foot"); break;
                    }
                    string modmessage = string.Empty;
                    if (ad.Attacker is GamePlayer == false) // if attacked by player, don't show resists (?)
                    {
                        if (ad.Modifier > 0) modmessage = " (+" + ad.Modifier + ")";
                        if (ad.Modifier < 0) modmessage = " (" + ad.Modifier + ")";
                    }

                    if (ad.Attacker is GameNPC)
                    {
                        if (hitLocName != null)
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.HitsYour",
                                ad.Attacker.GetName(0, true, Client.Account.Language, (ad.Attacker as GameNPC)), hitLocName, ad.Damage, modmessage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                        else
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.HitsYou",
                                ad.Attacker.IsAlive ? ad.Attacker.GetName(0, true, Client.Account.Language, (ad.Attacker as GameNPC)) : "A dead enemy", ad.Damage, modmessage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);

                        if (ad.CriticalDamage > 0)
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.HitsYouCritical",
                                ad.Attacker.GetName(0, true, Client.Account.Language, (ad.Attacker as GameNPC)), ad.CriticalDamage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        if (hitLocName != null)
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.HitsYour", ad.Attacker.GetName(0, true), hitLocName, ad.Damage, modmessage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                        else
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.HitsYou", ad.Attacker.IsAlive ? ad.Attacker.GetName(0, true) : "A dead enemy", ad.Damage, modmessage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);

                        if (ad.CriticalDamage > 0)
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.HitsYouCritical", ad.Attacker.GetName(0, true), ad.CriticalDamage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                    }

                    break;
                }
            }

            // vampiir
            if (CharacterClass is PlayerClass.ClassVampiir)
            {
                GameSpellEffect removeEffect = SpellHandler.FindEffectOnTarget(this, "VampiirSpeedEnhancement");
                if (removeEffect != null)
                    removeEffect.Cancel(false);
            }

            if (IsCrafting)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //CraftTimer.Stop();
                craftComponent.StopCraft();
                CraftTimer = null;
                Out.SendCloseTimerWindow();
            }

            if (IsSalvagingOrRepairing)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                CraftTimer.Stop();
                CraftTimer = null;
                Out.SendCloseTimerWindow();
            }
        }
        /// <summary>
        /// Does needed interrupt checks and interrupts if needed
        /// </summary>
        /// <param name="attacker">the attacker that is interrupting</param>
        /// <param name="attackType">The attack type</param>
        /// <returns>true if interrupted successfully</returns>
        protected override bool CheckRangedAttackInterrupt(GameLiving attacker, AttackData.eAttackType attackType)
        {
            if (base.CheckRangedAttackInterrupt(attacker, attackType))
            {
                attackComponent.attackAction.OnAimInterrupt(attacker);
                return true;
            }

            return false;
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (Duel != null && !IsDuelPartner(source as GameLiving))
                Duel.Stop();

            base.TakeDamage(source, damageType, damageAmount, criticalAmount);

            if (HasAbility(Abilities.DefensiveCombatPowerRegeneration))
                Mana += (int)((damageAmount + criticalAmount) * 0.25);
        }

        public override int MeleeAttackRange
        {
            get
            {
                int range = 128;

                if (TargetObject is GameKeepComponent)
                    range += 150;
                else
                {
                    if (TargetObject is GameLiving target && target.IsMoving)
                        range += 32;

                    if (IsMoving)
                        range += 32;
                }

                return range;
            }
        }

        /// <summary>
        /// Gets the effective AF of this living.  This is used for the overall AF display
        /// on the character but not used in any damage equations.
        /// </summary>
        public override int EffectiveOverallAF
        {
            get
            {
                int eaf = 0;
                int abs = 0;
                foreach (DbInventoryItem item in Inventory.VisibleItems)
                {
                    double factor = 0;
                    switch (item.Item_Type)
                    {
                        case Slot.TORSO:
                            factor = 2.2;
                            break;
                        case Slot.LEGS:
                            factor = 1.3;
                            break;
                        case Slot.ARMS:
                            factor = 0.75;
                            break;
                        case Slot.HELM:
                            factor = 0.5;
                            break;
                        case Slot.HANDS:
                            factor = 0.25;
                            break;
                        case Slot.FEET:
                            factor = 0.25;
                            break;
                    }

                    int itemAFCap = Level << 1;
                    if (RealmLevel > 39)
                        itemAFCap += 2;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            abs = 0;
                            itemAFCap >>= 1;
                            break;
                        case eObjectType.Leather:
                            abs = 10;
                            break;
                        case eObjectType.Reinforced:
                            abs = 19;
                            break;
                        case eObjectType.Studded:
                            abs = 19;
                            break;
                        case eObjectType.Scale:
                            abs = 27;
                            break;
                        case eObjectType.Chain:
                            abs = 27;
                            break;
                        case eObjectType.Plate:
                            abs = 34;
                            break;
                    }

                    if (factor > 0)
                    {
                        int af = item.DPS_AF;
                        if (af > itemAFCap)
                            af = itemAFCap;
                        double piece_eaf = af * item.Quality / 100.0 * item.ConditionPercent / 100.0 * (1 + abs / 100.0);
                        eaf += (int)(piece_eaf * factor);
                    }
                }

                // Overall AF CAP = 10 * level * (1 + abs%/100)
                int bestLevel = -1;
                bestLevel = Math.Max(bestLevel, GetAbilityLevel(Abilities.AlbArmor));
                bestLevel = Math.Max(bestLevel, GetAbilityLevel(Abilities.HibArmor));
                bestLevel = Math.Max(bestLevel, GetAbilityLevel(Abilities.MidArmor));
                switch (bestLevel)
                {
                    default: abs = 0; break; // cloth etc
                    case ArmorLevel.Leather: abs = 10; break;
                    case ArmorLevel.Studded: abs = 19; break;
                    case ArmorLevel.Chain: abs = 27; break;
                    case ArmorLevel.Plate: abs = 34; break;
                }

                eaf += BaseBuffBonusCategory[eProperty.ArmorFactor]; // base buff before cap
                int eafcap = (int)(10 * Level * (1 + abs * 0.01));
                if (eaf > eafcap)
                    eaf = eafcap;
                eaf += (int)Math.Min(Level * 1.875, SpecBuffBonusCategory[eProperty.ArmorFactor])
                       - DebuffCategory[eProperty.ArmorFactor]
                       + OtherBonus[eProperty.ArmorFactor]
                       + Math.Min(Level, ItemBonus[eProperty.ArmorFactor]);

                eaf = (int)(eaf * BuffBonusMultCategory1.Get((int)eProperty.ArmorFactor));

                return eaf;
            }
        }
        /// <summary>
        /// Calc Armor hit location when player is hit by enemy
        /// </summary>
        /// <returns>slotnumber where enemy hits</returns>
        /// attackdata(ad) changed
        public virtual eArmorSlot CalculateArmorHitLocation(AttackData ad)
        {
            if (ad.Style != null)
            {
                if (ad.Style.ArmorHitLocation != eArmorSlot.NOTSET)
                    return ad.Style.ArmorHitLocation;
            }
            int chancehit = Util.Random(1, 100);
            if (chancehit <= 40)
            {
                return eArmorSlot.TORSO;
            }
            else if (chancehit <= 65)
            {
                return eArmorSlot.LEGS;
            }
            else if (chancehit <= 80)
            {
                return eArmorSlot.ARMS;
            }
            else if (chancehit <= 90)
            {
                return eArmorSlot.HEAD;
            }
            else if (chancehit <= 95)
            {
                return eArmorSlot.HAND;
            }
            else
            {
                return eArmorSlot.FEET;
            }
        }

        public override int WeaponSpecLevel(eObjectType objectType, int slotPosition)
        {
            // Use axe spec if left hand axe is not in the left hand slot.
            if (objectType is eObjectType.LeftAxe && slotPosition is not Slot.LEFTHAND)
                return GameServer.ServerRules.GetObjectSpecLevel(this, eObjectType.Axe);

            // Use left axe spec if axe is in the left hand slot.
            if (slotPosition is Slot.LEFTHAND && objectType is eObjectType.Axe)
                return GameServer.ServerRules.GetObjectSpecLevel(this, eObjectType.LeftAxe);

            return GameServer.ServerRules.GetObjectSpecLevel(this, objectType);
        }

        /// <summary>
        /// determines current weaponspeclevel
        /// </summary>
        public override int WeaponSpecLevel(DbInventoryItem weapon)
        {
            if (weapon == null)
                return 0;

            return WeaponSpecLevel((eObjectType) weapon.Object_Type, weapon.SlotPosition);
        }

        /// <summary>
        /// determines current weaponspeclevel
        /// </summary>
        public int WeaponBaseSpecLevel(eObjectType objectType, int slotPosition)
        {
            // Use axe spec if left hand axe is not in the left hand slot.
            if (objectType is eObjectType.LeftAxe && slotPosition is not Slot.LEFTHAND)
                return GameServer.ServerRules.GetObjectBaseSpecLevel(this, eObjectType.Axe);

            // Use left axe spec if axe is in the left hand slot.
            if (slotPosition is Slot.LEFTHAND && objectType is eObjectType.Axe)
                return GameServer.ServerRules.GetObjectBaseSpecLevel(this, eObjectType.LeftAxe);

            return GameServer.ServerRules.GetObjectBaseSpecLevel(this, objectType);
        }

        public int WeaponBaseSpecLevel(DbInventoryItem weapon)
        {
            if (weapon == null)
                return 0;

            return WeaponBaseSpecLevel((eObjectType) weapon.Object_Type, weapon.SlotPosition);
        }

        /// <summary>
        /// Gets the weaponskill of weapon
        /// </summary>
        public override double GetWeaponSkill(DbInventoryItem weapon)
        {
            if (weapon == null)
                return 0;

            int classBaseWeaponSkill = weapon.SlotPosition == (int)eInventorySlot.DistanceWeapon ? CharacterClass.WeaponSkillRangedBase : CharacterClass.WeaponSkillBase;
            double weaponSkill = Level * classBaseWeaponSkill / 200.0 * (1 + 0.01 * GetWeaponStat(weapon) / 2) * Effectiveness;
            return Math.Max(1, weaponSkill * GetModified(eProperty.WeaponSkill) * 0.01);
        }

        /// <summary>
        /// calculates weapon stat
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns></returns>
        public override int GetWeaponStat(DbInventoryItem weapon)
        {
            if (weapon != null)
            {
                switch ((eObjectType)weapon.Object_Type)
                {
                    // DEX modifier
                    case eObjectType.Staff:
                    case eObjectType.Fired:
                    case eObjectType.Longbow:
                    case eObjectType.Crossbow:
                    case eObjectType.CompositeBow:
                    case eObjectType.RecurvedBow:
                    case eObjectType.Thrown:
                    case eObjectType.Shield:
                        return GetModified(eProperty.Dexterity);

                    // STR+DEX modifier
                    case eObjectType.ThrustWeapon:
                    case eObjectType.Piercing:
                    case eObjectType.Spear:
                    case eObjectType.Flexible:
                    case eObjectType.HandToHand:
                        return (GetModified(eProperty.Strength) + GetModified(eProperty.Dexterity)) >> 1;
                }
            }
            // STR modifier for others
            return GetModified(eProperty.Strength);
        }

        /// <summary>
        /// calculate item armor factor influenced by quality, con and duration
        /// </summary>
        public override double GetArmorAF(eArmorSlot slot)
        {
            if (slot is eArmorSlot.NOTSET)
                return 0;

            DbInventoryItem item = Inventory.GetItem((eInventorySlot) slot);

            if (item == null)
                return 0;

            int characterLevel = Level;

            if (RealmLevel > 39)
                characterLevel++;

            int armorFactorCap = characterLevel * 2;
            double armorFactor = Math.Min(item.DPS_AF, (eObjectType) item.Object_Type is eObjectType.Cloth ? characterLevel : armorFactorCap);
            armorFactor += BaseBuffBonusCategory[eProperty.ArmorFactor] / 6.0; // Base AF buffs need to be applied manually for players.
            armorFactor *= item.Quality * 0.01 * item.Condition / item.MaxCondition; // Apply condition and quality before the second cap. Maybe incorrect, but it makes base AF buffs a little more useful.
            armorFactor = Math.Min(armorFactor, armorFactorCap);
            armorFactor += GetModified(eProperty.ArmorFactor) / 6.0; // Don't call base here.

            /*GameSpellEffect effect = SpellHandler.FindEffectOnTarget(this, typeof(VampiirArmorDebuff));
            if (effect != null && slot == (effect.SpellHandler as VampiirArmorDebuff).Slot)
                armorFactor -= (int) (effect.SpellHandler as VampiirArmorDebuff).Spell.Value;*/

            return Math.Max(0, armorFactor);
        }

        /// <summary>
        /// Calculates armor absorb level
        /// </summary>
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            if (slot == eArmorSlot.NOTSET)
                return 0;

            DbInventoryItem item = Inventory.GetItem((eInventorySlot)slot);

            if (item == null)
                return 0;

            // Debuffs can't lower absorb below 0%: https://darkageofcamelot.com/article/friday-grab-bag-08302019
            return Math.Clamp((item.SPD_ABS + GetModified(eProperty.ArmorAbsorption)) * 0.01, 0, 1);
        }

        /// <summary>
        /// Weaponskill thats shown to the player
        /// </summary>
        public virtual int DisplayedWeaponSkill
        {
            get
            {
                int itemBonus = WeaponSpecLevel(ActiveWeapon) - WeaponBaseSpecLevel(ActiveWeapon) - RealmLevel / 10;
                double m = 0.56 + itemBonus / 70.0;
                double weaponSpec = WeaponSpecLevel(ActiveWeapon) + itemBonus * m;
                double oldWStoNewWSScalar = (3 + .02 * GetWeaponStat(ActiveWeapon) ) /(1 + .005 * GetWeaponStat(ActiveWeapon));
                return (int)(GetWeaponSkill(ActiveWeapon) * (1.00 + weaponSpec * 0.01) * oldWStoNewWSScalar);
            }
        }

        /// <summary>
        /// Gets the weapondamage of currently used weapon
        /// Used to display weapon damage in stats, 16.5dps = 1650
        /// </summary>
        /// <param name="weapon">the weapon used for attack</param>
        public override double WeaponDamage(DbInventoryItem weapon)
        {
            if (weapon == null)
                return 0;

            return ApplyWeaponQualityAndConditionToDamage(weapon, WeaponDamageWithoutQualityAndCondition(weapon));
        }

        public double WeaponDamageWithoutQualityAndCondition(DbInventoryItem weapon)
        {
            if (weapon == null)
                return 0;

            double Dps = weapon.DPS_AF;

            // Apply dps cap before quality and condition.
            // http://www.classesofcamelot.com/faq.asp?mode=view&cat=10
            int dpsCap = 12 + 3 * Level;

            if (RealmLevel > 39)
                dpsCap += 3;

            if (Dps > dpsCap)
                Dps = dpsCap;

            Dps *= 1 + GetModified(eProperty.DPS) * 0.01;
            return Dps * 0.1;
        }

        public static double ApplyWeaponQualityAndConditionToDamage(DbInventoryItem weapon, double damage)
        {
            return damage * weapon.Quality * 0.01 * weapon.Condition / weapon.MaxCondition;
        }

        public override bool CanCastWhileAttacking()
        {
            switch (CharacterClass)
            {
                case ClassVampiir:
                case ClassMaulerAlb:
                case ClassMaulerMid:
                case ClassMaulerHib:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Stores the amount of realm points gained by other players on last death
        /// </summary>
        protected long m_lastDeathRealmPoints;

        /// <summary>
        /// Gets/sets the amount of realm points gained by other players on last death
        /// </summary>
        public long LastDeathRealmPoints
        {
            get { return m_lastDeathRealmPoints; }
            set { m_lastDeathRealmPoints = value; }
        }

        /// <summary>
        /// Method to broadcast Player to Discord
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="realm">The realm</param>
        public static void BroadcastDeathOnDiscord(string message, string name, string lastname, string playerClass, int level, long playedTime)
        {
            int color = 0;
            TimeSpan timeLived = TimeSpan.FromSeconds(playedTime);
            string timeLivedString = timeLived.Days + "d " + timeLived.Hours + "h " + timeLived.Minutes + "m ";

            string playerName = string.Empty;
            if (lastname != string.Empty)
                playerName = name + " " + lastname;
            else
                playerName = name;

            var DiscordObituaryHook = Properties.DISCORD_WEBHOOK_ID; // Make it a property later
            var client = new DiscordWebhookClient(DiscordObituaryHook);

            // Create your DiscordMessage with all parameters of your message.
            var discordMessage = new DiscordMessage(
                "",
                username: "Obituary",
                avatarUrl: "",
                tts: false,
                embeds: new[]
                {
                    new DiscordMessageEmbed(
                        author: new DiscordMessageEmbedAuthor(playerName),
                        color: color,
                        description: message,
                        fields: new[]
                        {
                            new DiscordMessageEmbedField("Level", level.ToString()),
                            new DiscordMessageEmbedField("Class", playerClass),
                            new DiscordMessageEmbedField("Time alive", timeLivedString)
                        }
                    )
                }
            );
            client.SendToDiscord(discordMessage);
        }

        public override void AddXPGainer(GameLiving xpGainer, double damageAmount)
        {
            // In case a player is attacked by a player of the same realm (e.g. in a duel, due to a confusion spell, a bug, etc.).
            // This also means the amount of damage dealt by the xp gainer won't be taken into account when awarding XP, RPs, BPs.
            if (xpGainer.Realm == Realm)
                return;

            base.AddXPGainer(xpGainer, damageAmount);
        }

        /// <summary>
        /// Called when the player dies
        /// </summary>
        /// <param name="killer">the killer</param>
        public override void ProcessDeath(GameObject killer)
        {
            // Ambient trigger upon killing player
            if (killer is GameNPC)
                (killer as GameNPC).FireAmbientSentence(GameNPC.eAmbientTrigger.killing, killer as GameLiving);

            CharacterClass.Die(killer);

            bool killingBlowByEnemyRealm = killer != null && killer.Realm != eRealm.None && killer.Realm != Realm;

            TargetObject = null;
            UpdateWaterBreathState(eWaterBreath.None);
            if (IsOnHorse)
                IsOnHorse = false;

            // cancel task if active
            if (GameTask != null && GameTask.TaskActive)
                GameTask.ExpireTask();

            string playerMessage;
            string publicMessage;
            ushort messageDistance = WorldMgr.DEATH_MESSAGE_DISTANCE;
            m_releaseType = eReleaseType.Normal;

            string location = string.Empty;
            if (CurrentAreas.Count > 0 && (CurrentAreas[0] is Area.BindArea) == false)
                location = (CurrentAreas[0] as AbstractArea).Description;
            else
                location = CurrentZone.Description;

            if (killer == null)
            {
                if (killingBlowByEnemyRealm)
                {
                    playerMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.KilledLocation", GetName(0, true), location);
                    publicMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.KilledLocation", GetName(0, true), location);
                }
                else
                {
                    playerMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.Killed", GetName(0, true));
                    publicMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.Killed", GetName(0, true));
                }
            }
            else
            {
                if (IsDuelPartner(killer as GameLiving))
                {
                    m_releaseType = eReleaseType.Duel;
                    messageDistance = WorldMgr.YELL_DISTANCE;
                    playerMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.DuelDefeated", GetName(0, true), killer.GetName(1, false));
                    publicMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.DuelDefeated", GetName(0, true), killer.GetName(1, false));
                }
                else
                {
                    messageDistance = 0;
                    if (killingBlowByEnemyRealm)
                    {
                        playerMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.KilledByLocation", GetName(0, true), killer.GetName(1, false), location);
                        publicMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.KilledByLocation", GetName(0, true), killer.GetName(1, false), location);
                    }
                    else
                    {
                        playerMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.KilledBy", GetName(0, true), killer.GetName(1, false));
                        publicMessage = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.KilledBy", GetName(0, true), killer.GetName(1, false));
                    }
                }
            }

            if (HCFlag)
            {
                playerMessage = "[HC Lv" + Level + "] " + playerMessage;
                publicMessage = "[HC Lv" + Level + "] " + publicMessage;

                if (Properties.DISCORD_ACTIVE && !string.IsNullOrEmpty(Properties.DISCORD_WEBHOOK_ID))
                {
                    BroadcastDeathOnDiscord(publicMessage, Name, LastName, CharacterClass.Name, Level, PlayedTime);
                }
            }

            Duel?.Stop();

            eChatType messageType;
            if (m_releaseType == eReleaseType.Duel)
                messageType = eChatType.CT_Emote;
            else if (killer == null)
            {
                messageType = eChatType.CT_PlayerDied;
            }
            else
            {
                switch ((eRealm)killer.Realm)
                {
                    case eRealm.Albion: messageType = eChatType.CT_KilledByAlb; break;
                    case eRealm.Midgard: messageType = eChatType.CT_KilledByMid; break;
                    case eRealm.Hibernia: messageType = eChatType.CT_KilledByHib; break;
                    default: messageType = eChatType.CT_PlayerDied; break; // killed by mob
                }
            }

            if (killer is GamePlayer && killer != this)
            {
                ((GamePlayer)killer).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)killer).Client.Account.Language, "GamePlayer.Die.YouKilled", GetName(0, false)), eChatType.CT_PlayerDied, eChatLoc.CL_SystemWindow);
                ((GamePlayer)killer).Out.SendMessage(playerMessage, messageType, eChatLoc.CL_SystemWindow);
            }

            List<GamePlayer> players;

            if (messageDistance == 0)
                players = ClientService.Instance.GetPlayersOfRegion(CurrentRegion);
            else
                players = GetPlayersInRadius(messageDistance);

            foreach (GamePlayer player in players)
            {
                // on normal server type send messages only to the killer and dead players realm
                // check for gameplayer is needed because killers realm don't see deaths by guards
                if (
                    (player != killer) && (
                        (killer != null && killer is GamePlayer && GameServer.ServerRules.IsSameRealm((GamePlayer)killer, player, true))
                        || (GameServer.ServerRules.IsSameRealm(this, player, true))
                        || (Properties.DEATH_MESSAGES_ALL_REALMS && (killer is GamePlayer || killer is GameKeepGuard))) //Only show Player/Guard kills if shown to all realms
                )
                    if (player == this)
                        player.Out.SendMessage(playerMessage, messageType, eChatLoc.CL_SystemWindow);
                    else player.Out.SendMessage(publicMessage, messageType, eChatLoc.CL_SystemWindow);
            }

            //Dead ppl. dismount ...
            if (Steed != null)
                DismountSteed(true);
            //Dead ppl. don't sit ...
            if (IsSitting)
            {
                IsSitting = false;
                UpdatePlayerStatus();
            }

            // then buffs drop messages
            base.ProcessDeath(killer);

            lock (_tradeLock)
            {
                if (m_releaseTimer != null)
                {
                    m_releaseTimer.Stop();
                    m_releaseTimer = null;
                }

                if (_quitTimer != null)
                {
                    _quitTimer.Stop();
                    _quitTimer = null;
                }

                if (m_healthRegenerationTimer != null)
                {
                    m_healthRegenerationTimer.Stop();
                    m_healthRegenerationTimer = null;
                }

                m_automaticRelease = m_releaseType == eReleaseType.Duel;
                m_releasePhase = 0;
                DeathTick = GameLoop.GameLoopTime; // we use realtime, because timer window is realtime

                Out.SendTimerWindow(LanguageMgr.GetTranslation(Client.Account.Language, "System.ReleaseTimer"), (m_automaticRelease ? RELEASE_MINIMUM_WAIT : RELEASE_TIME));
                m_releaseTimer = new ECSGameTimer(this);
                m_releaseTimer.Callback = new ECSGameTimer.ECSTimerCallback(ReleaseTimerCallback);
                m_releaseTimer.Start(1000);

                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.ReleaseToReturn"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);

                // clear target object so no more actions can used on this target, spells, styles, attacks...
                TargetObject = null;

                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player == null) continue;
                    player.Out.SendPlayerDied(this, killer);
                }

                // first penalty is 5% of expforlevel, second penalty comes from release
                int xpLossPercent;
                if (Level < 40)
                {
                    xpLossPercent = MAX_LEVEL - Level;
                }
                else
                {
                    xpLossPercent = MAX_LEVEL - 40;
                }

                if (killingBlowByEnemyRealm || InCombatPvP || killer?.Realm == Realm)
                {
                    LastDeathPvP = true;
                    int conPenalty = 0;

                    switch (GameServer.Instance.Configuration.ServerType)
                    {
                        case EGameServerType.GST_Normal:
                        {
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.DeadRVR"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);
                            xpLossPercent = 0;
                            m_deathtype = eDeathType.RvR;
                            break;
                        }
                        case EGameServerType.GST_PvP:
                        {
                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.DeadRVR"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);
                            xpLossPercent = 0;
                            m_deathtype = eDeathType.PvP;

                            // Live PvP servers have 3 con loss on pvp death.
                            if (Properties.PVP_DEATH_CON_LOSS)
                            {
                                conPenalty = 3;
                                TempProperties.SetProperty(DEATH_CONSTITUTION_LOSS_PROPERTY, conPenalty);
                            }

                            break;
                        }
                    }
                }
                else
                {
                    if (Level >= Properties.PVE_EXP_LOSS_LEVEL)
                    {
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.LoseExperience"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);
                        // if this is the first death in level, you lose only half the penalty
                        switch (DeathCount)
                        {
                            case 0:
                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.DeathN1"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);
                                xpLossPercent /= 3;
                                break;
                            case 1:
                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Die.DeathN2"), eChatType.CT_YouDied, eChatLoc.CL_SystemWindow);
                                xpLossPercent = xpLossPercent * 2 / 3;
                                break;
                        }

                        DeathCount++;
                        m_deathtype = eDeathType.PvE;
                        long xpLoss = (ExperienceForNextLevel - ExperienceForCurrentLevel) * xpLossPercent / 1000;
                        GainExperience(new(-xpLoss, 0, 0, 0, 0, 0, false, true, eXPSource.Other));
                        TempProperties.SetProperty(DEATH_EXP_LOSS_PROPERTY, xpLoss);
                    }

                    if (Level >= Properties.PVE_CON_LOSS_LEVEL)
                    {
                        int conLoss = DeathCount;
                        if (conLoss > 3)
                            conLoss = 3;
                        else if (conLoss < 1)
                            conLoss = 1;
                        TempProperties.SetProperty(DEATH_CONSTITUTION_LOSS_PROPERTY, conLoss);
                    }
                }
                GameEventMgr.AddHandler(this, GamePlayerEvent.Revive, new DOLEventHandler(OnRevive));
            }

            if (this.ControlledBrain != null)
                CommandNpcRelease();

            if (this.SiegeWeapon != null)
                SiegeWeapon.ReleaseControl();

            // sent after buffs drop
            // GamePlayer.Die.CorpseLies:		{0} just died. {1} corpse lies on the ground.
            Message.SystemToOthers2(this, eChatType.CT_PlayerDied, "GamePlayer.Die.CorpseLies", GetName(0, true), GetPronoun(this.Client, 1, true));

            if (m_releaseType == eReleaseType.Duel)
            {
                foreach (GamePlayer player in killer.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    if (player != killer)
                        // Message: {0} wins the duel!
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Duel.Die.KillerWinsDuel", killer.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
                }
                // Message: {0} wins the duel!
                //Message.SystemToOthers(Client, LanguageMgr.GetTranslation(this, "GamePlayer.Duel.Die.KillerWinsDuel", killer.Name), eChatType.CT_Emote);
            }

            // deal out exp and realm points based on server rules
            // no other way to keep correct message order...
            GameServer.ServerRules.OnPlayerKilled(this, killer);
            if (m_releaseType != eReleaseType.Duel)
                DeathTime = PlayedTime;

            CancelAllConcentrationEffects();
            //effectListComponent.CancelAll();

            IsSwimming = false;

            if (HCFlag)
                HardCoreLogin.HandleDeath(this);
        }

        public override void EnemyKilled(GameLiving enemy)
        {
            if (Group != null)
            {
                foreach (GamePlayer player in Group.GetPlayersInTheGroup())
                {
                    if (player == this)
                        continue;

                    if (enemy.attackComponent.AttackerTracker.ContainsAttacker(player))
                        continue;

                    if (IsWithinRadius(player, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                        Notify(GameLivingEvent.EnemyKilled, player, new EnemyKilledEventArgs(enemy));
                }
            }

            base.EnemyKilled(enemy);
        }

        public override bool InCombatPvE => base.InCombatPvE || ControlledBrain?.Body.InCombatPvE == true;
        public override bool InCombatPvP => base.InCombatPvP || ControlledBrain?.Body.InCombatPvP == true;
        public override bool InCombat => base.InCombat || ControlledBrain?.Body.InCombat == true;

        #endregion

        #region Duel

        public GameDuel Duel { get; private set; }
        public GamePlayer DuelPartner => Duel?.GetPartnerOf(this);

        public void OnDuelStart(GameDuel duel)
        {
            Duel?.Stop();
            Duel = duel;
        }

        public void OnDuelStop()
        {
            if (Duel == null)
                return;

            Duel = null;
        }

        public bool IsDuelPartner(GameLiving living)
        {
            if (living == null)
                return false;

            GamePlayer partner = DuelPartner;

            if (partner == null)
                return false;

            if (living is GameNPC npc && npc.Brain is ControlledMobBrain brain)
                living = brain.GetPlayerOwner();

            return partner == living;
        }

        #endregion

        #region Spell cast

        /// <summary>
        /// The time someone can not cast
        /// </summary>
        protected long m_disabledCastingTimeout = 0;
        /// <summary>
        /// Time when casting is allowed again (after interrupt from enemy attack)
        /// </summary>
        public long DisabledCastingTimeout
        {
            get { return m_disabledCastingTimeout; }
            set { m_disabledCastingTimeout = value; }
        }

        /// <summary>
        /// Grey out some skills on client for specified duration
        /// </summary>
        /// <param name="skill">the skill to disable</param>
        /// <param name="duration">duration of disable in milliseconds</param>
        public override void DisableSkill(Skill skill, int duration)
        {
            if (this.Client.Account.PrivLevel > 1)
                return;

            base.DisableSkill(skill, duration);
            List<Tuple<Skill, int>> disables = new();
            disables.Add(new Tuple<Skill, int>(skill, duration));
            Out.SendDisableSkill(disables);
        }

        /// <summary>
        /// Grey out collection of skills on client for specified duration
        /// </summary>
        /// <param name="skill">the skill to disable</param>
        /// <param name="duration">duration of disable in milliseconds</param>
        public override void DisableSkills(ICollection<Tuple<Skill, int>> skills)
        {
            if (this.Client.Account.PrivLevel > 1)
                return;

            base.DisableSkills(skills);
            Out.SendDisableSkill(skills);
        }

        /// <summary>
        /// The next spell
        /// </summary>
        protected Spell m_nextSpell;
        /// <summary>
        /// The next spell line
        /// </summary>
        protected SpellLine m_nextSpellLine;
        /// <summary>
        /// The next spell target
        /// </summary>
        protected GameLiving m_nextSpellTarget;

        /// <summary>
        /// Clears the spell queue when a player is interrupted
        /// </summary>
        public void ClearSpellQueue()
        {
            m_nextSpell = null;
            m_nextSpellLine = null;
            m_nextSpellTarget = null;
        }

        public override bool CastSpell(Spell spell, SpellLine line, ISpellCastingAbilityHandler spellCastingAbilityHandler = null, bool checkLos = true)
        {
            // Don't pass the current target to the casting component. It's supposed to be the one at cast time, not the one at queue time.
            return castingComponent.RequestCastSpell(spell, line, spellCastingAbilityHandler, null, checkLos);
        }

        public override bool CastSpell(ISpellCastingAbilityHandler ab)
        {
            bool casted = false;

            if (IsCrafting)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                craftComponent.StopCraft();
                CraftTimer = null;
                Out.SendCloseTimerWindow();
            }

            if (IsSalvagingOrRepairing)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                CraftTimer.Stop();
                CraftTimer = null;
                Out.SendCloseTimerWindow();
            }

            GameLiving target = TargetObject as GameLiving;
            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(this, ab.Spell, ab.SpellLine);

            if (spellHandler != null)
            {
                spellHandler.Ability = ab;
                casted = spellHandler.CheckBeginCast(target) && spellHandler.StartSpell(target);
            }
            else
                Out.SendMessage(ab.Spell.Name + " not implemented yet (" + ab.Spell.SpellType + ")", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            return casted;
        }

        public override int CalculateCastingTime(SpellHandler spellHandler)
        {
            int castTime;

            if (!CharacterClass.CanChangeCastingSpeed(spellHandler.SpellLine, spellHandler.Spell))
                castTime = spellHandler.Spell.CastTime;
            else
                castTime = base.CalculateCastingTime(spellHandler);

            if (UseDetailedCombatLog)
                Out.SendMessage($"Casting Speed: {castTime * 0.001:0.##}s", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

            return castTime;
        }

        #endregion

        #region Realm Abilities
        /// <summary>
        /// This is the timer used to count time when a player casts a RA
        /// </summary>
        private ECSGameTimer m_realmAbilityCastTimer;

        /// <summary>
        /// Get and set the RA cast timer
        /// </summary>
        public ECSGameTimer RealmAbilityCastTimer
        {
            get { return m_realmAbilityCastTimer; }
            set { m_realmAbilityCastTimer = value; }
        }

        /// <summary>
        /// Does the player is casting a realm ability
        /// </summary>
        public bool IsCastingRealmAbility
        {
            get { return (m_realmAbilityCastTimer != null && m_realmAbilityCastTimer.IsAlive); }
        }
        #endregion

        #region Vault/Money/Items/Trading/UseSlot/ApplyPoison

        private IGameInventoryObject m_activeInventoryObject;

        public AccountVault AccountVault { get; private set; }

        /// <summary>
        /// The currently active InventoryObject
        /// </summary>
        public IGameInventoryObject ActiveInventoryObject
        {
            get { return m_activeInventoryObject; }
            set	{ m_activeInventoryObject = value; }
        }

        /// <summary>
        /// Property that holds tick when charged item was used last time
        /// </summary>
        public const string LAST_CHARGED_ITEM_USE_TICK = "LastChargedItemUsedTick";
        public const string ITEM_USE_DELAY = "ItemUseDelay";
        public const string NEXT_POTION_AVAIL_TIME = "LastPotionItemUsedTick";
        public const string NEXT_SPELL_AVAIL_TIME_BECAUSE_USE_POTION = "SpellAvailableTime";

        /// <summary>
        /// Called when this player receives a trade item
        /// </summary>
        /// <param name="source">the source of the item</param>
        /// <param name="item">the item</param>
        /// <returns>true to accept, false to deny the item</returns>
        public virtual bool ReceiveTradeItem(GamePlayer source, DbInventoryItem item)
        {
            if (source == null || item == null || source == this)
                return false;

            lock (_tradeLock)
            {
                lock (source)
                {
                    if ((TradeWindow != null && source != TradeWindow.Partner) || (TradeWindow == null && !OpenTrade(source)))
                    {
                        if (TradeWindow != null)
                        {
                            GamePlayer partner = TradeWindow.Partner;
                            if (partner == null)
                            {
                                source.Out.SendMessage(Name + " is still selfcrafting.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                source.Out.SendMessage(Name + " is still trading with " + partner.Name + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                        }
                        else if (source.TradeWindow != null)
                        {
                            GamePlayer sourceTradePartner = source.TradeWindow.Partner;
                            if (sourceTradePartner == null)
                            {
                                source.Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ReceiveTradeItem.StillSelfcrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                source.Out.SendMessage("You are still trading with " + sourceTradePartner.Name + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                        }
                        return false;
                    }
                    if (item.IsTradable == false && source.CanTradeAnyItem == false && TradeWindow.Partner.CanTradeAnyItem == false)
                    {
                        source.Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ReceiveTradeItem.CantTrade"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    if (!source.TradeWindow.AddItemToTrade(item))
                    {
                        source.Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ReceiveTradeItem.CantTrade"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Called when the player receives trade money
        /// </summary>
        /// <param name="source">the source</param>
        /// <param name="money">the money value</param>
        /// <returns>true to accept, false to deny</returns>
        public virtual bool ReceiveTradeMoney(GamePlayer source, long money)
        {
            if (source == null || source == this || money == 0)
                return false;

            lock (_tradeLock)
            {
                lock (source)
                {
                    if ((TradeWindow != null && source != TradeWindow.Partner) || (TradeWindow == null && !OpenTrade(source)))
                    {
                        if (TradeWindow != null)
                        {
                            GamePlayer partner = TradeWindow.Partner;
                            if (partner == null)
                            {
                                source.Out.SendMessage(Name + " is still selfcrafting.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                source.Out.SendMessage(Name + " is still trading with " + partner.Name + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                        }
                        else if (source.TradeWindow != null)
                        {
                            GamePlayer sourceTradePartner = source.TradeWindow.Partner;
                            if (sourceTradePartner == null)
                            {
                                source.Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ReceiveTradeItem.StillSelfcrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                source.Out.SendMessage("You are still trading with " + sourceTradePartner.Name + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                        }
                        return false;
                    }

                    source.TradeWindow.AddMoneyToTrade(money);
                    return true;
                }
            }
        }

        #region Money

        /// <summary>
        /// Player Mithril Amount
        /// </summary>
        public virtual int Mithril { get { return m_Mithril; } protected set { m_Mithril = value; if (DBCharacter != null) DBCharacter.Mithril = m_Mithril; }}
        protected int m_Mithril = 0;

        /// <summary>
        /// Player Platinum Amount
        /// </summary>
        public virtual int Platinum { get { return m_Platinum; } protected set { m_Platinum = value; if (DBCharacter != null) DBCharacter.Platinum = m_Platinum; }}
        protected int m_Platinum = 0;

        /// <summary>
        /// Player Gold Amount
        /// </summary>
        public virtual int Gold { get { return m_Gold; } protected set { m_Gold = value; if (DBCharacter != null) DBCharacter.Gold = m_Gold; }}
        protected int m_Gold = 0;

        /// <summary>
        /// Player Silver Amount
        /// </summary>
        public virtual int Silver { get { return m_Silver; } protected set { m_Silver = value; if (DBCharacter != null) DBCharacter.Silver = m_Silver; }}
        protected int m_Silver = 0;

        /// <summary>
        /// Player Copper Amount
        /// </summary>
        public virtual int Copper { get { return m_Copper; } protected set { m_Copper = value; if (DBCharacter != null) DBCharacter.Copper = m_Copper; }}
        protected int m_Copper = 0;

        /// <summary>
        /// Gets the money value this player owns
        /// </summary>
        /// <returns></returns>
        public virtual long GetCurrentMoney()
        {
            return Money.GetMoney(Mithril, Platinum, Gold, Silver, Copper);
        }

        public long ApplyGuildDues(long money)
        {
            Guild guild = Guild;

            if (guild == null || !guild.IsGuildDuesOn())
                return money;

            long moneyToGuild = money * guild.GetGuildDuesPercent() / 100;

            if (moneyToGuild <= 0 || !guild.AddToBank(moneyToGuild, false))
                return money;

            return money - moneyToGuild;
        }

        /// <summary>
        /// Adds money to this player
        /// </summary>
        /// <param name="money">money to add</param>
        public virtual void AddMoney(long money)
        {
            AddMoney(money, null, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// Adds money to this player
        /// </summary>
        /// <param name="money">money to add</param>
        /// <param name="messageFormat">null if no message or "text {0} text"</param>
        public virtual void AddMoney(long money, string messageFormat)
        {
            AddMoney(money, messageFormat, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// Adds money to this player
        /// </summary>
        /// <param name="money">money to add</param>
        /// <param name="messageFormat">null if no message or "text {0} text"</param>
        /// <param name="ct">message chat type</param>
        /// <param name="cl">message chat location</param>
        public virtual void AddMoney(long money, string messageFormat, eChatType ct, eChatLoc cl)
        {
            long newMoney = GetCurrentMoney() + money;

            Copper = Money.GetCopper(newMoney);
            Silver = Money.GetSilver(newMoney);
            Gold = Money.GetGold(newMoney);
            Platinum = Money.GetPlatinum(newMoney);
            Mithril = Money.GetMithril(newMoney);

            Out.SendUpdateMoney();

            if (messageFormat != null)
                Out.SendMessage(string.Format(messageFormat, Money.GetString(money)), ct, cl);
        }

        /// <summary>
        /// Removes money from the player
        /// </summary>
        /// <param name="money">money value to subtract</param>
        /// <returns>true if successfull, false if player doesn't have enough money</returns>
        public virtual bool RemoveMoney(long money)
        {
            return RemoveMoney(money, null, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// Removes money from the player
        /// </summary>
        /// <param name="money">money value to subtract</param>
        /// <param name="messageFormat">null if no message or "text {0} text"</param>
        /// <returns>true if successfull, false if player doesn't have enough money</returns>
        public virtual bool RemoveMoney(long money, string messageFormat)
        {
            return RemoveMoney(money, messageFormat, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// Removes money from the player
        /// </summary>
        /// <param name="money">money value to subtract</param>
        /// <param name="messageFormat">null if no message or "text {0} text"</param>
        /// <param name="ct">message chat type</param>
        /// <param name="cl">message chat location</param>
        /// <returns>true if successfull, false if player doesn't have enough money</returns>
        public virtual bool RemoveMoney(long money, string messageFormat, eChatType ct, eChatLoc cl)
        {
            if (money > GetCurrentMoney())
                return false;

            long newMoney = GetCurrentMoney() - money;

            Mithril = Money.GetMithril(newMoney);
            Platinum = Money.GetPlatinum(newMoney);
            Gold = Money.GetGold(newMoney);
            Silver = Money.GetSilver(newMoney);
            Copper = Money.GetCopper(newMoney);

            Out.SendUpdateMoney();

            if (messageFormat != null && money != 0)
            {
                Out.SendMessage(string.Format(messageFormat, Money.GetString(money)), ct, cl);
            }
            return true;
        }
        #endregion

        private DbInventoryItem m_useItem;

        /// <summary>
        /// The item the player is trying to use.
        /// </summary>
        public DbInventoryItem UseItem
        {
            get { return m_useItem; }
            set { m_useItem = value; }
        }

        /// <summary>
        /// Called when the player uses an inventory in a slot
        /// eg. by clicking on the icon in the qickbar dragged from a slot
        /// </summary>
        /// <param name="slot">inventory slot used</param>
        /// <param name="type">type of slot use (0=simple click on icon, 1=use, 2=/use2)</param>
        public virtual void UseSlot(eInventorySlot slot, eUseType type)
        {
            UseSlot((int)slot, (int)type);
        }

        public virtual void UseSlot(int slot, int type)
        {
            if (!IsAlive)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.CantFire"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            lock (Inventory.Lock)
            {
                DbInventoryItem useItem = Inventory.GetItem((eInventorySlot) slot);
                UseItem = useItem;

                if (useItem == null)
                {
                    if (slot is >= Slot.FIRSTQUIVER and <= Slot.FOURTHQUIVER)
                        Out.SendMessage($"The quiver slot {slot - Slot.FIRSTQUIVER + 1} is empty!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    else
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.IllegalSourceObject", slot), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    return;
                }

                if (useItem is IGameInventoryItem inventoryItem)
                {
                    if (inventoryItem.Use(this))
                        return;
                }

                if (useItem.Item_Type is >= ((int) eInventorySlot.LeftFrontSaddleBag) and <= ((int) eInventorySlot.RightRearSaddleBag))
                {
                    UseSaddleBag(useItem);
                    return;
                }

                if (useItem.Item_Type != Slot.RANGED && (slot != Slot.HORSE || type != 0))
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.AttemptToUse", useItem.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                switch (slot)
                {
                    case Slot.HORSEARMOR:
                    case Slot.HORSEBARDING:
                        return;
                    case Slot.HORSE:
                    {
                        if (type != 0)
                            break;

                        if (IsOnHorse)
                            IsOnHorse = false;
                        else
                        {
                            if (Level < useItem.Level)
                            {
                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.SummonHorseLevel", useItem.Level), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            string reason = GameServer.ServerRules.ReasonForDisallowMounting(this);

                            if (!string.IsNullOrEmpty(reason))
                            {
                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, reason), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (IsSummoningMount)
                            {
                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.StopCallingMount"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                StopWhistleTimers();
                                return;
                            }

                            Out.SendTimerWindow("Summoning Mount", 5);

                            foreach (GamePlayer plr in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            {
                                if (plr == null)
                                    continue;
                                plr.Out.SendEmoteAnimation(this, eEmote.Horse_whistle);
                            }

                            GameSpellEffect effects = SpellHandler.FindEffectOnTarget(this, "VampiirSpeedEnhancement");
                            ECSGameEffect effect = EffectListService.GetEffectOnTarget(this, eEffect.MovementSpeedBuff);

                            effects?.Cancel(false);
                            effect?.Stop();

                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.WhistleMount"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
                            m_whistleMountTimer = new(this, new ECSGameTimer.ECSTimerCallback(WhistleMountTimerCallback), 5000);
                        }

                        break;
                    }
                    case Slot.RIGHTHAND:
                    case Slot.LEFTHAND:
                    {
                        if (type != 0 || ActiveWeaponSlot == eActiveWeaponSlot.Standard)
                            break;

                        SwitchWeapon(eActiveWeaponSlot.Standard);
                        Notify(GamePlayerEvent.UseSlot, this, new UseSlotEventArgs(slot, type));
                        return;
                    }
                    case Slot.TWOHAND:
                    {
                        if (type != 0 || ActiveWeaponSlot == eActiveWeaponSlot.TwoHanded)
                            break;

                        SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        Notify(GamePlayerEvent.UseSlot, this, new UseSlotEventArgs(slot, type));
                        return;
                    }
                    case Slot.RANGED:
                    {
                        bool newAttack = false;
                        ECSGameEffect volley = EffectListService.GetEffectOnTarget(this, eEffect.Volley);

                        if (ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                        {
                            SwitchWeapon(eActiveWeaponSlot.Distance);

                            if(useItem.Object_Type == (int) eObjectType.Instrument)
                                return;
                        }
                        else if (!attackComponent.AttackState && useItem.Object_Type != (int) eObjectType.Instrument && volley == null)
                        {
                            StopCurrentSpellcast();
                            attackComponent.RequestStartAttack();
                            newAttack = true;
                        }

                        if (!attackComponent.AttackState && volley == null)
                        {
                            rangeAttackComponent.RangedAttackState = eRangedAttackState.None;
                            rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;
                        }

                        if (!newAttack && rangeAttackComponent.RangedAttackState != eRangedAttackState.None && volley == null)
                        {
                            if (rangeAttackComponent.RangedAttackState == eRangedAttackState.ReadyToFire)
                            {
                                rangeAttackComponent.AutoFireTarget = null;
                                rangeAttackComponent.RangedAttackState = eRangedAttackState.Fire;
                                StopCurrentSpellcast();
                            }
                            else if (rangeAttackComponent.RangedAttackState == eRangedAttackState.Aim)
                            {
                                if (!TargetInView)
                                {
                                    if (volley == null)
                                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.CantSeeTarget"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                }
                                else
                                {
                                    rangeAttackComponent.AutoFireTarget = TargetObject;
                                    rangeAttackComponent.RangedAttackState = eRangedAttackState.AimFire;
                                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.AutoReleaseShot"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                }
                            }
                            else if (rangeAttackComponent.RangedAttackState == eRangedAttackState.AimFire)
                            {
                                rangeAttackComponent.RangedAttackState = eRangedAttackState.AimFireReload;
                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.AutoReleaseShotReload"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            else if (rangeAttackComponent.RangedAttackState == eRangedAttackState.AimFireReload)
                            {
                                rangeAttackComponent.RangedAttackState = eRangedAttackState.Aim;
                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.NoAutoReleaseShotReload"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                        }

                        break;
                    }
                    case Slot.FIRSTQUIVER: SwitchQuiver(eActiveQuiverSlot.First, false);
                        break;
                    case Slot.SECONDQUIVER: SwitchQuiver(eActiveQuiverSlot.Second, false);
                        break;
                    case Slot.THIRDQUIVER: SwitchQuiver(eActiveQuiverSlot.Third, false);
                        break;
                    case Slot.FOURTHQUIVER: SwitchQuiver(eActiveQuiverSlot.Fourth, false);
                        break;
                }

                if (useItem.SpellID != 0 || useItem.SpellID1 != 0 || useItem.PoisonSpellID != 0)
                {
                    if (IsSitting && useItem.Object_Type != (int) eObjectType.Poison)
                    {
                        Out.SendMessage("You can't use an item while sitting!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    // Item with a non-charge ability.
                    if (useItem.Object_Type == (int) eObjectType.Magical &&
                        useItem.Item_Type == (int) eInventorySlot.FirstBackpack &&
                        useItem.SpellID > 0 &&
                        useItem.MaxCharges == 0)
                    {
                        UseMagicalItem(useItem, type);
                        return;
                    }

                    // Artifacts don't require charges.
                    if ((type < 2 && useItem.SpellID > 0 && useItem.Charges < 1 && useItem.MaxCharges > -1) ||
                        (type == 2 && useItem.SpellID1 > 0 && useItem.Charges1 < 1 && useItem.MaxCharges1 > -1) ||
                        (useItem.PoisonSpellID > 0 && useItem.PoisonCharges < 1))
                    {
                        Out.SendMessage($"The {useItem.Name} is out of charges.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }
                    else
                    {
                        if (useItem.Object_Type == (int) eObjectType.Poison)
                        {
                            DbInventoryItem mainHand = ActiveWeapon;
                            DbInventoryItem leftHand = ActiveLeftWeapon;

                            if (mainHand != null && mainHand.PoisonSpellID == 0)
                                ApplyPoison(useItem, mainHand);
                            else if (leftHand != null && leftHand.PoisonSpellID == 0)
                                ApplyPoison(useItem, leftHand);
                        }
                        else if (useItem.SpellID > 0 && useItem.Charges > 0 && useItem.Object_Type == (int) eObjectType.Magical && (useItem.Item_Type == (int) eInventorySlot.FirstBackpack || useItem.Item_Type == 41))
                        {
                            SpellLine potionEffectLine = SkillBase.GetSpellLine(GlobalSpellsLines.Potions_Effects);

                            if (useItem.Item_Type == 41)
                                potionEffectLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);

                            Spell spell = SkillBase.FindSpell(useItem.SpellID, potionEffectLine);

                            if (spell != null)
                            {
                                // For potions most can be used by any player level except a few higher level ones.
                                // So for the case of potions we will only restrict the level of usage if LevelRequirement is >0 for the item.
                                long nextPotionAvailTime = TempProperties.GetProperty<long>($"{NEXT_POTION_AVAIL_TIME}_Type{spell.SharedTimerGroup}");

                                if (Client.Account.PrivLevel == 1 && nextPotionAvailTime > CurrentRegion.Time)
                                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.MustWaitBeforeUse", (nextPotionAvailTime - CurrentRegion.Time) / 1000), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                else
                                {
                                    if (potionEffectLine != null)
                                    {
                                        int requiredLevel = useItem.Template.LevelRequirement > 0 ? useItem.Template.LevelRequirement : Math.Min(MAX_LEVEL, useItem.Level);

                                        if (requiredLevel <= Level)
                                        {
                                            if (spell.CastTime > 0 && attackComponent.AttackState)
                                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.CantUseInCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                            else if ((IsStunned && !(Steed != null && Steed.Name == "Forceful Zephyr")) || IsMezzed || !IsAlive)
                                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.CantUseState", useItem.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                            else if (spell.CastTime > 0 && IsCasting)
                                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.CantUseCast", useItem.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                            else
                                            {
                                                if (ScriptMgr.CreateSpellHandler(this, spell, potionEffectLine) is SpellHandler spellHandler)
                                                {
                                                    if (spell.IsHealing && spell.CastTime == 0 && IsAttacking)
                                                    {
                                                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.CantUseAttacking", useItem.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                                        return;
                                                    }

                                                    Stealth(false);

                                                    if (useItem.Item_Type == (int) eInventorySlot.FirstBackpack)
                                                    {
                                                        Emote(eEmote.Drink);

                                                        if (spell.CastTime > 0)
                                                            TempProperties.SetProperty(NEXT_SPELL_AVAIL_TIME_BECAUSE_USE_POTION, 6 * 1000 + CurrentRegion.Time);
                                                    }

                                                    if (spellHandler.StartSpell(this, useItem))
                                                    {
                                                        if (useItem.Count > 1)
                                                        {
                                                            Inventory.RemoveCountFromStack(useItem, 1);
                                                            InventoryLogging.LogInventoryAction(this, "(potion)", eInventoryActionType.Other, useItem.Template);
                                                        }
                                                        else
                                                        {
                                                            useItem.Charges--;

                                                            if (useItem.Charges < 1)
                                                            {
                                                                Inventory.RemoveCountFromStack(useItem, 1);
                                                                InventoryLogging.LogInventoryAction(this, "(potion)", eInventoryActionType.Other, useItem.Template);
                                                            }
                                                        }

                                                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.Used", useItem.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                                        TempProperties.SetProperty($"{NEXT_POTION_AVAIL_TIME}_Type{spell.SharedTimerGroup}", useItem.CanUseEvery * 1000 + CurrentRegion.Time);
                                                    }
                                                }
                                                else
                                                    Out.SendMessage($"Potion effect ID {spell.ID} is not implemented yet.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                            }
                                        }
                                        else
                                            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.NotEnouthPower"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    }
                                    else
                                        Out.SendMessage("Potion effect line not found", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                }
                            }
                            else
                                Out.SendMessage($"Potion effect spell ID {useItem.SpellID} not found.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        }
                        else if (type > 0)
                        {
                            if (!Inventory.EquippedItems.Contains(useItem))
                                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.UseSlot.CantUseFromBackpack"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            else
                            {
                                long lastChargedItemUseTick = TempProperties.GetProperty<long>(LAST_CHARGED_ITEM_USE_TICK);
                                long changeTime = CurrentRegion.Time - lastChargedItemUseTick;
                                long delay = TempProperties.GetProperty<long>(ITEM_USE_DELAY);
                                long itemDelay = TempProperties.GetProperty<long>("ITEMREUSEDELAY" + useItem.Id_nb);
                                long itemReuse = (long)useItem.CanUseEvery * 1000;

                                if (itemDelay == 0)
                                    itemDelay = CurrentRegion.Time - itemReuse;

                                if ((IsStunned && !(Steed != null && Steed.Name == "Forceful Zephyr")) || IsMezzed || !IsAlive)
                                    Out.SendMessage("In your state you can't discharge any object.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                else if ((type == 1 && SelfBuffChargeIDs.Contains(useItem.SpellID)) || (type == 2 && SelfBuffChargeIDs.Contains(useItem.SpellID1)))
                                    UseItemCharge(useItem, type);
                                else if (Client.Account.PrivLevel == 1 && (changeTime < delay || (CurrentRegion.Time - itemDelay) < itemReuse)) //2 minutes reuse timer
                                {
                                    if ((CurrentRegion.Time - itemDelay) < itemReuse)
                                        Out.SendMessage($"You must wait {(itemReuse - (CurrentRegion.Time - itemDelay)) / 1000} more second before discharge {useItem.Name}!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    else
                                        Out.SendMessage($"You must wait {(delay - changeTime) / 1000} more second before discharge another object!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                                    return;
                                }
                                else
                                {
                                    if (type == 1)
                                    {
                                        if (useItem.SpellID == 0)
                                            return;

                                        UseItemCharge(useItem, type);
                                    }
                                    else if (type == 2)
                                    {
                                        if (useItem.SpellID1 == 0)
                                            return;

                                        UseItemCharge(useItem, type);
                                    }
                                }
                            }
                        }
                    }
                }

                Notify(GamePlayerEvent.UseSlot, this, new UseSlotEventArgs(slot, type));
            }
        }

        /// <summary>
        /// Player is using a saddle bag to open up slots on a mount
        /// </summary>
        /// <param name="useItem"></param>
        protected virtual void UseSaddleBag(DbInventoryItem useItem)
        {
            eHorseSaddleBag bag = eHorseSaddleBag.None;

            switch ((eInventorySlot)useItem.Item_Type)
            {
                case eInventorySlot.LeftFrontSaddleBag:
                    if (ChampionLevel >= 2)
                    {
                        bag = eHorseSaddleBag.LeftFront;
                    }
                    else
                    {
                        Out.SendMessage("This saddlebag requires Champion Level 2!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case eInventorySlot.RightFrontSaddleBag:
                    if (ChampionLevel >= 3)
                    {
                        bag = eHorseSaddleBag.RightFront;
                    }
                    else
                    {
                        Out.SendMessage("This saddlebag requires Champion Level 3!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case eInventorySlot.LeftRearSaddleBag:
                    if (ChampionLevel >= 4)
                    {
                        bag = eHorseSaddleBag.LeftRear;
                    }
                    else
                    {
                        Out.SendMessage("This saddlebag requires Champion Level 4!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case eInventorySlot.RightRearSaddleBag:
                    if (ChampionLevel >= 5)
                    {
                        bag = eHorseSaddleBag.RightRear;
                    }
                    else
                    {
                        Out.SendMessage("This saddlebag requires Champion Level 5!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    }
                    break;
            }

            if (bag != eHorseSaddleBag.None)
            {
                if ((ActiveSaddleBags & (byte)bag) == 0)
                {
                    if (Inventory.RemoveItem(useItem))
                    {
                        InventoryLogging.LogInventoryAction(this, "(HorseSaddleBag)", eInventoryActionType.Other, useItem.Template, useItem.Count);
                        ActiveSaddleBags |= (byte)bag;
                        Out.SendSetControlledHorse(this);
                        Out.SendMessage("You've activated a saddlebag!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        SaveIntoDatabase();
                    }
                    else
                    {
                        Out.SendMessage("An error occurred while trying to activate this saddlebag!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    }
                }
                else
                {
                    Out.SendMessage("You've already activated this saddlebag!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                }
            }
        }

        private const int NUM_SLOTS_PER_SADDLEBAG = 4;

        /// <summary>
        /// Can player use this horse inventory slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public virtual bool CanUseHorseInventorySlot(int slot)
        {
            if (Inventory.GetItem(eInventorySlot.Horse) == null)
            {
                Out.SendMessage("You must be equipped with a horse.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (IsOnHorse == false)
            {
                Out.SendMessage("You must be on your horse to use this inventory.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (ChampionLevel == 0)
            {
                Out.SendMessage("You must be a champion to use this inventory.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (slot < (int)eInventorySlot.FirstBagHorse || slot > (int)eInventorySlot.LastBagHorse || (ChampionLevel >= 5 && ActiveSaddleBags == (byte)eHorseSaddleBag.All))
            {
                return true;
            }

            try
            {
                eHorseSaddleBag saddleBagRequired = (eHorseSaddleBag)Enum.GetValues(typeof(eHorseSaddleBag)).GetValue(((slot / NUM_SLOTS_PER_SADDLEBAG) - 19)); // 1, 2, 3, or 4

                // ChatUtil.SendDebugMessage(this, string.Format("Check slot {0} if between {1} and {2}.  CL is {3}, ActiveSaddleBags is {4}, Required Bag is {5}", slot, (int)eInventorySlot.FirstBagHorse, (int)eInventorySlot.LastBagHorse, ChampionLevel, ActiveSaddleBags, saddleBagRequired));

                if ((ActiveSaddleBags & (byte)saddleBagRequired) > 0)
                {
                    if (ChampionLevel >= 2 && slot < (int)eInventorySlot.FirstBagHorse + NUM_SLOTS_PER_SADDLEBAG)
                    {
                        return true;
                    }
                    else if (ChampionLevel >= 3 && slot < (int)eInventorySlot.FirstBagHorse + NUM_SLOTS_PER_SADDLEBAG * 2)
                    {
                        return true;
                    }
                    else if (ChampionLevel >= 4 && slot < (int)eInventorySlot.FirstBagHorse + NUM_SLOTS_PER_SADDLEBAG * 3)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                ChatUtil.SendDebugMessage(this, "CanSeeInventory: " + ex.Message);
            }

            Out.SendMessage("You can't use this inventory.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }


        /// <summary>
        /// Use a charged ability on an item
        /// </summary>
        /// <param name="useItem"></param>
        /// <param name="type">1 == use1, 2 == use2</param>
        protected virtual void UseItemCharge(DbInventoryItem useItem, int type)
        {
            int requiredLevel = useItem.Template.LevelRequirement > 0 ? useItem.Template.LevelRequirement : Math.Min(MAX_LEVEL, useItem.Level);

            if (requiredLevel > Level)
            {
                Out.SendMessage("You are not powerful enough to use this item's spell.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            SpellLine chargeEffectLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
            Spell spell = null;

            if (type == 1)
            {
                spell = SkillBase.FindSpell(useItem.SpellID, chargeEffectLine);
            }
            else
            {
                spell = SkillBase.FindSpell(useItem.SpellID1, chargeEffectLine);
            }

            if (spell != null)
            {
                if (ActiveBuffCharges >= Properties.MAX_CHARGE_ITEMS
                    && SelfBuffChargeIDs.Contains(spell.ID)
                    && effectListComponent.GetSpellEffects().FirstOrDefault(x => x.SpellHandler.Spell.ID == spell.ID) == null)
                {
                    Out.SendMessage("You may only use two buff charge effects.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                GameLiving target = TargetObject as GameLiving;
                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(this, spell, chargeEffectLine);

                if (spellHandler != null)
                {
                    if (!spellHandler.CheckBeginCast(target))
                        return;

                    if (IsOnHorse && !spellHandler.HasPositiveEffect)
                        IsOnHorse = false;

                    Stealth(false);

                    if (spellHandler.StartSpell(target, useItem))
                    {
                        bool castOk = spellHandler.StartReuseTimer;

                        if (spell.SubSpellID > 0)
                        {
                            Spell subspell = SkillBase.GetSpellByID(spell.SubSpellID);
                            if (subspell != null)
                            {
                                ISpellHandler subSpellHandler = ScriptMgr.CreateSpellHandler(this, subspell, chargeEffectLine);

                                if (subSpellHandler != null && subSpellHandler.CheckBeginCast(target))
                                    subSpellHandler.StartSpell(target, useItem);
                            }
                        }

                        if (useItem.MaxCharges > 0)
                        {
                            useItem.Charges--;
                        }

                        if (castOk)
                        {
                            TempProperties.SetProperty(LAST_CHARGED_ITEM_USE_TICK, CurrentRegion.Time);
                            TempProperties.SetProperty(ITEM_USE_DELAY, (long)(60000 * 2));
                            TempProperties.SetProperty("ITEMREUSEDELAY" + useItem.Id_nb, CurrentRegion.Time);
                        }
                    }
                }
                else
                {
                    Out.SendMessage("Charge effect ID " + spell.ID + " is not implemented yet.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
            else
            {
                if (type == 1)
                {
                    Out.SendMessage("Charge effect ID " + useItem.SpellID + " not found.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    Out.SendMessage("Charge effect ID " + useItem.SpellID1 + " not found.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
        }


        /// <summary>
        /// Use a magical item's spell.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="type"></param>
        protected virtual bool UseMagicalItem(DbInventoryItem item, int type)
        {
            if (item == null)
                return false;

            int cooldown = item.CanUseAgainIn;
            if (cooldown > 0 && Client.Account.PrivLevel == (uint)ePrivLevel.Player)
            {
                int minutes = cooldown / 60;
                int seconds = cooldown % 60;
                Out.SendMessage(String.Format("You must wait {0} to discharge this item!",
                        (minutes <= 0)
                            ? String.Format("{0} more seconds", seconds)
                            : String.Format("{0} more minutes and {1} seconds",
                                minutes, seconds)),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);

                return false;
            }


            //Eden
            if (IsMezzed || (IsStunned && !(Steed != null && Steed.Name == "Forceful Zephyr")) || !IsAlive)
            {
                Out.SendMessage("You can't use anything in your state.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (CurrentSpellHandler != null)
            {
                Out.SendMessage("You are already casting a spell.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            SpellLine itemSpellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);

            if (itemSpellLine == null)
                return false;

            if (type == 1 || type == 0)
            {
                Spell spell = SkillBase.FindSpell(item.SpellID, itemSpellLine);

                if (spell != null)
                {
                    int requiredLevel = item.Template.LevelRequirement > 0 ? item.Template.LevelRequirement : Math.Min(MAX_LEVEL, item.Level);

                    if (requiredLevel > Level)
                    {
                        Out.SendMessage("You are not powerful enough to use this item's spell.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    Out.SendMessage(String.Format("You use {0}.", item.GetName(0, false)), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);

                    ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(this, spell, itemSpellLine);
                    if (spellHandler == null)
                        return false;

                    if (IsOnHorse && !spellHandler.HasPositiveEffect)
                        IsOnHorse = false;

                    Stealth(false);

                    if (spellHandler != null && spellHandler.CheckBeginCast(TargetObject as GameLiving))
                    {
                        CastSpell(spell, itemSpellLine);
                        TempProperties.SetProperty(LAST_USED_ITEM_SPELL, item);
                        //CurrentSpellHandler = spellHandler;
                        //CurrentSpellHandler.CastingCompleteEvent += new CastingCompleteCallback(OnAfterSpellCastSequence);
                        //spellHandler.CastSpell(item);
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool CanPoisonWeapon(int objectType)
        {
            switch (objectType)
            {
                case (int)eObjectType.Crossbow:
                case (int)eObjectType.Thrown:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Apply poison to weapon
        /// </summary>
        /// <param name="poisonPotion"></param>
        /// <param name="toItem"></param>
        /// <returns>true if applied</returns>
        public bool ApplyPoison(DbInventoryItem poisonPotion, DbInventoryItem toItem)
        {
            if (poisonPotion == null || toItem == null) return false;
            int envenomSpec = GetModifiedSpecLevel(Specs.Envenom);
            if (envenomSpec < 1)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ApplyPoison.CantUsePoisons"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            if (!GlobalConstants.IsWeapon(toItem.Object_Type))
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ApplyPoison.PoisonsAppliedWeapons"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            if (!HasAbilityToUseItem(toItem.Template) || !CanPoisonWeapon(toItem.Object_Type))
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ApplyPoison.CantPoisonWeapon"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            if (envenomSpec < poisonPotion.Level || Level < poisonPotion.Level)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ApplyPoison.CantUsePoison"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            if (InCombat)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ApplyPoison.CantApplyRecentCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            if (EffectListService.GetEffectOnTarget(this, eEffect.Mez) != null)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ApplyPoison.CantApplyRecentCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            //if (toItem.PoisonCharges > 0)
            //{
            //	Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GameObjects.GamePlayer.ApplyPoison.CantReapplyPoison"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            //	return false;
            //}

            //if (toItem.PoisonSpellID != 0)
            //{
            //	bool canApply = false;
            //	SpellLine poisonLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mundane_Poisons);
            //	if (poisonLine != null)
            //	{
            //		List<Spell> spells = SkillBase.GetSpellList(poisonLine.KeyName);
            //		foreach (Spell spl in spells)
            //		{
            //			if (spl.ID == toItem.PoisonSpellID)
            //			{
            //				canApply = true;
            //				break;
            //			}
            //		}
            //	}
            //	if (canApply == false)
            //	{
            //		Out.SendMessage(string.Format("You can't poison your {0}!", toItem.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            //		return false;
            //	}
            //}

            // Apply poison effect to weapon
            if (poisonPotion.PoisonSpellID != 0)
            {
                toItem.PoisonCharges = poisonPotion.PoisonCharges;
                toItem.PoisonMaxCharges = poisonPotion.PoisonMaxCharges;
                toItem.PoisonSpellID = poisonPotion.PoisonSpellID;
            }
            else
            {
                toItem.PoisonCharges = poisonPotion.Template.PoisonCharges;
                toItem.PoisonMaxCharges = poisonPotion.Template.PoisonMaxCharges;
                toItem.PoisonSpellID = poisonPotion.Template.PoisonSpellID;
            }
            Inventory.RemoveCountFromStack(poisonPotion, 1);
            InventoryLogging.LogInventoryAction(this, "(poison)", eInventoryActionType.Other, poisonPotion.Template);
            Out.SendMessage(string.Format("You apply {0} to {1}.", poisonPotion.GetName(0, false), toItem.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return true;
        }

        #endregion

        #region Send/Say/Yell/Whisper/Messages

        public bool IsIgnoring(GameLiving source)
        {
            if (source is GamePlayer)
            {
                var sender = source as GamePlayer;
                foreach (string Name in IgnoreList)
                {
                    if (sender.Name == Name && sender.Client.Account.PrivLevel < 2)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Delegate to be called when this player receives a text
        /// by someone sending something
        /// </summary>
        public delegate bool SendReceiveHandler(GamePlayer source, GamePlayer receiver, string str);

        /// <summary>
        /// Delegate to be called when this player is about to send a text
        /// </summary>
        public delegate bool SendHandler(GamePlayer source, GamePlayer receiver, string str);

        /// <summary>
        /// Event that is fired when the Player is about to send a text
        /// </summary>
        public event SendHandler OnSend;

        /// <summary>
        /// Event that is fired when the Player receives a Send text
        /// </summary>
        public event SendReceiveHandler OnSendReceive;

        /// <summary>
        /// Clears all send event handlers
        /// </summary>
        public void ClearOnSend()
        {
            OnSend = null;
        }

        /// <summary>
        /// Clears all OnSendReceive event handlers
        /// </summary>
        public void ClearOnSendReceive()
        {
            OnSendReceive = null;
        }

        /// <summary>
        /// This function is called when the Player receives a sent text
        /// </summary>
        /// <param name="source">GamePlayer that was sending</param>
        /// <param name="str">string that was sent</param>
        /// <returns>true if the string was received successfully, false if it was not received</returns>
        public virtual bool PrivateMessageReceive(GamePlayer source, string str)
        {
            var onSendReceive = OnSendReceive;

            if (onSendReceive != null && !onSendReceive(source, this, str))
                return false;

            if (IsIgnoring(source))
                return true;

            if (GameServer.ServerRules.IsAllowedToUnderstand(source, this))
            {
                if (source.Client.Account.PrivLevel > 1)
                    // Message: {0} [TEAM] sends, "{1}"
                    ChatUtil.SendGMMessage(Client, "Social.ReceiveMessage.Staff.SendsToYou", source.Name, str);
                else
                    // Message: {0} sends, "{1}"
                    ChatUtil.SendSendMessage(Client, "Social.ReceiveMessage.Private.Sends", source.Name, str);
            }
            else
            {
                // Message: {0} sends something in a language you don't understand.
                ChatUtil.SendSystemMessage(Client, "Social.ReceiveMessage.Private.FalseLanguage", source.Name);
                return true;
            }

            var afkmessage = TempProperties.GetProperty<string>(AFK_MESSAGE);
            if (afkmessage != null)
            {
                if (afkmessage == string.Empty)
                {
                    // Message: {0} is currently AFK.
                    ChatUtil.SendSystemMessage(source.Client, "Social.ReceiveMessage.AFK.PlayerAFK", Name);
                }
                else
                {
                    // Message: {0} is currently AFK.
                    ChatUtil.SendSystemMessage(source.Client, "Social.ReceiveMessage.AFK.PlayerAFK", Name);
                    // Message: <AFK> {0} sends, "{1}"
                    ChatUtil.SendSendMessage(source.Client, "Social.ReceiveMessage.AFK.Sends", Name, afkmessage);
                }
            }

            return true;
        }

        /// <summary>
        /// Sends a text to a target
        /// </summary>
        /// <param name="target">The target of the send</param>
        /// <param name="str">string to send (without any "xxx sends:" in front!!!)</param>
        /// <returns>true if text was sent successfully</returns>
        public virtual bool SendPrivateMessage(GamePlayer target, string str)
        {
            if (target == null || str == null)
                return false;

            SendHandler onSend = OnSend;
            if (onSend != null && !onSend(this, target, str))
                return false;

            if (!target.PrivateMessageReceive(this, str))
            {
                // Message: {0} doesn't seem to understand you!
                ChatUtil.SendSystemMessage(Client, "Social.SendMessage.Private.DontUnderstandYou", target.Name);
                return false;
            }

            if (Client.Account.PrivLevel == 1 && target.Client.Account.PrivLevel > 1 && target.IsAnonymous)
                return true;

            // Message: You send, "{0}" to {1}.
            ChatUtil.SendSendMessage(Client, "Social.SendMessage.Private.YouSendTo", str, target.Name);

            return true;
        }

        /// <summary>
        /// This function is called when the Player receives a Say text!
        /// </summary>
        /// <param name="source">The source living saying something</param>
        /// <param name="str">the text that was said</param>
        /// <returns>true if received successfully</returns>
        public override bool SayReceive(GameLiving source, string str)
        {
            if (!base.SayReceive(source, str))
                return false;
            if (IsIgnoring(source))
                return true;

            if (GameServer.ServerRules.IsAllowedToUnderstand(source, this) || Properties.ENABLE_DEBUG)
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.SayReceive.Says", source.GetName(0, false), str),
                    eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            else
                Out.SendMessage(
                    LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.SayReceive.FalseLanguage", source.GetName(0, false)),
                    eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            return true;
        }

        /// <summary>
        /// Call this function to make the player say something
        /// </summary>
        /// <param name="str">string to say</param>
        /// <returns>true if said successfully</returns>
        public override bool Say(string str)
        {
            if (!GameServer.ServerRules.IsAllowedToSpeak(this, "talk"))
                return false;
            if (!base.Say(str))
                return false;
            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Say.YouSay", str), eChatType.CT_Say,
                eChatLoc.CL_ChatWindow);
            return true;
        }

        /// <summary>
        /// This function is called when the player hears a yell
        /// </summary>
        /// <param name="source">the source living yelling</param>
        /// <param name="str">string that was yelled</param>
        /// <returns>true if received successfully</returns>
        public override bool YellReceive(GameLiving source, string str)
        {
            if (!base.YellReceive(source, str))
                return false;
            if (IsIgnoring(source))
                return true;
            if (GameServer.ServerRules.IsAllowedToUnderstand(source, this))
                Out.SendMessage(source.GetName(0, false) + " yells, \"" + str + "\"", eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            else
                Out.SendMessage(source.GetName(0, false) + " yells something in a language you don't understand.", eChatType.CT_Say,
                    eChatLoc.CL_ChatWindow);
            return true;
        }

        /// <summary>
        /// Call this function to make the player yell something
        /// </summary>
        /// <param name="str">string to yell</param>
        /// <returns>true if yelled successfully</returns>
        public override bool Yell(string str)
        {
            if (!GameServer.ServerRules.IsAllowedToSpeak(this, "yell"))
                return false;
            if (!base.Yell(str))
                return false;
            Out.SendMessage("You yell, \"" + str + "\"", eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            return true;
        }

        /// <summary>
        /// This function is called when the player hears a whisper
        /// from some living
        /// </summary>
        /// <param name="source">Source that was living</param>
        /// <param name="str">string that was whispered</param>
        /// <returns>true if whisper was received successfully</returns>
        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;
            if (IsIgnoring(source))
                return true;
            if (GameServer.ServerRules.IsAllowedToUnderstand(source, this))
                Out.SendMessage(source.GetName(0, false) + " whispers to you, \"" + str + "\"", eChatType.CT_Say,
                    eChatLoc.CL_ChatWindow);
            else
                Out.SendMessage(source.GetName(0, false) + " whispers something in a language you don't understand.",
                    eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            return true;
        }

        /// <summary>
        /// Call this function to make the player whisper to someone
        /// </summary>
        /// <param name="target">GameLiving to whisper to</param>
        /// <param name="str">string to whisper</param>
        /// <returns>true if whispered successfully</returns>
        public override bool Whisper(GameObject target, string str)
        {
            if (target == null)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Whisper.SelectTarget"), eChatType.CT_System,
                    eChatLoc.CL_ChatWindow);
                return false;
            }
            if (!GameServer.ServerRules.IsAllowedToSpeak(this, "whisper"))
                return false;
            if (!base.Whisper(target, str))
                return false;
            if (target is GamePlayer)
                Out.SendMessage("You whisper, \"" + str + "\" to " + target.GetName(0, false), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            return true;
        }

        /// <summary>
        /// A message to this player from some piece of code (message to ourself)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="chatType"></param>
        public override void MessageToSelf(string message, eChatType chatType)
        {
            Out.SendMessage(message, chatType, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// A message sent to all objects within a set radius of the triggering entity (e.g., MessageToArea)
        /// </summary>
        /// <param name="source">The originating source of the message</param>
        /// <param name="message">The content of the message</param>
        /// <param name="chatType">The message type (e.g., CT_Say, CT_Spell)</param>
        /// <param name="chatLocation">The UI element to display the message in (e.g., CL_SystemWindow)</param>
        public virtual void MessageFromArea(GameObject source, string message, eChatType chatType, eChatLoc chatLocation)
        {
            Out.SendMessage(message, chatType, chatLocation);
        }

        #endregion

        #region Steed

        /// <summary>
        /// Holds the GameLiving that is the steed of this player as weakreference
        /// </summary>
        protected WeakReference m_steed;
        /// <summary>
        /// Holds the Steed of this player
        /// </summary>
        public GameNPC Steed
        {
            get { return m_steed.Target as GameNPC; }
            set { m_steed.Target = value; }
        }

        /// <summary>
        /// Delegate callback to be called when the player
        /// tries to mount a steed
        /// </summary>
        public delegate bool MountSteedHandler(GamePlayer rider, GameNPC steed, bool forced);

        /// <summary>
        /// Event will be fired whenever the player tries to
        /// mount a steed
        /// </summary>
        public event MountSteedHandler OnMountSteed;
        /// <summary>
        /// Clears all MountSteed handlers
        /// </summary>
        public void ClearMountSteedHandlers()
        {
            OnMountSteed = null;
        }

        /// <summary>
        /// Mounts the player onto a steed
        /// </summary>
        /// <param name="steed">the steed to mount</param>
        /// <param name="forced">true if the mounting can not be prevented by handlers</param>
        /// <returns>true if mounted successfully or false if not</returns>
        public virtual bool MountSteed(GameNPC steed, bool forced)
        {
            // Sanity 'coherence' checks
            if (Steed != null)
                if (!DismountSteed(forced))
                    return false;

            if (IsOnHorse)
                IsOnHorse = false;

            if (!steed.RiderMount(this, forced) && !forced)
                return false;

            if (OnMountSteed != null && !OnMountSteed(this, steed, forced) && !forced)
                return false;

            // Standard checks, as specified in rules
            if (GameServer.ServerRules.ReasonForDisallowMounting(this) != string.Empty && !forced)
                return false;

            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player == null) continue;
                player.Out.SendRiding(this, steed, false);
            }

            return true;
        }

        /// <summary>
        /// Delegate callback to be called whenever the player tries
        /// to dismount from a steed
        /// </summary>
        public delegate bool DismountSteedHandler(GamePlayer rider, GameLiving steed, bool forced);

        /// <summary>
        /// Event will be fired whenever the player tries to dismount
        /// from a steed
        /// </summary>
        public event DismountSteedHandler OnDismountSteed;
        /// <summary>
        /// Clears all DismountSteed handlers
        /// </summary>
        public void ClearDismountSteedHandlers()
        {
            OnDismountSteed = null;
        }

        /// <summary>
        /// Dismounts the player from it's steed.
        /// </summary>
        /// <param name="forced">true if the dismounting should not be prevented by handlers</param>
        /// <returns>true if the dismount was successful, false if not</returns>
        public virtual bool DismountSteed(bool forced)
        {
            if (Steed == null)
                return false;
            if (Steed.Name == "Forceful Zephyr" && !forced) return false;
            if (OnDismountSteed != null && !OnDismountSteed(this, Steed, forced) && !forced)
                return false;
            GameObject steed = Steed;
            if (!Steed.RiderDismount(forced, this) && !forced)
                return false;

            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player == null) continue;
                player.Out.SendRiding(this, steed, true);
            }
            return true;
        }

        /// <summary>
        /// Returns if the player is riding or not
        /// </summary>
        /// <returns>true if on a steed, false if not</returns>
        public virtual bool IsRiding
        {
            get { return Steed != null; }
        }

        public void SwitchSeat(int slot)
        {
            if (Steed == null)
                return;

            if (Steed.Riders[slot] != null)
                return;

            Out.SendMessage("You switch to seat " + slot + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            GameNPC steed = Steed;
            steed.RiderDismount(true, this);
            steed.RiderMount(this, true, slot);
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player == null) continue;
                player.Out.SendRiding(this, steed, false);
            }
        }

        #endregion

        #region Add/Move/Remove

        /// <summary>
        /// Called to create an player in the world and send the other
        /// players around this player an update
        /// </summary>
        /// <returns>true if created, false if creation failed</returns>
        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
            {
                if (log.IsErrorEnabled)
                    log.Error($"Failed to add player to world: {this}");

                return false;
            }

            m_invulnerabilityTick = 0;
            craftComponent = new CraftComponent(this);

            foreach (GamePlayer playerInRadius in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                // Prevents players from seeing stealthed GMs during their loading time.
                if (playerInRadius != this && playerInRadius.CanDetect(this))
                    playerInRadius.Out.SendPlayerCreate(this);
            }

            UpdateEquipmentAppearance();
            UpdateEncumbrance(true);

            // display message
            if (SpecPointsOk == false)
            {
                log.Debug(Name + " is told spec points are incorrect!");
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language,"GamePlayer.AddToWorld.SpecsPointsIncorrect"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                SpecPointsOk = true;
            }

            (CurrentRegion as BaseInstance)?.OnPlayerEnterInstance(this);
            AppealMgr.OnPlayerEnter(this);
            RefreshItemBonuses();
            LastPositionUpdatePacketReceivedTime = GameLoop.GameLoopTime;
            LastPlayerActivityTime = GameLoop.GameLoopTime;
            ClientService.Instance.OnPlayerJoin(this);
            return true;
        }

        /// <summary>
        /// Called to remove the item from the world. Also removes the
        /// player visibly from all other players around this one
        /// </summary>
        /// <returns>true if removed, false if removing failed</returns>
        public override bool RemoveFromWorld()
        {
            if (!CharacterClass.RemoveFromWorld())
                return false;

            DismountSteed(true);

            if (!base.RemoveFromWorld())
                return false;

            if (CurrentRegion.GetZone(X, Y) == null)
                MoveToBind();

            if (m_invulnerabilityTimer != null)
            {
                m_invulnerabilityTimer.Stop();
                m_invulnerabilityTimer = null;
            }

            UpdateWaterBreathState(eWaterBreath.None);

            if (IsOnHorse)
                IsOnHorse = false;

            if (CurrentRegion is BaseInstance instance)
                instance.OnPlayerLeaveInstance(this);

            Duel?.Stop();
            ClientService.Instance.OnPlayerLeave(this);
            return true;
        }

        /// <summary>
        /// Marks this player as deleted
        /// </summary>
        public override void Delete()
        {
            CleanupOnDisconnect();
            base.Delete();
        }

        /// <summary>
        /// The property to save debug mode on region change
        /// </summary>
        public const string DEBUG_MODE_PROPERTY = "Player.DebugMode";

        /// <summary>
        /// This function moves a player to a specific region and
        /// specific coordinates.
        /// </summary>
        /// <param name="regionID">RegionID to move to</param>
        /// <param name="x">X target coordinate</param>
        /// <param name="y">Y target coordinate</param>
        /// <param name="z">Z target coordinate (0 to put player on floor)</param>
        /// <param name="heading">Target heading</param>
        /// <returns>true if move succeeded, false if failed</returns>
        public override bool MoveTo(ushort regionID, int x, int y, int z, ushort heading)
        {
            // If we are jumping somewhere away from our house not using house.Exit, we need to make the server know we have left the house.
            if ((CurrentHouse != null || InHouse) && CurrentHouse.RegionID != regionID)
            {
                InHouse = false;
                CurrentHouse = null;
            }

            if (IsOnHorse)
                IsOnHorse = false;

            Region rgn = WorldMgr.GetRegion(regionID);

            if (rgn == null || !GameServer.ServerRules.IsAllowedToZone(this, rgn))
                return false;

            if (rgn.GetZone(x, y) == null)
                return false;

            UpdateWaterBreathState(eWaterBreath.None);
            SiegeWeapon?.ReleaseControl();

            if (regionID != CurrentRegionID)
            {
                GameEventMgr.Notify(GameLivingEvent.RegionChanging, this);

                if (!RemoveFromWorld())
                    return false;

                CurrentRegion.Notify(RegionEvent.PlayerLeave, CurrentRegion, new RegionPlayerEventArgs(this));

                if (ControlledBrain != null)
                    CommandNpcRelease();
            }
            else
            {
                if (Steed != null)
                    DismountSteed(true);
            }

            CurrentSpeed = 0;
            X = x;
            Y = y;
            Z = z;
            Heading = heading;
            IsSitting = false;
            movementComponent.OnTeleportOrRegionChange();

            if (regionID != CurrentRegionID)
            {
                CurrentRegionID = regionID;
                Out.SendRegionChanged();
                return true;
            }

            Out.SendPlayerJump(false);
            UpdateEquipmentAppearance();

            if (IsUnderwater)
                IsDiving = true;

            if (ControlledBrain == null)
                return true;

            Point2D point = GetPointFromHeading(Heading, 64);
            IControlledBrain petBrain = ControlledBrain;

            if (petBrain == null)
                return true;

            GameNPC pet = petBrain.Body;

            if (pet.MaxSpeedBase <= 0)
                return true;

            pet.MoveInRegion(CurrentRegionID, point.X, point.Y, Z + 10, (ushort) ((Heading + 2048) % 4096), false);
            return true;
        }

        public virtual bool MoveToBind()
        {
            if (!GameServer.ServerRules.IsAllowedToMoveToBind(this))
                return false;

            ValidateBind();
            return MoveTo((ushort) BindRegion, BindXpos, BindYpos, BindZpos, (ushort) BindHeading);
        }

        public void ValidateBind()
        {
            Region region = WorldMgr.GetRegion((ushort) BindRegion);

            if (region != null && GameServer.ServerRules.IsAllowedToZone(this, region) && region.GetZone(BindXpos, BindYpos) != null)
                return;

            if (log.IsErrorEnabled)
                log.Error($"Unknown bind point (Bind: {BindXpos},{BindYpos} in {BindRegion}) (Player: {this})");

            // This method wasn't meant to be used this way, but this is good enough for now.
            GameEvents.StartupLocations.CharacterCreation(null, null, new CharacterEventArgs(DBCharacter, Client));
        }

        #endregion

        #region Group/Friendlist/guild

        private Guild m_guild;
        private DbGuildRank m_guildRank;

        /// <summary>
        /// Gets or sets the player's guild
        /// </summary>
        public Guild Guild
        {
            get => m_guild;
            set
            {
                if (value == null)
                    m_guild.RemoveOnlineMember(this);

                m_guild = value;

                // Update guild name for all players if client is playing.
                if (ObjectState == eObjectState.Active)
                {
                    Out.SendUpdatePlayer();

                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        if (player != this)
                        {
                            player.Out.SendObjectRemove(this);
                            player.Out.SendPlayerCreate(this);
                            player.Out.SendLivingEquipmentUpdate(this);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the player's guild rank
        /// </summary>
        public DbGuildRank GuildRank
        {
            get { return m_guildRank; }
            set
            {
                m_guildRank = value;
                if (value != null && DBCharacter != null)
                    DBCharacter.GuildRank = value.RankLevel;//maybe mistake here and need to change and make an index var
            }
        }

        /// <summary>
        /// Gets or sets the database guildid of this player
        /// (delegate to DBCharacter)
        /// </summary>
        public string GuildID
        {
            get { return DBCharacter != null ? DBCharacter.GuildID : string.Empty; }
            set { if (DBCharacter != null) DBCharacter.GuildID = value; }
        }

        /// <summary>
        /// Gets or sets the player's guild flag
        /// (delegate to DBCharacter)
        /// </summary>
        public bool ClassNameFlag
        {
            get { return DBCharacter != null ? DBCharacter.FlagClassName : false; }
            set { if (DBCharacter != null) DBCharacter.FlagClassName = value; }
        }

        /// <summary>
        /// true if this player is looking for a group
        /// </summary>
        protected bool m_lookingForGroup;
        /// <summary>
        /// true if this player want to receive loot with autosplit between members of group
        /// </summary>
        protected bool m_autoSplitLoot = true;

        /// <summary>
        /// Gets or sets the LookingForGroup flag in this player
        /// </summary>
        public bool LookingForGroup
        {
            get { return m_lookingForGroup; }
            set { m_lookingForGroup = value; }
        }

        /// <summary>
        /// Gets/sets the autosplit for loot
        /// </summary>
        public bool AutoSplitLoot
        {
            get { return m_autoSplitLoot; }
            set { m_autoSplitLoot = value; }
        }

        /// <summary>
        /// Gets or sets the IgnoreList of a Player
        /// (delegate to PlayerCharacter)
        /// </summary>
        public ArrayList IgnoreList
        {
            get
            {
                if (SerializedIgnoreList.Length > 0)
                    return new ArrayList(SerializedIgnoreList);
                return new ArrayList(0);
            }
            set
            {
                if (value == null)
                    SerializedIgnoreList = new string[0];
                else
                    SerializedIgnoreList = value.OfType<string>().ToArray();

                if (DBCharacter != null)
                    GameServer.Database.SaveObject(DBCharacter);
            }
        }

        /// <summary>
        /// Modifies the friend list of this player
        /// </summary>
        /// <param name="friendName">the friend name</param>
        /// <param name="remove">true to remove this friend, false to add it</param>
        public void ModifyIgnoreList(string Name, bool remove)
        {
            ArrayList currentIgnores = IgnoreList;
            if (remove && currentIgnores != null)
            {
                if (currentIgnores.Contains(Name))
                {
                    currentIgnores.Remove(Name);
                    IgnoreList = currentIgnores;
                }
            }
            else
            {
                if (!currentIgnores.Contains(Name))
                {
                    currentIgnores.Add(Name);
                    IgnoreList = currentIgnores;
                }
            }
        }

        #endregion

        #region X/Y/Z/Region/Realm/Position...

        /// <summary>
        /// Holds all areas this player is currently within
        /// </summary>
        private ReaderWriterList<IArea> m_currentAreas = new ReaderWriterList<IArea>();

        /// <summary>
        /// Holds all areas this player is currently within
        /// </summary>
        public override IList<IArea> CurrentAreas
        {
            get { return m_currentAreas; }
            set { m_currentAreas.FreezeWhile(l => { l.Clear(); l.AddRange(value); }); }
        }

        /// <summary>
        /// Property that saves last maximum Z value
        /// </summary>
        public const string MAX_LAST_Z = "max_last_z";

        /// <summary>
        /// The base speed of the player
        /// </summary>
        public const int PLAYER_BASE_SPEED = 191;

        public long m_areaUpdateTick = 0;


        /// <summary>
        /// Gets the tick when the areas should be updated
        /// </summary>
        public long AreaUpdateTick
        {
            get { return m_areaUpdateTick; }
            set { m_areaUpdateTick = value; }
        }

        /// <summary>
        /// Gets the current position of this player
        /// </summary>
        public override int X
        {
            set
            {
                base.X = value;

                if (DBCharacter != null)
                    DBCharacter.Xpos = base.X;
            }
        }

        /// <summary>
        /// Gets the current position of this player
        /// </summary>
        public override int Y
        {
            set
            {
                base.Y = value;

                if (DBCharacter != null)
                    DBCharacter.Ypos = base.Y;
            }
        }

        /// <summary>
        /// Gets the current position of this player
        /// </summary>
        public override int Z
        {
            set
            {
                base.Z = value;

                if (DBCharacter != null)
                    DBCharacter.Zpos = base.Z;
            }
        }

        /// <summary>
        /// Gets or sets the current speed of this player
        /// </summary>
        public override short CurrentSpeed
        {
            set => base.CurrentSpeed = value;
        }

        public short FallSpeed { get; set; }
        public PlayerPositionUpdateHandler.StateFlags StateFlags { get; set; }
        public PlayerPositionUpdateHandler.ActionFlags ActionFlags { get; set; }

        /// <summary>
        /// Gets or sets the region of this player
        /// </summary>
        public override Region CurrentRegion
        {
            set
            {
                base.CurrentRegion = value;
                if (DBCharacter != null) DBCharacter.Region = CurrentRegionID;
            }
        }

        public Zone LastPositionUpdateZone { get; set; }
        public long LastPlayerActivityTime { get; set; }

        /// <summary>
        /// Holds the players max Z for fall damage
        /// </summary>
        private int m_maxLastZ;

        /// <summary>
        /// Gets or sets the players max Z for fall damage
        /// </summary>
        public int MaxLastZ
        {
            get { return m_maxLastZ; }
            set { m_maxLastZ = value; }
        }

        /// <summary>
        /// Gets or sets the realm of this player
        /// </summary>
        public override eRealm Realm
        {
            get { return DBCharacter != null ? (eRealm)DBCharacter.Realm : base.Realm; }
            set
            {
                base.Realm = value;
                if (DBCharacter != null) DBCharacter.Realm = (byte)value;
            }
        }

        /// <summary>
        /// Gets or sets the heading of this player
        /// </summary>
        public override ushort Heading
        {
            set
            {
                base.Heading = value;

                if (DBCharacter != null)
                    DBCharacter.Direction = value;

                attackComponent.attackAction.OnHeadingUpdate();
            }
        }

        protected bool m_climbing;
        /// <summary>
        /// Gets/sets the current climbing state
        /// </summary>
        public bool IsClimbing
        {
            get { return m_climbing; }
            set
            {
                if (value == m_climbing) return;
                m_climbing = value;
            }
        }

        protected bool m_swimming;
        /// <summary>
        /// Gets/sets the current swimming state
        /// </summary>
        public virtual bool IsSwimming
        {
            get { return m_swimming; }
            set
            {
                if (value == m_swimming) return;
                m_swimming = value;
                Notify(GamePlayerEvent.SwimmingStatus, this);
                //Handle Lava Damage
                if (m_swimming && CurrentZone.IsLava == true)
                {
                    if (m_lavaBurningTimer == null)
                    {
                        m_lavaBurningTimer = new ECSGameTimer(this);
                        m_lavaBurningTimer.Callback = new ECSGameTimer.ECSTimerCallback(LavaBurnTimerCallback);
                        m_lavaBurningTimer.Interval = 2000;
                        m_lavaBurningTimer.Start(1);
                    }
                }
                if (!m_swimming && CurrentZone.IsLava == true && m_lavaBurningTimer != null)
                {
                    m_lavaBurningTimer.Stop();
                    m_lavaBurningTimer = null;
                }
            }
        }

        protected long m_beginDrowningTick;
        protected eWaterBreath m_currentWaterBreathState;

        protected int DrowningTimerCallback(ECSGameTimer callingTimer)
        {
            if (!IsAlive)
            {
                Out.SendCloseTimerWindow();
                return 0;
            }

            if (ObjectState != eObjectState.Active)
                return 0;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.DrowningTimerCallback.CannotBreath"), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.DrowningTimerCallback.Take5%Damage"), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);

            if (GameLoop.GameLoopTime - m_beginDrowningTick > 15000)
            {
                TakeDamage(null, eDamageType.Natural, MaxHealth, 0);
                Out.SendCloseTimerWindow();
                return 0;
            }
            else
                TakeDamage(null, eDamageType.Natural, MaxHealth / 20, 0);

            return 1000;
        }

        protected ECSGameTimer m_drowningTimer;
        protected ECSGameTimer m_holdBreathTimer;
        protected ECSGameTimer m_lavaBurningTimer;
        /// <summary>
        /// The diving state of this player
        /// </summary>
        protected bool m_diving;

        public bool IsDiving
        {
            get => m_diving;
            set
            {
                // Force the diving state instead of trusting the client.
                if (!value)
                    value = IsUnderwater;

                if (value && !CurrentZone.IsDivingEnabled && Client.Account.PrivLevel == 1)
                {
                    Z += 1;
                    Out.SendPlayerJump(false);
                    return;
                }

                if (m_diving == value)
                    return;

                m_diving = value;

                if (m_diving)
                {
                    if (!CanBreathUnderWater)
                        UpdateWaterBreathState(eWaterBreath.Holding);
                }
                else
                    UpdateWaterBreathState(eWaterBreath.None);
            }
        }

        protected bool m_canBreathUnderwater;
        public bool CanBreathUnderWater
        {
            get => m_canBreathUnderwater;
            set
            {
                if (m_canBreathUnderwater == value)
                    return;

                m_canBreathUnderwater = value;

                if (IsDiving)
                {
                    if (m_canBreathUnderwater)
                        UpdateWaterBreathState(eWaterBreath.None);
                    else
                        UpdateWaterBreathState(eWaterBreath.Holding);
                }
            }
        }

        public void UpdateWaterBreathState(eWaterBreath state)
        {
            if (Client.Account.PrivLevel != 1)
                return;

            switch (state)
            {
                case eWaterBreath.None:
                {
                    m_holdBreathTimer.Stop();
                    m_drowningTimer.Stop();
                    Out.SendCloseTimerWindow();
                    break;
                }
                case eWaterBreath.Holding:
                {

                    m_drowningTimer.Stop();

                    if (!m_holdBreathTimer.IsAlive)
                    {
                        Out.SendTimerWindow("Holding Breath", 30);
                        m_holdBreathTimer.Start(30000);
                    }

                    break;
                }
                case eWaterBreath.Drowning:
                {
                    if (m_holdBreathTimer.IsAlive)
                    {
                        m_holdBreathTimer.Stop();

                        if (!m_drowningTimer.IsAlive)
                        {
                            Out.SendTimerWindow("Drowning", 15);
                            m_beginDrowningTick = CurrentRegion.Time;
                            m_drowningTimer.Start(0);
                        }
                    }
                    else
                    {
                        // In case the player gets out of water right before the timer is stopped (and ticks instead).
                        UpdateWaterBreathState(eWaterBreath.None);
                        return;
                    }

                    break;
                }
            }

            m_currentWaterBreathState = state;
        }

        protected int LavaBurnTimerCallback(ECSGameTimer callingTimer)
        {
            if (!IsAlive || ObjectState != eObjectState.Active || !IsSwimming)
                return 0;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.LavaBurnTimerCallback.YourInLava"), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.LavaBurnTimerCallback.Take34%Damage"), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);

            if (Client.Account.PrivLevel == 1)
            {
                TakeDamage(null, eDamageType.Natural, (int)(MaxHealth * 0.34), 0);

                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendCombatAnimation(null, this, 0x0000, 0x0000, 0x00, 0x00, 0x14, HealthPercent);
            }

            return 2000;
        }

        protected bool m_sitting;
        public override bool IsSitting
        {
            get => m_sitting;
            set
            {
                if (!m_sitting && value)
                {
                    CurrentSpellHandler?.CasterMoves();

                    if (attackComponent.AttackState && ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                    {
                        string attackTypeMsg = (eObjectType) ActiveWeapon.Object_Type == eObjectType.Thrown ? "throw" : "shot";
                        Out.SendMessage($"You move and interrupt your {attackTypeMsg}!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        attackComponent.StopAttack();
                    }
                }

                m_sitting = value;
            }
        }

        /// <summary>
        /// Gets or sets the max speed of this player
        /// (delegate to PlayerCharacter)
        /// </summary>
        public override short MaxSpeedBase
        {
            get => DBCharacter != null ? (short) DBCharacter.MaxSpeed : base.MaxSpeedBase;
            set
            {
                base.MaxSpeedBase = value;

                if (DBCharacter != null)
                    DBCharacter.MaxSpeed = value;
            }
        }

        public override bool IsMoving => base.IsMoving || IsStrafing;

        public bool IsSprinting => effectListComponent.ContainsEffectForEffectType(eEffect.Sprint);

        public virtual bool Sprint(bool state)
        {
            if (state == IsSprinting)
                return state;

            if (state)
            {
                // Can't start sprinting with 10 endurance on 1.68 server.
                if (Endurance <= 10)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sprint.TooFatigued"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (IsStealthed)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sprint.CantSprintHidden"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (!IsAlive)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sprint.CantSprintDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                ECSGameEffectFactory.Create(new(this, 0, 1), static (in ECSGameEffectInitParams i) => new SprintECSGameEffect(i));
                return true;
            }
            else
            {
                ECSGameEffect effect = EffectListService.GetEffectOnTarget(this, eEffect.Sprint);
                effect?.Stop();
                return false;
            }
        }

        protected bool m_strafing;
        public override bool IsStrafing
        {
            get => m_strafing;
            set => m_strafing = value;
        }

        public virtual void OnPlayerMove()
        {
            if (IsSitting)
                Sit(false);

            if (IsCasting)
                CurrentSpellHandler?.CasterMoves();

            if (IsCastingRealmAbility)
            {
                Out.SendInterruptAnimation(this);
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "SpellHandler.CasterMove"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                RealmAbilityCastTimer.Stop();
                RealmAbilityCastTimer = null;
            }

            if (IsCrafting)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnPlayerMove.InterruptCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                craftComponent.StopCraft();
                Out.SendCloseTimerWindow();
            }

            if (IsSalvagingOrRepairing)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnPlayerMove.InterruptCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                CraftTimer.Stop();
                CraftTimer = null;
                Out.SendCloseTimerWindow();
            }

            if (IsSummoningMount)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnPlayerMove.CannotCallMount"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                StopWhistleTimers();
            }

            if (attackComponent.AttackState)
            {
                if (ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    string attackTypeMsg = (ActiveWeapon.Object_Type == (int)eObjectType.Thrown ? "throw" : "shot");
                    Out.SendMessage("You move and interrupt your " + attackTypeMsg + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    attackComponent.StopAttack();
                }
                else
                {
                    AttackData ad = attackComponent.attackAction.LastAttackData;

                    if (ad != null && ad.IsMeleeAttack && (ad.AttackResult == eAttackResult.TargetNotVisible || ad.AttackResult == eAttackResult.OutOfRange))
                    {
                        if (ad.Target != null && IsObjectInFront(ad.Target, 120) && IsWithinRadius(ad.Target, attackComponent.AttackRange))
                            attackComponent.attackAction.OnHeadingUpdate();
                    }
                }
            }

            // Volley is weird and doesn't activate attack mode.
            if (effectListComponent.ContainsEffectForEffectType(eEffect.Volley))
            {
                AtlasOF_VolleyECSEffect volley = EffectListService.GetEffectOnTarget(this, eEffect.Volley) as AtlasOF_VolleyECSEffect;
                volley?.OnPlayerMoved();
            }
        }

        public virtual void Sit(bool sit)
        {
            Sprint(false);

            if (IsSummoningMount)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.InterruptCallMount"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                StopWhistleTimers();
            }

            if (IsSitting == sit)
            {
                if (sit)
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.AlreadySitting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                else
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.NotSitting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                return;
            }

            if (!IsAlive)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.CantSitDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (IsStunned)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.CantSitStunned"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (IsMezzed)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.CantSitMezzed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (sit && IsMoving)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.MustStandingStill"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (Steed != null || IsOnHorse)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.MustDismount"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (sit)
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.YouSitDown"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.YouStandUp"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            // Stop attacking if the player sits down.
            if (sit && attackComponent.AttackState)
                attackComponent.StopAttack();

            if (!sit)
            {
                // Stop quit sequence if the player stands up.
                if (_quitTimer != null)
                {
                    _quitTimer.Stop();
                    _quitTimer = null;
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Sit.NoLongerWaitingQuit"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                // Stop praying if the player stands up.
                if (IsPraying)
                    m_prayAction.Stop();
            }

            // Update the client.
            if (sit && !IsSitting)
                Out.SendStatusUpdate(2);

            IsSitting = sit;
            UpdatePlayerStatus();
        }

        /// <summary>
        /// Sets the Living's ground-target Coordinates inside the current Region
        /// </summary>
        public override void SetGroundTarget(int groundX, int groundY, int groundZ)
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(this, eEffect.Volley);//volley check for gt
            if (volley != null)
            {
                Out.SendMessage("You can't change ground target under volley effect!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            else
            {
                base.SetGroundTarget(groundX, groundY, groundZ);

                Out.SendMessage(String.Format("You ground-target {0},{1},{2}", groundX, groundY, groundZ), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                if (SiegeWeapon != null)
                    SiegeWeapon.SetGroundTarget(groundX, groundY, groundZ);
            }
        }

        /// <summary>
        /// Updates Health, Mana, Sitting, Endurance, Concentration and Alive status to client
        /// </summary>
        public void UpdatePlayerStatus()
        {
            Out.SendStatusUpdate();
        }
        #endregion

        #region Equipment/Encumberance

        public int MaxCarryingCapacity
        {
            get
            {
                // Patch 1.62
                // Strength (and strength only) debuffs and disease spells should no longer reduce a player's encumbrance below their unbuffed maximum.
                // Debuffers were using the fact that you could reduce an enemy to 0 movement speed as an effective one minute total snare with no counter,
                // which was not the intention of strength debuff spells.

                double result = Math.Max(GetModified(eProperty.Strength), GetModifiedBase(eProperty.Strength));
                RAPropertyEnhancer lifter = GetAbility<AtlasOF_LifterAbility>();

                if (lifter != null)
                    result *= 1 + lifter.Amount * 0.01;

                return (int) result;
            }
        }

        private int _previousInventoryWeight;
        private int _previousMaxCarryingCapacity;

        public bool IsEncumbered { get; private set;}
        public double MaxSpeedModifierFromEncumbrance { get; private set; }

        public void UpdateEncumbrance(bool forced = false)
        {
            int inventoryWeight = Inventory.InventoryWeight;
            int maxCarryingCapacity = MaxCarryingCapacity;

            if (!forced && _previousInventoryWeight == inventoryWeight && _previousMaxCarryingCapacity == maxCarryingCapacity)
                return;

            double maxCarryingCapacityRatio = maxCarryingCapacity * 0.35;
            double newMaxSpeedModifier = 1 - inventoryWeight / maxCarryingCapacityRatio + maxCarryingCapacity / maxCarryingCapacityRatio;

            if (forced || MaxSpeedModifierFromEncumbrance != newMaxSpeedModifier)
            {
                if (inventoryWeight > maxCarryingCapacity)
                {
                    IsEncumbered = true;
                    string message;

                    if (movementComponent.MaxSpeedPercent <= 0)
                        message = "GamePlayer.UpdateEncumbrance.EncumberedCannotMove";
                    else
                        message = "GamePlayer.UpdateEncumbrance.EncumberedMoveSlowly";

                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, message), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }
                else
                    IsEncumbered = false;

                MaxSpeedModifierFromEncumbrance = newMaxSpeedModifier;
                Out.SendUpdateMaxSpeed(); // Should automatically end up updating max speed using `MaxSpeedModifierFromEncumbrance` if `IsEncumbered` is set to true.
            }

            _previousInventoryWeight = inventoryWeight;
            _previousMaxCarryingCapacity = maxCarryingCapacity;
            Out.SendEncumbrance();
        }

        public void UpdateEquipmentAppearance()
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player != this)
                    player.Out.SendLivingEquipmentUpdate(this);
            }
        }

        public override void UpdateHealthManaEndu()
        {
            Out.SendCharStatsUpdate();
            Out.SendUpdateWeaponAndArmorStats();
            UpdateEncumbrance();
            UpdatePlayerStatus();
            base.UpdateHealthManaEndu();
        }

        /// <summary>
        /// Get the bonus names
        /// </summary>
        public string ItemBonusName(int BonusType)
        {
            string BonusName = string.Empty;

            if (BonusType == 1) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus1");//Strength
            if (BonusType == 2) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus2");//Dexterity
            if (BonusType == 3) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus3");//Constitution
            if (BonusType == 4) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus4");//Quickness
            if (BonusType == 5) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus5");//Intelligence
            if (BonusType == 6) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus6");//Piety
            if (BonusType == 7) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus7");//Empathy
            if (BonusType == 8) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus8");//Charisma
            if (BonusType == 9) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus9");//Power
            if (BonusType == 10) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus10");//Hits
            if (BonusType == 11) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus11");//Body
            if (BonusType == 12) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus12");//Cold
            if (BonusType == 13) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus13");//Crush
            if (BonusType == 14) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus14");//Energy
            if (BonusType == 15) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus15");//Heat
            if (BonusType == 16) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus16");//Matter
            if (BonusType == 17) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus17");//Slash
            if (BonusType == 18) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus18");//Spirit
            if (BonusType == 19) BonusName = LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ItemBonusName.Bonus19");//Thrust
            return BonusName;
        }

        /// <summary>
        /// Adds magical bonuses whenever item was equipped
        /// </summary>
        public virtual void OnItemEquipped(DbInventoryItem item, eInventorySlot slot)
        {
            if (item == null)
                return;

            if (item is IGameInventoryItem inventoryItem)
                inventoryItem.OnEquipped(this);

            if (item.Item_Type is >= Slot.RIGHTHAND and <= Slot.RANGED)
            {
                if (item.Hand == 1) // 2h
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.WieldBothHands", item.GetName(0, false))), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                else if (item.SlotPosition == Slot.LEFTHAND)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.WieldLeftHand", item.GetName(0, false))), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                else
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.WieldRightHand", item.GetName(0, false))), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            if ((eInventorySlot) item.Item_Type == eInventorySlot.Horse)
            {
                if (item.SlotPosition == Slot.HORSE)
                {
                    ActiveHorse.ID = (byte) (item.SPD_ABS == 0 ? 1 : item.SPD_ABS);
                    ActiveHorse.Name = item.Creator;
                }

                return;
            }
            else if ((eInventorySlot) item.Item_Type == eInventorySlot.HorseArmor)
            {
                if (item.SlotPosition == Slot.HORSEARMOR)
                    ActiveHorse.Saddle = (byte) item.DPS_AF;

                return;
            }
            else if ((eInventorySlot) item.Item_Type == eInventorySlot.HorseBarding)
            {
                if (item.SlotPosition == Slot.HORSEBARDING)
                    ActiveHorse.Barding = (byte) item.DPS_AF;

                return;
            }

            if (!item.IsMagical)
                return;

            Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Magic", item.GetName(0, false))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);

            if (item.Bonus1 != 0)
            {
                ItemBonus[(eProperty) item.Bonus1Type] += item.Bonus1;

                if (item.Bonus1Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus1Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus2 != 0)
            {
                ItemBonus[(eProperty) item.Bonus2Type] += item.Bonus2;

                if (item.Bonus2Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus2Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus3 != 0)
            {
                ItemBonus[(eProperty) item.Bonus3Type] += item.Bonus3;

                if (item.Bonus3Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus3Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus4 != 0)
            {
                ItemBonus[(eProperty) item.Bonus4Type] += item.Bonus4;

                if (item.Bonus4Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus4Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus5 != 0)
            {
                ItemBonus[(eProperty) item.Bonus5Type] += item.Bonus5;

                if (item.Bonus5Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus5Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus6 != 0)
            {
                ItemBonus[(eProperty) item.Bonus6Type] += item.Bonus6;

                if (item.Bonus6Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus6Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus7 != 0)
            {
                ItemBonus[(eProperty) item.Bonus7Type] += item.Bonus7;

                if (item.Bonus7Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus7Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus8 != 0)
            {
                ItemBonus[(eProperty) item.Bonus8Type] += item.Bonus8;

                if (item.Bonus8Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus8Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus9 != 0)
            {
                ItemBonus[(eProperty) item.Bonus9Type] += item.Bonus9;

                if (item.Bonus9Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus9Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus10 != 0)
            {
                ItemBonus[(eProperty) item.Bonus10Type] += item.Bonus10;

                if (item.Bonus10Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemEquipped.Increased", ItemBonusName(item.Bonus10Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.ExtraBonus != 0)
                ItemBonus[(eProperty) item.ExtraBonusType] += item.ExtraBonus;

            if ((ePrivLevel) Client.Account.PrivLevel == ePrivLevel.Player && Client.Player != null && Client.Player.ObjectState == eObjectState.Active)
            {
                if (item.SpellID > 0 || item.SpellID1 > 0)
                    TempProperties.SetProperty("ITEMREUSEDELAY" + item.Id_nb, CurrentRegion.Time);
            }

            _statsSenderOnEquipmentChange ??= new(this, OnStatsSendCompletionAfterEquipmentChange);
        }

        private int m_activeBuffCharges = 0;

        public int ActiveBuffCharges
        {
            get
            {
                return m_activeBuffCharges;
            }
            set
            {
                m_activeBuffCharges = value;
            }
        }

        public static List<int> SelfBuffChargeIDs { get; } =
            [
                31133, // Strength/Constitution Charge
                31132, // Dexterity/Quickness Charge
                31131, // Acuity Charge
                31130  // AF Charge
            ];

        /// <summary>
        /// Removes magical bonuses whenever item was unequipped
        /// </summary>
        public virtual void OnItemUnequipped(DbInventoryItem item, eInventorySlot slot)
        {
            if (item == null)
                return;

            if (item.Item_Type is >= Slot.RIGHTHAND and <= Slot.RANGED)
            {
                if (item.Hand == 1) // 2h
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.BothHandsFree", item.GetName(0, false))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                else if (slot == eInventorySlot.LeftHandWeapon)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.LeftHandFree", item.GetName(0, false))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                else
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.RightHandFree", item.GetName(0, false))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (slot == eInventorySlot.Mythical && (eInventorySlot) item.Item_Type == eInventorySlot.Mythical && item is GameMythirian mythirian)
                mythirian.OnUnEquipped(this);

            if ((eInventorySlot) item.Item_Type == eInventorySlot.Horse)
            {
                if (IsOnHorse)
                    IsOnHorse = false;

                ActiveHorse.ID = 0;
                ActiveHorse.Name = string.Empty;
                return;
            }
            else if ((eInventorySlot) item.Item_Type == eInventorySlot.HorseArmor)
            {
                ActiveHorse.Saddle = 0;
                return;
            }
            else if ((eInventorySlot) item.Item_Type == eInventorySlot.HorseBarding)
            {
                ActiveHorse.Barding = 0;
                return;
            }

            // Cancel any self buffs that are unequipped.
            if (item.SpellID > 0 && SelfBuffChargeIDs.Contains(item.SpellID) && Inventory.EquippedItems.Where(x => x.SpellID == item.SpellID).Count() <= 1)
                CancelChargeBuff(item.SpellID);

            if (!item.IsMagical)
                return;

            if (item.Bonus1 != 0)
            {
                ItemBonus[(eProperty) item.Bonus1Type] -= item.Bonus1;

                if (item.Bonus1Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus1Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus2 != 0)
            {
                ItemBonus[(eProperty) item.Bonus2Type] -= item.Bonus2;

                if (item.Bonus2Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus2Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus3 != 0)
            {
                ItemBonus[(eProperty) item.Bonus3Type] -= item.Bonus3;

                if (item.Bonus3Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus3Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus4 != 0)
            {
                ItemBonus[(eProperty) item.Bonus4Type] -= item.Bonus4;

                if (item.Bonus4Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus4Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus5 != 0)
            {
                ItemBonus[(eProperty) item.Bonus5Type] -= item.Bonus5;

                if (item.Bonus5Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus5Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus6 != 0)
            {
                ItemBonus[(eProperty) item.Bonus6Type] -= item.Bonus6;

                if (item.Bonus6Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus6Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus7 != 0)
            {
                ItemBonus[(eProperty) item.Bonus7Type] -= item.Bonus7;

                if (item.Bonus7Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus7Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus8 != 0)
            {
                ItemBonus[(eProperty) item.Bonus8Type] -= item.Bonus8;

                if (item.Bonus8Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus8Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus9 != 0)
            {
                ItemBonus[(eProperty) item.Bonus9Type] -= item.Bonus9;

                if (item.Bonus9Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus9Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.Bonus10 != 0)
            {
                ItemBonus[(eProperty) item.Bonus10Type] -= item.Bonus10;

                if (item.Bonus10Type < 20)
                    Out.SendMessage(string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OnItemUnequipped.Decreased", ItemBonusName(item.Bonus10Type))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            if (item.ExtraBonus != 0)
                ItemBonus[(eProperty) item.ExtraBonusType] -= item.ExtraBonus;

            if (item is IGameInventoryItem inventoryItem)
                inventoryItem.OnUnEquipped(this);

            _statsSenderOnEquipmentChange ??= new(this, OnStatsSendCompletionAfterEquipmentChange);
        }

        private StatsSenderOnEquipmentChange _statsSenderOnEquipmentChange;

        private int OnStatsSendCompletionAfterEquipmentChange()
        {
            _statsSenderOnEquipmentChange = null;
            return 0;
        }

        public class StatsSenderOnEquipmentChange : ECSGameTimerWrapperBase
        {
            private new GamePlayer Owner { get; }
            private Func<int> _onCompletion;

            public StatsSenderOnEquipmentChange(GameObject owner, Func<int> OnCompletion) : base(owner)
            {
                Owner = owner as GamePlayer;
                _onCompletion = OnCompletion;
                Start(0);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (Owner.ObjectState is not eObjectState.Active)
                    return _onCompletion();

                Owner.Out.SendCharStatsUpdate();
                Owner.Out.SendCharResistsUpdate();
                Owner.Out.SendUpdateWeaponAndArmorStats();
                Owner.Out.SendUpdateMaxSpeed();
                Owner.Out.SendUpdatePlayerSkills(false);
                Owner.UpdateEncumbrance();
                Owner.UpdatePlayerStatus();

                if (!IsAlive)
                    return _onCompletion();

                int maxHealth = Owner.MaxHealth;

                if (Owner.Health < maxHealth)
                    Owner.StartHealthRegeneration();
                else if (Owner.Health > maxHealth)
                    Owner.Health = maxHealth;

                int maxMana = Owner.MaxMana;

                if (Owner.Mana < maxMana)
                    Owner.StartPowerRegeneration();
                else if (Owner.Mana > maxMana)
                    Owner.Mana = maxMana;

                int maxEndurance = Owner.MaxEndurance;

                if (Owner.Endurance < maxEndurance)
                    Owner.StartEnduranceRegeneration();
                else if (Owner.Endurance > maxEndurance)
                    Owner.Endurance = maxEndurance;

                return _onCompletion();
            }
        }

        private void CancelChargeBuff(int spellID)
        {
            effectListComponent.GetSpellEffects().FirstOrDefault(x => x.SpellHandler.Spell.ID == spellID)?.Stop();
        }

        public virtual void RefreshItemBonuses()
        {
            ItemBonus.Clear();
            string slotToLoad = string.Empty;
            switch (VisibleActiveWeaponSlots)
            {
                case 16: slotToLoad = "rightandleftHandSlot"; break;
                case 18: slotToLoad = "leftandtwoHandSlot"; break;
                case 31: slotToLoad = "leftHandSlot"; break;
                case 34: slotToLoad = "twoHandSlot"; break;
                case 51: slotToLoad = "distanceSlot"; break;
                case 240: slotToLoad = "righttHandSlot"; break;
                case 242: slotToLoad = "twoHandSlot"; break;
                default: break;
            }

            //log.Debug("VisibleActiveWeaponSlots= " + VisibleActiveWeaponSlots);
            foreach (DbInventoryItem item in Inventory.EquippedItems)
            {
                if (item == null)
                    continue;
                // skip weapons. only active weapons should fire equip event, done in player.SwitchWeapon
                bool add = true;
                if (slotToLoad != string.Empty)
                {
                    switch (item.SlotPosition)
                    {

                        case Slot.TWOHAND:
                            if (slotToLoad.Contains("twoHandSlot") == false)
                            {
                                add = false;
                            }
                            break;

                        case Slot.RIGHTHAND:
                            if (slotToLoad.Contains("right") == false)
                            {
                                add = false;
                            }
                            break;
                        case Slot.SHIELD:
                        case Slot.LEFTHAND:
                            if (slotToLoad.Contains("left") == false)
                            {
                                add = false;
                            }
                            break;
                        case Slot.RANGED:
                            if (slotToLoad != "distanceSlot")
                            {
                                add = false;
                            }
                            break;
                        default: break;
                    }
                }

                if (!add) continue;
                if (item is IGameInventoryItem)
                {
                    (item as IGameInventoryItem).CheckValid(this);
                }

                if (item.IsMagical)
                {
                    if (item.Bonus1 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus1Type] += item.Bonus1;
                    }
                    if (item.Bonus2 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus2Type] += item.Bonus2;
                    }
                    if (item.Bonus3 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus3Type] += item.Bonus3;
                    }
                    if (item.Bonus4 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus4Type] += item.Bonus4;
                    }
                    if (item.Bonus5 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus5Type] += item.Bonus5;
                    }
                    if (item.Bonus6 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus6Type] += item.Bonus6;
                    }
                    if (item.Bonus7 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus7Type] += item.Bonus7;
                    }
                    if (item.Bonus8 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus8Type] += item.Bonus8;
                    }
                    if (item.Bonus9 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus9Type] += item.Bonus9;
                    }
                    if (item.Bonus10 != 0)
                    {
                        ItemBonus[(eProperty) item.Bonus10Type] += item.Bonus10;
                    }
                    if (item.ExtraBonus != 0)
                    {
                        ItemBonus[(eProperty) item.ExtraBonusType] += item.ExtraBonus;
                    }
                }
            }
        }

        /// <summary>
        /// Handles a bonus change on an item.
        /// </summary>
        protected virtual void OnItemBonusChanged(eProperty bonusType, int bonusAmount)
        {
            if (bonusType == 0 || bonusAmount == 0)
                return;

            ItemBonus[bonusType] += bonusAmount;

            if (ObjectState is eObjectState.Active)
            {
                Out.SendCharStatsUpdate();
                Out.SendCharResistsUpdate();
                Out.SendUpdateWeaponAndArmorStats();
                Out.SendUpdateMaxSpeed();
                Out.SendEncumbrance();
                // Out.SendUpdatePlayerSkills();
                UpdatePlayerStatus();

                if (IsAlive)
                {
                    if (Health < MaxHealth)
                        StartHealthRegeneration();
                    else if (Health > MaxHealth)
                        Health = MaxHealth;

                    if (Mana < MaxMana)
                        StartPowerRegeneration();
                    else if (Mana > MaxMana)
                        Mana = MaxMana;

                    if (Endurance < MaxEndurance)
                        StartEnduranceRegeneration();
                    else if (Endurance > MaxEndurance)
                        Endurance = MaxEndurance;
                }
            }
        }

        #endregion

        #region ReceiveItem/DropItem/PickupObject

        /// <summary>
        /// Receive an item from another living
        /// </summary>
        /// <returns>true if player took the item</returns>
        public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
        {
            //if (item == null) return false;
            GamePlayer sourcePlayer = source as GamePlayer;
            if (item == null) return false;

            if (!Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item))
                return false;
            InventoryLogging.LogInventoryAction(source, this, eInventoryActionType.Trade, item.Template, item.Count);

            if (source == null)
            {
                Out.SendMessage(String.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ReceiveItem.Receive", item.GetName(0, false))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }
            else
            {
                if (source is GameNPC)
                    Out.SendMessage(String.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ReceiveItem.ReceiveFrom", item.GetName(0, false), source.GetName(0, false, Client.Account.Language, (source as GameNPC)))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                else
                    Out.SendMessage(String.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.ReceiveItem.ReceiveFrom", item.GetName(0, false), source.GetName(0, false))), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }

            //if (source is gameplayer)
            //{
            //	gameplayer sourceplayer = source as gameplayer;
            //	if (sourceplayer != null)
            //	{
            //		uint privlevel1 = client.account.privlevel;
            //		uint privlevel2 = sourceplayer.client.account.privlevel;
            //		if (privlevel1 != privlevel2
            //		    && (privlevel1 > 1 || privlevel2 > 1)
            //		    && (privlevel1 == 1 || privlevel2 == 1))
            //		{
            //			gameserver.instance.loggmaction("   item: " + source.name + "(" + sourceplayer.client.account.name + ") -> " + name + "(" + client.account.name + ") : " + item.name + "(" + item.id_nb + ")");
            //		}
            //	}
            //}
            //return true;
            if (sourcePlayer != null)
            {
                uint privLevel1 = Client.Account.PrivLevel;
                uint privLevel2 = sourcePlayer.Client.Account.PrivLevel;
                if (privLevel1 != privLevel2
                    && (privLevel1 > 1 || privLevel2 > 1)
                    && (privLevel1 == 1 || privLevel2 == 1))
                {
                    GameServer.Instance.LogGMAction("   Item: " + source.Name + "(" + sourcePlayer.Client.Account.Name + ") -> " + Name + "(" + Client.Account.Name + ") : " + item.Name + "(" + item.Id_nb + ")");
                }
            }
            return true;

        }

        /// <summary>
        /// Called to drop an Item from the Inventory to the floor
        /// </summary>
        /// <param name="slot_pos">SlotPosition to drop</param>
        /// <returns>true if dropped</returns>
        public virtual bool DropItem(eInventorySlot slot_pos)
        {
            WorldInventoryItem tempItem;
            return DropItem(slot_pos, out tempItem);
        }

        /// <summary>
        /// Called to drop an item from the Inventory to the floor
        /// and return the GameInventoryItem that is created on the floor
        /// </summary>
        /// <param name="slot_pos">SlotPosition to drop</param>
        /// <param name="droppedItem">out GameItem that was created</param>
        /// <returns>true if dropped</returns>
        public virtual bool DropItem(eInventorySlot slot_pos, out WorldInventoryItem droppedItem)
        {
            droppedItem = null;
            if (slot_pos >= eInventorySlot.FirstBackpack && slot_pos <= eInventorySlot.LastBackpack)
            {
                lock (Inventory.Lock)
                {
                    DbInventoryItem item = Inventory.GetItem(slot_pos);
                    if (!item.IsDropable)
                    {
                        Out.SendMessage(item.GetName(0, true) + " can not be dropped!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    if (!Inventory.RemoveItem(item)) return false;
                    InventoryLogging.LogInventoryAction(this, "(ground)", eInventoryActionType.Other, item.Template, item.Count);

                    droppedItem = CreateItemOnTheGround(item);

                    if (droppedItem != null)
                    {
                        Notify(PlayerInventoryEvent.ItemDropped, this, new ItemDroppedEventArgs(item, droppedItem));
                    }

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// called to make an item on the ground with owner is player
        /// </summary>
        /// <param name="item">the item to create on the ground</param>
        /// <returns>the GameInventoryItem on the ground</returns>
        public virtual WorldInventoryItem CreateItemOnTheGround(DbInventoryItem item)
        {
            WorldInventoryItem gameItem;

            if (item is IGameInventoryItem)
                gameItem = (item as IGameInventoryItem).Drop(this);
            else
            {
                gameItem = new PlayerDiscardedWorldInventoryItem(item);
                Point2D loc = GetPointFromHeading(Heading, 30);
                gameItem.X = loc.X;
                gameItem.Y = loc.Y;
                gameItem.Z = Z;
                gameItem.Heading = Heading;
                gameItem.CurrentRegionID = CurrentRegionID;
                gameItem.CurrentHouse = CurrentHouse;

                if (gameItem.CurrentHouse != null)
                    gameItem.InHouse = true;

                gameItem.AddOwner(this);
                gameItem.AddToWorld();
            }

            return gameItem;
        }

        /// <summary>
        /// Called when the player tries to pick up an object
        /// </summary>
        public virtual void PickupObject(GameObject floorObject, bool checkRange)
        {
            if (floorObject == null)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.MustHaveTarget"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (floorObject.ObjectState is not eObjectState.Active)
                return;

            if (floorObject is GameStaticItemTimed staticItem &&
                !staticItem.IsOwner(this) &&
                (ePrivLevel) Client.Account.PrivLevel <= ePrivLevel.Player)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.LootDoesntBelongYou"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (floorObject is not GameBoat && !checkRange && !floorObject.IsWithinRadius(this, Properties.WORLD_PICKUP_DISTANCE, true))
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.ObjectTooFarAway", floorObject.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (floorObject is WorldInventoryItem floorItem)
            {
                if (floorItem.Item == null || !floorItem.Item.IsPickable)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.CantGetThat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (floorItem.GetPickupTime > 0)
                {
                    Out.SendMessage($"You must wait another {floorItem.GetPickupTime / 1000} seconds to pick up {floorItem.Name}!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                BattleGroup battleGroup = TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);

                if (battleGroup == null || floorItem.TryPickUp(this, battleGroup) is TryPickUpResult.DoesNotWant)
                {
                    Group group = Group;

                    if (group == null || floorItem.TryPickUp(this, group) is TryPickUpResult.DoesNotWant)
                        floorItem.TryPickUp(this, this);
                }

                return;
            }

            if (floorObject is GameMoney money)
            {
                BattleGroup battleGroup = TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);

                if (battleGroup == null || money.TryPickUp(this, battleGroup) is TryPickUpResult.DoesNotWant)
                {
                    Group group = Group;

                    if (group == null || money.TryPickUp(this, group) is TryPickUpResult.DoesNotWant)
                        money.TryPickUp(this, this);
                }

                return;
            }

            if (floorObject is GameBoat)
            {
                if (!IsWithinRadius(floorObject, 1000))
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.TooFarFromBoat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (!InCombat)
                    MountSteed(floorObject as GameBoat, false);

                return;
            }

            if (floorObject is GameConsignmentMerchant)
            {
                floorObject.CurrentHouse.PickUpConsignmentMerchant(this);
                return;
            }

            if (floorObject is GameHouseVault && floorObject.CurrentHouse != null)
            {
                GameHouseVault houseVault = floorObject as GameHouseVault;
                if (houseVault.Detach(this))
                {
                    DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(houseVault.TemplateID);
                    Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(template));
                    InventoryLogging.LogInventoryAction("(HOUSE;" + floorObject.CurrentHouse.HouseNumber + ")", this, eInventoryActionType.Other, template);
                }
                return;
            }

            if ((floorObject is GameNPC || floorObject is GameStaticItem) && floorObject.CurrentHouse != null)
            {
                floorObject.CurrentHouse.EmptyHookpoint(this, floorObject);
                return;
            }

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.CantGetThat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }

        public object GameStaticItemOwnerComparand => AccountName;

        public TryPickUpResult TryAutoPickUpMoney(GameMoney money)
        {
            return Autoloot ? TryPickUpMoney(this, money) : TryPickUpResult.DoesNotWant;
        }

        public TryPickUpResult TryAutoPickUpItem(WorldInventoryItem inventoryItem)
        {
            return Autoloot ? TryPickUpItem(this, inventoryItem) : TryPickUpResult.DoesNotWant;
        }

        public TryPickUpResult TryPickUpMoney(GamePlayer source, GameMoney money)
        {
            money.AssertLockAcquisition();

            if (this != source)
            {
                if (log.IsErrorEnabled)
                    log.Error($"The passed down {nameof(source)} isn't equal to 'this'. Money pick up aborted. ({nameof(source)}: {source}) (this: {this})");

                return TryPickUpResult.DoesNotWant;
            }

            long moneyToPlayer = ApplyGuildDues(money.Value);

            if (moneyToPlayer > 0)
            {
                AddMoney(moneyToPlayer, LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.YouPickUp", Money.GetString(moneyToPlayer)));
                InventoryLogging.LogInventoryAction("(ground)", this, eInventoryActionType.Loot, moneyToPlayer);
            }

            money.RemoveFromWorld();
            return TryPickUpResult.Success;
        }

        public TryPickUpResult TryPickUpItem(GamePlayer source, WorldInventoryItem item)
        {
            item.AssertLockAcquisition();

            if (this != source)
            {
                if (log.IsErrorEnabled)
                    log.Error($"The passed down {nameof(source)} isn't equal to 'this'. Item pick up aborted. ({nameof(source)}: {source}) (this: {this})");

                return TryPickUpResult.DoesNotWant;
            }

            if (!GiveItem(this, item.Item))
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.BackpackFull"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return TryPickUpResult.Blocked;
            }

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.YouGet", item.Item.GetName(1, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            Message.SystemToOthers(this, LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.PickupObject.GroupMemberPicksUp", Name, item.Item.GetName(1, false)), eChatType.CT_System);
            InventoryLogging.LogInventoryAction("(ground)", this, eInventoryActionType.Loot, item.Item.Template, item.Item.IsStackable ? item.Item.Count : 1);
            item.RemoveFromWorld();
            return TryPickUpResult.Success;

            static bool GiveItem(GamePlayer player, DbInventoryItem item)
            {
                if (item.IsStackable)
                    return player.Inventory.AddTemplate(item, item.Count, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

                return player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);
            }
        }

        /// <summary>
        /// Checks to see if an object is viewable from the players perspective
        /// </summary>
        /// <param name="obj">The Object to be seen</param>
        /// <returns>True/False</returns>
        public bool CanSeeObject(GameObject obj)
        {
            return IsWithinRadius(obj, WorldMgr.VISIBILITY_DISTANCE);
        }

        /// <summary>
        /// Checks to see if an object is viewable from the players perspective
        /// </summary>
        /// <param name="player">The Player that can see</param>
        /// <param name="obj">The Object to be seen</param>
        /// <returns>True/False</returns>
        public static bool CanSeeObject(GamePlayer player, GameObject obj)
        {
            return player.IsWithinRadius(obj, WorldMgr.VISIBILITY_DISTANCE);
        }

        #endregion

        #region Database

        /// <summary>
        /// Subtracts the current time from the last time the character was saved
        /// and adds it in the form of seconds to player.PlayedTime
        /// for the /played command.
        /// </summary>
        public long PlayedTime
        {
            get
            {
                DateTime rightNow = DateTime.Now;
                DateTime oldLast = LastPlayed;
                // Get the total amount of time played between now and lastplayed
                // This is safe as lastPlayed is updated on char load.
                TimeSpan playaPlayed = rightNow.Subtract(oldLast);
                TimeSpan newPlayed = playaPlayed + TimeSpan.FromSeconds(DBCharacter.PlayedTime);
                return (long)newPlayed.TotalSeconds;
            }
        }

        /// <summary>
        /// Subtracts the current time from the last time the character was saved
        /// and adds it in the form of seconds to player.PlayedTime
        /// for the /played level command.
        /// </summary>
        public long PlayedTimeSinceLevel
        {
            get
            {
                DateTime rightNow = DateTime.Now;
                DateTime oldLast = LastPlayed;
                // Get the total amount of time played between now and lastplayed
                // This is safe as lastPlayed is updated on char load.
                TimeSpan playaPlayed = rightNow.Subtract(oldLast);
                TimeSpan newPlayed = playaPlayed + TimeSpan.FromSeconds(DBCharacter.PlayedTimeSinceLevel);
                return (long)newPlayed.TotalSeconds;
            }
        }

        /// <summary>
        /// Saves the player's skills
        /// </summary>
        protected virtual void SaveSkillsToCharacter()
        {
            StringBuilder ab = new StringBuilder();
            StringBuilder sp = new StringBuilder();

            // Build Serialized Spec list
            List<Specialization> specs = null;
            lock (_specializationLock)
            {
                specs = m_specialization.Values.Where(s => s.AllowSave).ToList();
                foreach (Specialization spec in specs)
                {
                    if (sp.Length > 0)
                    {
                        sp.Append(";");
                    }
                    sp.AppendFormat("{0}|{1}", spec.KeyName, spec.GetSpecLevelForLiving(this));
                }
            }

            // Build Serialized Ability List to save Order
            foreach (Ability ability in m_usableSkills.Where(e => e.Item1 is Ability).Select(e => e.Item1).Cast<Ability>())
            {					
                if (ability != null)
                {
                    if (ab.Length > 0)
                    {
                        ab.Append(";");
                    }
                    ab.AppendFormat("{0}|{1}", ability.KeyName, ability.Level);
                }
            }

            // Build Serialized disabled Spell/Ability
            StringBuilder disabledSpells = new StringBuilder();
            StringBuilder disabledAbilities = new StringBuilder();
            ICollection<Skill> disabledSkills = GetAllDisabledSkills();

            foreach (Skill skill in disabledSkills)
            {
                int duration = GetSkillDisabledDuration(skill);

                if (duration <= 0)
                    continue;

                if (skill is Spell)
                {
                    Spell spl = (Spell)skill;

                    if (disabledSpells.Length > 0)
                        disabledSpells.Append(";");

                    disabledSpells.AppendFormat("{0}|{1}", spl.ID, duration);
                }
                else if (skill is Ability)
                {
                    Ability ability = (Ability)skill;

                    if (disabledAbilities.Length > 0)
                        disabledAbilities.Append(";");

                    disabledAbilities.AppendFormat("{0}|{1}", ability.KeyName, duration);
                }
                else
                {
                    if (log.IsWarnEnabled)
                        log.WarnFormat("{0}: Can't save disabled skill {1}", Name, skill.GetType().ToString());
                }
            }

            StringBuilder sra = new StringBuilder();

            foreach (RealmAbility rab in m_realmAbilities)
            {
                if (sra.Length > 0)
                    sra.Append(";");

                sra.AppendFormat("{0}|{1}", rab.KeyName, rab.Level);
            }

            if (DBCharacter != null)
            {
                DBCharacter.SerializedAbilities = ab.ToString();
                DBCharacter.SerializedSpecs = sp.ToString();
                DBCharacter.SerializedRealmAbilities = sra.ToString();
                DBCharacter.DisabledSpells = disabledSpells.ToString();
                DBCharacter.DisabledAbilities = disabledAbilities.ToString();
            }
        }

        /// <summary>
        /// Load this player Classes Specialization.
        /// </summary>
        public virtual void LoadClassSpecializations(bool sendMessages)
        {
            // Get this Attached Class Specialization from SkillBase.
            IDictionary<Specialization, int> careers = SkillBase.GetSpecializationCareer(CharacterClass.ID);

            // Remove All Trainable Specialization or "Career Spec" that aren't managed by This Data Career anymore
            var speclist = GetSpecList();
            var careerslist = careers.Keys.Select(k => k.KeyName.ToLower());
            foreach (var spec in speclist.Where(sp => sp.Trainable || !sp.AllowSave))
            {
                if (!careerslist.Contains(spec.KeyName.ToLower()))
                    RemoveSpecialization(spec.KeyName);
            }

            // sort ML Spec depending on ML Line
            byte mlindex = 0;
            foreach (KeyValuePair<Specialization, int> constraint in careers)
            {
                if (constraint.Key is IMasterLevelsSpecialization)
                {
                    if (mlindex != MLLine)
                    {
                        if (HasSpecialization(constraint.Key.KeyName))
                            RemoveSpecialization(constraint.Key.KeyName);

                        mlindex++;
                        continue;
                    }

                    mlindex++;

                    if (!MLGranted || MLLevel < 1)
                    {
                        continue;
                    }
                }

                // load if the spec doesn't exists
                if (Level >= constraint.Value)
                {
                    if (!HasSpecialization(constraint.Key.KeyName))
                        AddSpecialization(constraint.Key, sendMessages);
                }
                else
                {
                    if (HasSpecialization(constraint.Key.KeyName))
                        RemoveSpecialization(constraint.Key.KeyName);
                }
            }
        }

        /// <summary>
        /// Verify this player has the correct number of spec points for the players level
        /// </summary>
        public virtual int VerifySpecPoints()
        {
            // calc normal spec points for the level & classe
            int allpoints = -1;
            for (int i = 1; i <= Level; i++)
            {
                if (i <= 5) allpoints += i; //start levels
                if (i > 5) allpoints += CharacterClass.SpecPointsMultiplier * i / 10; //normal levels
                if (i > 40) allpoints += CharacterClass.SpecPointsMultiplier * (i - 1) / 20; //half levels
            }
            if (IsLevelSecondStage && Level != MAX_LEVEL)
                allpoints += CharacterClass.SpecPointsMultiplier * Level / 20; // add current half level

            // calc spec points player have (autotrain is not anymore processed here - 1.87 livelike)
            int usedpoints = 0;
            foreach (Specialization spec in GetSpecList().Where(e => e.Trainable))
            {
                usedpoints += (spec.Level * (spec.Level + 1) - 2) / 2;
                usedpoints -= GetAutoTrainPoints(spec, 0);
            }

            allpoints -= usedpoints;

            // check if correct, if not respec. Not applicable to GMs
            if (allpoints < 0)
            {
                if (Client.Account.PrivLevel == 1)
                {
                    log.WarnFormat("Spec points total for player {0} incorrect: {1} instead of {2}.", Name, usedpoints, allpoints+usedpoints);
                    RespecAllLines();
                    return allpoints+usedpoints;
                }
            }

            return allpoints;
        }

        public async Task LoadFromDatabaseAsync(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            if (obj is not DbCoreCharacter dbCoreCharacter)
                return;

            m_dbCharacter = dbCoreCharacter;
            m_previousLoginDate = DBCharacter.LastPlayed;
            DBCharacter.LastPlayed = DateTime.Now; // Has to be updated on load to ensure time offline isn't added to character /played.
            IsMuted = Client.Account.IsMuted; // Account mutes are persistent.

            // Prepare the tasks.
            var moneyForRealmTask = DOLDB<DbAccountXMoney>.SelectObjectAsync(DB.Column("AccountID").IsEqualTo(Client.Account.ObjectId).And(DB.Column("Realm").IsEqualTo(Realm)));
            var inventoryTask = Inventory.StartLoadFromDatabaseTask(InternalID);
            var craftingForRealmTask = DOLDB<DbAccountXCrafting>.SelectObjectAsync(DB.Column("AccountID").IsEqualTo(AccountName).And(DB.Column("Realm").IsEqualTo(Realm)));
            var scriptedQuestsTask = DOLDB<DbQuest>.SelectObjectsAsync(DB.Column("Character_ID").IsEqualTo(QuestPlayerID));
            var dataQuestsTask = LoadDataQuestsAsync();
            var factionRelationsTask = DOLDB<DbFactionAggroLevel>.SelectObjectsAsync(DB.Column("CharacterID").IsEqualTo(ObjectId));
            var tasksTask = DOLDB<DbTask>.SelectObjectsAsync(DB.Column("Character_ID").IsEqualTo(InternalID));
            var masterLevelsTask = DOLDB<DbCharacterXMasterLevel>.SelectObjectsAsync(DB.Column("Character_ID").IsEqualTo(QuestPlayerID));

            SetCharacterClass(DBCharacter.Class);
            HandleWorldPosition();
            HandleCharacterModel();
            HandleGuild();
            HandleMoney(await moneyForRealmTask);
            HandleInventory(await inventoryTask);
            HandleCharacterSkills();
            HandleCraftingSkills(await craftingForRealmTask);
            HandleQuests(await scriptedQuestsTask, await dataQuestsTask);
            FactionMgr.LoadAllAggroToFaction(this, await factionRelationsTask);
            HandleTasks(await tasksTask);
            HandleMasterLevels(await masterLevelsTask);
            HandleStats(); // Should be done after loading gear, abilities, buffs.
            HandleTitles(); // Should be done after loading crafting skills.

            VerifySpecPoints();
            GuildMgr.AddPlayerToGuildMemberViews(this); // Needed for starter guilds since they are forced onto the `DBCharacter`.
            styleComponent.OnPlayerLoadFromDatabase(); // Sets `AutomaticBackupStyle`.

            async Task<DataQuest[]> LoadDataQuestsAsync()
            {
                var characterDataQuests = await DOLDB<DbCharacterXDataQuest>.SelectObjectsAsync(DB.Column("Character_ID").IsEqualTo(QuestPlayerID));

                var innerTasks = characterDataQuests.Select(async characterQuest =>
                {
                    var dbDataQuest = await DOLDB<DbDataQuest>.SelectObjectAsync(DB.Column("DataQuestID").IsEqualTo(characterQuest.DataQuestID));

                    if (dbDataQuest == null || (DataQuest.eStartType)dbDataQuest.StartType is DataQuest.eStartType.Collection)
                        return null;

                    return new DataQuest(this, dbDataQuest, characterQuest);
                });

                DataQuest[] completedQuests = await Task.WhenAll(innerTasks);
                return completedQuests.Where(q => q != null).ToArray();
            }

            void HandleWorldPosition()
            {
                m_x = DBCharacter.Xpos;
                m_y = DBCharacter.Ypos;
                m_z = DBCharacter.Zpos;
                Heading = (ushort) DBCharacter.Direction;
                CurrentRegionID = (ushort) DBCharacter.Region; // Sets `Region` too.

                if (CurrentRegion == null || CurrentRegion.GetZone(m_x, m_y) == null)
                {
                    log.WarnFormat("Invalid region/zone on char load ({0}): x={1} y={2} z={3} reg={4}; moving to bind point.", DBCharacter.Name, X, Y, Z, DBCharacter.Region);
                    m_x = DBCharacter.BindXpos;
                    m_y = DBCharacter.BindYpos;
                    m_z = DBCharacter.BindZpos;
                    Heading = (ushort) DBCharacter.BindHeading;
                    CurrentRegionID = (ushort) DBCharacter.BindRegion;
                }
            }

            void HandleCharacterModel()
            {
                Model = (ushort) DBCharacter.CurrentModel;
                m_customFaceAttributes[(int) eCharFacePart.EyeSize] = DBCharacter.EyeSize;
                m_customFaceAttributes[(int) eCharFacePart.LipSize] = DBCharacter.LipSize;
                m_customFaceAttributes[(int) eCharFacePart.EyeColor] = DBCharacter.EyeColor;
                m_customFaceAttributes[(int) eCharFacePart.HairColor] = DBCharacter.HairColor;
                m_customFaceAttributes[(int) eCharFacePart.FaceType] = DBCharacter.FaceType;
                m_customFaceAttributes[(int) eCharFacePart.HairStyle] = DBCharacter.HairStyle;
                m_customFaceAttributes[(int) eCharFacePart.MoodType] = DBCharacter.MoodType;
            }

            void HandleGuild()
            {
                m_guildId = DBCharacter.GuildID;

                if (m_guildId != null)
                {
                    m_guild = GuildMgr.GetGuildByGuildID(m_guildId);

                    // If the guild is not found, we need to refresh the emblem ourselves.
                    // Otherwise this is done in `AddOnlineMember`.
                    if (m_guild == null)
                        GuildMgr.RefreshPersonalHouseEmblem(this);
                    else
                    {
                        foreach (DbGuildRank rank in m_guild.Ranks)
                        {
                            if (rank == null)
                                continue;

                            if (rank.RankLevel == DBCharacter.GuildRank)
                            {
                                m_guildRank = rank;
                                break;
                            }
                        }

                        m_guildName = m_guild.Name;
                        m_guild.AddOnlineMember(this);
                    }
                }
                else
                    m_guild = null;
            }

            void HandleMoney(DbAccountXMoney moneyForRealm)
            {
                if (moneyForRealm == null)
                {
                    int realmMithril = 0;
                    int realmPlatinum = 0;
                    int realmGold = 0;
                    int realmSilver = 0;
                    int realmCopper = 0;

                    DbAccountXMoney newMoney = new()
                    {
                        AccountId = Client.Account.ObjectId,
                        Realm = (int) Realm
                    };

                    foreach (DbCoreCharacter character in Client.Account.Characters)
                    {
                        if ((eRealm) character.Realm == Realm)
                        {
                            realmCopper += character.Copper;
                            realmSilver += character.Silver;
                            realmGold += character.Gold;
                            realmPlatinum += character.Platinum;
                            realmMithril += character.Mithril;

                            if (realmCopper > 100)
                            {
                                realmCopper -= 100;
                                realmSilver += 1;
                            }

                            if (realmSilver > 100)
                            {
                                realmSilver -= 100;
                                realmGold += 1;
                            }

                            if (realmGold > 1000)
                            {
                                realmGold -= 1000;
                                realmPlatinum += 1;
                            }
                        }
                    }

                    newMoney.Copper = realmCopper;
                    newMoney.Silver = realmSilver;
                    newMoney.Gold = realmGold;
                    newMoney.Platinum = realmPlatinum;
                    newMoney.Mithril = realmMithril;

                    GameServer.Database.AddObject(newMoney);
                    moneyForRealm = newMoney;
                }

                m_Copper = moneyForRealm.Copper;
                m_Silver = moneyForRealm.Silver;
                m_Gold = moneyForRealm.Gold;
                m_Platinum = moneyForRealm.Platinum;
                m_Mithril = moneyForRealm.Mithril;
            }

            void HandleInventory(IList items)
            {
                Inventory.LoadInventory(InternalID, items);
            }

            void HandleCharacterSkills()
            {
                LoadClassSpecializations(false);
                string tmpStr = DBCharacter.SerializedSpecs;

                if (tmpStr != null && tmpStr.Length > 0)
                {
                    foreach (string spec in Util.SplitCSV(tmpStr))
                    {
                        string[] values = spec.Split('|');

                        if (values.Length >= 2)
                        {
                            Specialization tempSpec = SkillBase.GetSpecialization(values[0], false);

                            if (tempSpec != null)
                            {
                                if (tempSpec.AllowSave)
                                {
                                    if (int.TryParse(values[1], out int level))
                                    {
                                        if (HasSpecialization(tempSpec.KeyName))
                                            GetSpecializationByName(tempSpec.KeyName).Level = level;
                                        else
                                        {
                                            tempSpec.Level = level;
                                            AddSpecialization(tempSpec, false);
                                        }
                                    }
                                    else if (log.IsErrorEnabled)
                                        log.Error($"{Name}: error in loading specs => '{tmpStr}'");
                                }
                            }
                            else if (log.IsErrorEnabled)
                                log.Error($"{Name}: can't find spec '{values[0]}'");
                        }
                    }
                }

                tmpStr = DBCharacter.SerializedAbilities;

                if (tmpStr != null && tmpStr.Length > 0 && m_usableSkills.Count == 0)
                {
                    foreach (string abilities in Util.SplitCSV(tmpStr))
                    {
                        string[] values = abilities.Split('|');

                        if (values.Length >= 2)
                        {
                            if (int.TryParse(values[1], out int level))
                            {
                                Ability ability = SkillBase.GetAbility(values[0], level);

                                if (ability != null)
                                    m_usableSkills.Add(new Tuple<Skill, Skill>(ability, ability));
                            }
                        }
                    }
                }

                tmpStr = DBCharacter.SerializedRealmAbilities;

                if (tmpStr != null && tmpStr.Length > 0)
                {
                    foreach (string abilities in Util.SplitCSV(tmpStr))
                    {
                        string[] values = abilities.Split('|');

                        if (values.Length >= 2)
                        {
                            if (int.TryParse(values[1], out int level))
                            {
                                Ability ability = SkillBase.GetAbility(values[0], level);

                                if (ability is RealmAbility realmAbility)
                                    m_realmAbilities.Add(realmAbility);
                            }
                        }
                    }
                }

                RefreshSpecDependantSkills(false);
                tmpStr = DBCharacter.DisabledAbilities;

                if (tmpStr != null && tmpStr.Length > 0)
                {
                    foreach (string str in Util.SplitCSV(tmpStr))
                    {
                        string[] values = str.Split('|');

                        if (values.Length >= 2)
                        {
                            string key= values[0];

                            if (HasAbility(key) && int.TryParse(values[1], out int duration))
                                DisableSkill(GetAbility(key), duration);
                            else if (log.IsErrorEnabled)
                                log.Error($"{Name}: error in loading disabled abilities => '{tmpStr}'");
                        }
                    }
                }

                tmpStr = DBCharacter.DisabledSpells;

                if (!string.IsNullOrEmpty(tmpStr))
                {
                    foreach (string str in Util.SplitCSV(tmpStr))
                    {
                        string[] values = str.Split('|');

                        if (values.Length >= 2 && int.TryParse(values[0], out int spellId) && int.TryParse(values[1], out int duration))
                        {
                            Spell sp = SkillBase.GetSpellByID(spellId);

                            if (sp != null)
                                DisableSkill(sp, duration);
                        }
                        else if (log.IsErrorEnabled)
                            log.ErrorFormat("{0}: error in loading disabled spells => '{1}'", Name, tmpStr);
                    }
                }

                CharacterClass.OnLevelUp(this, Level);
                CharacterClass.OnRealmLevelUp(this);
            }

            void HandleCraftingSkills(DbAccountXCrafting craftingForRealm)
            {
                if (craftingForRealm == null)
                {
                    DbAccountXCrafting newCrafting = new()
                    {
                        AccountId = this.AccountName,
                        Realm = (int) this.Realm,
                        CraftingPrimarySkill = 15
                    };
                    GameServer.Database.AddObject(newCrafting);
                    craftingForRealm = newCrafting;
                }
                try
                {
                    CraftingPrimarySkill = (eCraftingSkill) craftingForRealm.CraftingPrimarySkill;

                    lock (_craftingLock)
                    {
                        foreach (string skill in Util.SplitCSV(craftingForRealm.SerializedCraftingSkills))
                        {
                            string[] values = skill.Split('|');

                            if (values[0].Length > 3)
                            {
                                // Load by crafting skill name.
                                int i = 0;

                                switch (values[0])
                                {
                                    case "WeaponCrafting": i = 1; break;
                                    case "ArmorCrafting": i = 2; break;
                                    case "SiegeCrafting": i = 3; break;
                                    case "Alchemy": i = 4; break;
                                    case "MetalWorking": i = 6; break;
                                    case "LeatherCrafting": i = 7; break;
                                    case "ClothWorking": i = 8; break;
                                    case "GemCutting": i = 9; break;
                                    case "HerbalCrafting": i = 10; break;
                                    case "Tailoring": i = 11; break;
                                    case "Fletching": i = 12; break;
                                    case "SpellCrafting": i = 13; break;
                                    case "WoodWorking": i = 14; break;
                                    case "BasicCrafting": i = 15; break;
                                }

                                if (!m_craftingSkills.ContainsKey((eCraftingSkill)i))
                                {
                                    if (IsCraftingSkillDefined(Convert.ToInt32(values[0])))
                                    {
                                        if (Properties.CRAFTING_MAX_SKILLS)
                                            m_craftingSkills.Add((eCraftingSkill) Convert.ToInt32(values[0]), Properties.CRAFTING_MAX_SKILLS_AMOUNT);
                                        else
                                            m_craftingSkills.Add((eCraftingSkill) i, Convert.ToInt32(values[1]));
                                    }
                                    else
                                    {
                                        if (log.IsErrorEnabled)
                                            log.Error($"Tried to load invalid CraftingSkill: {values[0]}");
                                    }
                                }
                            }
                            else if (!m_craftingSkills.ContainsKey((eCraftingSkill) Convert.ToInt32(values[0])))
                            {
                                // Load by ID.
                                if (IsCraftingSkillDefined(Convert.ToInt32(values[0])))
                                {
                                    if (Properties.CRAFTING_MAX_SKILLS)
                                        m_craftingSkills.Add((eCraftingSkill) Convert.ToInt32(values[0]), Properties.CRAFTING_MAX_SKILLS_AMOUNT);
                                    else
                                        m_craftingSkills.Add((eCraftingSkill) Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
                                }
                                else
                                {
                                    if (log.IsErrorEnabled)
                                        log.Error($"Tried to load invalid CraftingSkill: {values[0]}");
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"{Name}: error in loading playerCraftingSkills => {DBCharacter.SerializedCraftingSkills}", e);
                }

                bool IsCraftingSkillDefined(int craftingSkillToCheck)
                {
                    return Enum.IsDefined(typeof(eCraftingSkill), craftingSkillToCheck);
                }
            }

            void HandleQuests(IList<DbQuest> scriptedQuests, DataQuest[] dataQuests)
            {
                AvailableQuestIndexes.Clear();
                QuestList.Clear();
                _questListFinished.Clear();
                int activeQuestCount = 0;

                foreach (DbQuest dbQuest in scriptedQuests)
                {
                    AbstractQuest quest = AbstractQuest.LoadFromDatabase(this, dbQuest);

                    if (quest == null)
                        continue;

                    if (quest.Step < 0)
                        AddFinishedQuest(quest);
                    else
                        QuestList.TryAdd(quest, (byte) activeQuestCount++);

                    switch (quest)
                    {
                        case Quests.DailyQuest dailyQuest:
                        {
                            dailyQuest.LoadQuestParameters();
                            break;
                        }
                        case Quests.WeeklyQuest weeklyQuest:
                        {
                            weeklyQuest.LoadQuestParameters();
                            break;
                        }
                        case Quests.MonthlyQuest monthlyQuest:
                        {
                            monthlyQuest.LoadQuestParameters();
                            break;
                        }
                    }
                }

                foreach (DataQuest dataQuest in dataQuests)
                {
                    if (dataQuest.Step > 0)
                        QuestList.TryAdd(dataQuest, (byte) activeQuestCount++);
                    else if (dataQuest.Count > 0)
                        _questListFinished.Add(dataQuest);
                }
            }

            void HandleTasks(IList<DbTask> tasks)
            {
                if (tasks.Count == 1)
                    m_task = AbstractTask.LoadFromDatabase(this, tasks[0]);
                else if (tasks.Count > 1)
                {
                    if (log.IsErrorEnabled)
                        log.Error("More than one DBTask Object found for player " + Name);
                }
            }

            void HandleMasterLevels(IList<DbCharacterXMasterLevel> masterLevels)
            {
                foreach (DbCharacterXMasterLevel masterLevel in masterLevels)
                    m_mlSteps.Add(masterLevel);
            }

            void HandleStats()
            {
                m_charStat[eStat.STR - eStat._First] = (short) DBCharacter.Strength;
                m_charStat[eStat.DEX - eStat._First] = (short) DBCharacter.Dexterity;
                m_charStat[eStat.CON - eStat._First] = (short) DBCharacter.Constitution;
                m_charStat[eStat.QUI - eStat._First] = (short) DBCharacter.Quickness;
                m_charStat[eStat.INT - eStat._First] = (short) DBCharacter.Intelligence;
                m_charStat[eStat.PIE - eStat._First] = (short) DBCharacter.Piety;
                m_charStat[eStat.EMP - eStat._First] = (short) DBCharacter.Empathy;
                m_charStat[eStat.CHR - eStat._First] = (short) DBCharacter.Charisma;

                if (MaxSpeedBase == 0)
                    MaxSpeedBase = PLAYER_BASE_SPEED;

                if (DBCharacter.PlayedTime < 1)
                {
                    Health = MaxHealth;
                    Mana = MaxMana;
                    Endurance = MaxEndurance;
                }
                else
                {
                    Health = DBCharacter.Health;
                    Mana = DBCharacter.Mana;
                    Endurance = DBCharacter.Endurance;
                }

                if (Health <= 0)
                    Health = 1;

                if (RealmLevel == 0)
                    RealmLevel = CalculateRealmLevelFromRPs(RealmPoints);
            }

            void HandleTitles()
            {
                m_titles.Clear();

                foreach (IPlayerTitle title in PlayerTitleMgr.GetPlayerTitles(this))
                    m_titles.Add(title);

                m_currentTitle = PlayerTitleMgr.GetTitleByTypeName(DBCharacter.CurrentTitleType) ?? PlayerTitleMgr.ClearTitle;
            }
        }

        /// <summary>
        /// Loads this player from a character table slot
        /// </summary>
        /// <param name="obj">DOLCharacter</param>
        public override void LoadFromDatabase(DataObject obj)
        {
            GameLoopAsyncHelper.Wait(LoadFromDatabaseAsync(obj));
        }

        /// <summary>
        /// Save the player into the database
        /// </summary>
        public override void SaveIntoDatabase()
        {
            try
            {
                DbAccountXMoney MoneyForRealm = DOLDB<DbAccountXMoney>.SelectObject(DB.Column("AccountID").IsEqualTo(this.Client.Account.ObjectId).And(DB.Column("Realm").IsEqualTo(this.Realm)));

                if (MoneyForRealm == null)
                {
                    DbAccountXMoney newMoney = new DbAccountXMoney();
                    newMoney.AccountId = this.Client.Account.ObjectId;
                    newMoney.Realm = (int)this.Realm;
                    newMoney.Copper = DBCharacter.Copper;
                    newMoney.Silver = DBCharacter.Silver;
                    newMoney.Gold = DBCharacter.Gold;
                    newMoney.Platinum = DBCharacter.Platinum;
                    GameServer.Database.AddObject(newMoney);
                    MoneyForRealm = newMoney;
                }
                else
                {
                    MoneyForRealm.Copper = Copper;
                    MoneyForRealm.Silver = Silver;
                    MoneyForRealm.Gold = Gold;
                    MoneyForRealm.Platinum = Platinum;
                    MoneyForRealm.Mithril = Mithril;
                    GameServer.Database.SaveObject(MoneyForRealm);
                }

                // Ff this player is a GM always check and set the IgnoreStatistics flag
                if (Client.Account.PrivLevel > (uint)ePrivLevel.Player && DBCharacter.IgnoreStatistics == false)
                {
                    DBCharacter.IgnoreStatistics = true;
                }

                //Save realmtimer
                RealmTimer.SaveRealmTimer(this);

                SaveSkillsToCharacter();
                DBCharacter.PlayedTime = PlayedTime;  //We have to set the PlayedTime on the character before setting the LastPlayed
                DBCharacter.PlayedTimeSinceLevel = PlayedTimeSinceLevel;
                DBCharacter.LastLevelUp = DateTime.Now;
                DBCharacter.LastPlayed = DateTime.Now;
                DBCharacter.ActiveWeaponSlot = (byte)((byte)ActiveWeaponSlot | (byte)rangeAttackComponent.ActiveQuiverSlot);

                styleComponent.OnPlayerSaveIntoDatabase();
                GameServer.Database.SaveObject(DBCharacter);
                Inventory.SaveIntoDatabase(InternalID);

                DbCoreCharacter cachedCharacter = null;

                foreach (DbCoreCharacter accountChar in Client.Account.Characters)
                {
                    if (accountChar.ObjectId == InternalID)
                    {
                        cachedCharacter = accountChar;
                        break;
                    }
                }

                if (cachedCharacter != null)
                    cachedCharacter = DBCharacter;

                foreach (AbstractQuest quest in QuestList.Keys)
                {
                    if (quest is Quests.DailyQuest dq)
                        dq.SaveQuestParameters();

                    if (quest is Quests.WeeklyQuest wq)
                        wq.SaveQuestParameters();

                    if (quest is Quests.MonthlyQuest mq)
                        mq.SaveQuestParameters();
                }

                if (m_mlSteps != null)
                    GameServer.Database.SaveObject(m_mlSteps.OfType<DbCharacterXMasterLevel>());

                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.SaveIntoDatabase.CharacterSaved"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error saving player {0}! - {1}", Name, e);
            }
        }

        #endregion

        #region CustomDialog

        /// <summary>
        /// Holds the delegates that calls
        /// </summary>
        public CustomDialogResponse m_customDialogCallback;

        /// <summary>
        /// Gets/sets the custom dialog callback
        /// </summary>
        public CustomDialogResponse CustomDialogCallback
        {
            get { return m_customDialogCallback; }
            set { m_customDialogCallback = value; }
        }

        #endregion

        #region GetPronoun/GetExamineMessages

        /// <summary>
        /// Pronoun of this player in case you need to refer it in 3rd person
        /// http://webster.commnet.edu/grammar/cases.htm
        /// </summary>
        /// <param name="firstLetterUppercase"></param>
        /// <param name="form">0=Subjective, 1=Possessive, 2=Objective</param>
        /// <returns>pronoun of this object</returns>
        public override string GetPronoun(int form, bool firstLetterUppercase)
        {
            if (Gender == eGender.Male) // male
                switch (form)
                {
                    default:
                        // Subjective
                        if (firstLetterUppercase)
                            return "He";
                        else
                            return "he";
                    case 1:
                        // Possessive
                        if (firstLetterUppercase)
                            return "His";
                        else
                            return "his";
                    case 2:
                        // Objective
                        if (firstLetterUppercase)
                            return "Him";
                        else
                            return "him";
                }
            else
                // female
                switch (form)
                {
                    default:
                        // Subjective
                        if (firstLetterUppercase)
                            return "She";
                        else
                            return "she";
                    case 1:
                        // Possessive
                        if (firstLetterUppercase)
                            return "Her";
                        else
                            return "her";
                    case 2:
                        // Objective
                        if (firstLetterUppercase)
                            return "Her";
                        else
                            return "her";
                }
        }

        public string GetPronoun(GameClient Client, int form, bool capitalize)
        {
            if (Gender == eGender.Male)
                switch (form)
                {
                    default:
                        return Capitalize(capitalize, LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Pronoun.Male.Subjective"));
                    case 1:
                        return Capitalize(capitalize, LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Pronoun.Male.Possessive"));
                    case 2:
                        return Capitalize(capitalize, LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Pronoun.Male.Objective"));
                }
            else
                switch (form)
                {
                    default:
                        return Capitalize(capitalize, LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Pronoun.Female.Subjective"));
                    case 1:
                        return Capitalize(capitalize, LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Pronoun.Female.Possessive"));
                    case 2:
                        return Capitalize(capitalize, LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Pronoun.Female.Objective"));
                }
        }

        public string GetName(GamePlayer target)
        {
            return GameServer.ServerRules.GetPlayerName(this, target);
        }

        /// <summary>
        /// Adds messages to ArrayList which are sent when object is targeted
        /// </summary>
        /// <param name="player">GamePlayer that is examining this object</param>
        /// <returns>list with string messages</returns>
        public override IList GetExamineMessages(GamePlayer player)
        {
            // TODO: PvP & PvE messages
            IList list = base.GetExamineMessages(player);

            string message = string.Empty;
            switch (GameServer.Instance.Configuration.ServerType)
            {//FIXME: Better extract this to a new function in ServerRules !!! (VaNaTiC)
                case EGameServerType.GST_Normal:
                {
                    if (Realm == player.Realm || Client.Account.PrivLevel > 1 || player.Client.Account.PrivLevel > 1)
                        message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.GetExamineMessages.RealmMember", player.GetName(this), GetPronoun(Client, 0, true), CharacterClass.Name);
                    else
                        message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.GetExamineMessages.EnemyRealmMember", player.GetName(this), GetPronoun(Client, 0, true));
                    break;
                }

                case EGameServerType.GST_PvP:
                {
                    if (Client.Account.PrivLevel > 1 || player.Client.Account.PrivLevel > 1)
                        message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.GetExamineMessages.YourGuildMember", player.GetName(this), GetPronoun(Client, 0, true), CharacterClass.Name);
                    else if (Guild == null)
                        message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.GetExamineMessages.NeutralMember", player.GetName(this), GetPronoun(Client, 0, true));
                    else if (Guild == player.Guild || Client.Account.PrivLevel > 1 || player.Client.Account.PrivLevel > 1)
                        message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.GetExamineMessages.YourGuildMember", player.GetName(this), GetPronoun(Client, 0, true), CharacterClass.Name);
                    else
                        message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.GetExamineMessages.OtherGuildMember", player.GetName(this), GetPronoun(Client, 0, true), GuildName);
                    break;
                }

                default:
                {
                    message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.GetExamineMessages.YouExamine", player.GetName(this));
                    break;
                }
            }

            list.Add(message);
            return list;
        }

        #endregion

        #region Stealth / Wireframe

        bool m_isWireframe = false;

        /// <summary>
        /// Player is drawn as a Wireframe.  Not sure why or when this is used.  -- Tolakram
        /// </summary>
        public bool IsWireframe
        {
            get { return m_isWireframe; }
            set
            {
                bool needUpdate = m_isWireframe != value;
                m_isWireframe = value;
                if (needUpdate && ObjectState == eObjectState.Active)
                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        if (player == null) continue;
                        player.Out.SendPlayerModelTypeChange(this, (byte)(value ? 1 : 0));
                    }
            }
        }

        private bool m_isTorchLighted = false;

        /// <summary>
        /// Is player Torch lighted ?
        /// </summary>
        public bool IsTorchLighted 
        {
            get { return m_isTorchLighted; }
            set { m_isTorchLighted = value; }
        }

        /// <summary>
        /// Property that holds tick when stealth state was changed last time
        /// </summary>
        public const string STEALTH_CHANGE_TICK = "StealthChangeTick";

        /// <summary>
        /// The stealth state of this player
        /// </summary>
        public override bool IsStealthed => effectListComponent.ContainsEffectForEffectType(eEffect.Stealth);

        public override void Stealth(bool goStealth)
        {
            if (IsStealthed == goStealth)
                return;

            if (goStealth)
            {
                if (CraftTimer != null && CraftTimer.IsAlive)
                {
                    Out.SendMessage("You can't stealth while crafting!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (effectListComponent.ContainsEffectForEffectType(eEffect.Pulse))
                {
                    Out.SendMessage("You currently have an active, pulsing spell effect and cannot hide!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (IsOnHorse || IsSummoningMount)
                    IsOnHorse = false;

                ECSGameEffectFactory.Create(new(this, 0, 1), static (in ECSGameEffectInitParams i) => new StealthECSGameEffect(i));
                return;
            }

            ECSGameEffect effect = EffectListService.GetEffectOnTarget(this, eEffect.Stealth);
            effect?.Stop();
        }

        public override void OnMaxSpeedChange()
        {
            base.OnMaxSpeedChange();
            Out.SendUpdateMaxSpeed();
        }

        // UncoverStealthAction is what unstealths player if they are too close to mobs.
        public void StartStealthUncoverAction()
        {
            UncoverStealthAction action = TempProperties.GetProperty<UncoverStealthAction>(UNCOVER_STEALTH_ACTION_PROP);
            //start the uncover timer
            if (action == null)
                action = new UncoverStealthAction(this);
            action.Interval = 1000;
            action.Start(1000);
            TempProperties.SetProperty(UNCOVER_STEALTH_ACTION_PROP, action);
        }

        // UncoverStealthAction is what unstealths player if they are too close to mobs.
        public void StopStealthUncoverAction()
        {
            UncoverStealthAction action = TempProperties.GetProperty<UncoverStealthAction>(UNCOVER_STEALTH_ACTION_PROP);
            //stop the uncover timer
            if (action != null)
            {
                action.Stop();
                TempProperties.RemoveProperty(UNCOVER_STEALTH_ACTION_PROP);
            }
        }

        /// <summary>
        /// The temp property that stores the uncover stealth action
        /// </summary>
        protected const string UNCOVER_STEALTH_ACTION_PROP = "UncoverStealthAction";

        /// <summary>
        /// Uncovers the player if a mob is too close
        /// </summary>
        protected class UncoverStealthAction : ECSGameTimerWrapperBase
        {
            /// <summary>
            /// Constructs a new uncover stealth action
            /// </summary>
            /// <param name="actionSource">The action source</param>
            public UncoverStealthAction(GamePlayer actionSource) : base(actionSource) { }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            protected override int OnTick(ECSGameTimer timer)
            {
                GamePlayer player = (GamePlayer) timer.Owner;

                if (player.Client.Account.PrivLevel > 1)
                    return 0;

                foreach (GameNPC npc in player.GetNPCsInRadius(1024))
                {
                    // Friendly mobs do not uncover stealthed players
                    if (!GameServer.ServerRules.IsAllowedToAttack(npc, player, true))
                        continue;

                    // Npc with player owner don't uncover
                    if (npc.Brain != null
                        && (npc.Brain as IControlledBrain) != null
                        && (npc.Brain as IControlledBrain).GetPlayerOwner() != null)
                        continue;

                    double npcLevel = Math.Max(npc.Level, 1.0);
                    double stealthLevel = player.GetModifiedSpecLevel(Specs.Stealth);
                    double detectRadius = 125.0 + ((npcLevel - stealthLevel) * 20.0);

                    // we have detect hidden and enemy don't = higher range
                    if (npc.HasAbility(Abilities.DetectHidden) &&
                        !player.HasAbility(Abilities.DetectHidden) &&
                        !player.effectListComponent.ContainsEffectForEffectType(eEffect.Camouflage) &&
                        !player.effectListComponent.ContainsEffectForEffectType(eEffect.Vanish))
                    {
                        detectRadius += 125;
                    }

                    if (detectRadius < 126) detectRadius = 126;

                    double distanceToPlayer = npc.GetDistanceTo(player);

                    if (distanceToPlayer > detectRadius)
                        continue;

                    double fieldOfView = 90.0;  //90 degrees  = standard FOV
                    double fieldOfListen = 120.0; //120 degrees = standard field of listening

                    if (npc.Level > 50)
                        fieldOfListen += (npc.Level - player.Level) * 3;

                    double angle = npc.GetAngle(player);

                    //player in front
                    fieldOfView /= 2.0;
                    bool canSeePlayer = (angle >= 360 - fieldOfView || angle < fieldOfView);

                    //If npc can not see nor hear the player, continue the loop
                    fieldOfListen /= 2.0;
                    if (canSeePlayer == false &&
                        !(angle >= (45 + 60) - fieldOfListen && angle < (45 + 60) + fieldOfListen) &&
                        !(angle >= (360 - 45 - 60) - fieldOfListen && angle < (360 - 45 - 60) + fieldOfListen))
                        continue;

                    double chanceMod = 1.0;

                    //Chance to detect player decreases after 125 coordinates!
                    if (distanceToPlayer > 125)
                        chanceMod = 1f - (distanceToPlayer - 125.0) / (detectRadius - 125.0);

                    double chanceToUncover = 0.1 + (npc.Level - stealthLevel) * 0.01 * chanceMod;

                    if (Util.Chance(chanceToUncover))
                    {
                        if (canSeePlayer)
                            player.Out.SendCheckLos(player, npc, new CheckLosResponse(player.UncoverLosHandler));
                        else
                            npc.TurnTo(player, 10000);
                    }
                }

                return Interval;
            }
        }
        /// <summary>
        /// This handler is called by the unstealth check of mobs
        /// </summary>
        public void UncoverLosHandler(GamePlayer player, LosCheckResponse response, ushort sourceOID, ushort targetOID)
        {
            GameObject target = CurrentRegion.GetObject(targetOID);

            if (target == null || !player.IsStealthed)
                return;

            if (response is LosCheckResponse.True)
            {
                player.Out.SendMessage(target.GetName(0, true) + " uncovers you!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.Stealth(false);
            }
        }

        public virtual bool CanDetect(GameObject enemy)
        {
            if (!enemy.IsStealthed || Client.Account.PrivLevel > 1)
                return true;

            if (!IsAlive)
                return false;

            switch (enemy.GameObjectType)
            {
                case eGameObjectType.PLAYER:
                {
                    GamePlayer enemyPlayer = enemy as GamePlayer;

                    // Own group is always visible.
                    if (enemyPlayer.Group != null && Group != null && enemyPlayer.Group == Group)
                        return true;

                    // Why is this still using the old effect list and vanish effect?
                    if (enemyPlayer.EffectList.GetOfType<VanishEffect>() != null || enemyPlayer.Client.Account.PrivLevel > 1)
                        return false;

                    // This ignores the hard cap.
                    if (effectListComponent.ContainsEffectForEffectType(eEffect.TrueSight))
                        return true;

                    /*
                     * http://www.critshot.com/forums/showthread.php?threadid=3142
                     * The person doing the looking has a chance to find them based on their level, minus the stealthed person's stealth spec.
                     *
                     * -Normal detection range = (enemy lvl  your stealth spec) * 20 + 125
                     * -Detect Hidden Range = (enemy lvl  your stealth spec) * 50 + 250
                     * -See Hidden range = 2700 - (38 * your stealth spec)
                     */

                    int enemyStealthLevel = Math.Min(50, enemyPlayer.GetModifiedSpecLevel(Specs.Stealth));
                    int levelDiff = Math.Max(0, Level - enemyStealthLevel);
                    int range = 0;

                    // Detect Hidden works only if the enemy doesn't have it or camouflage or vanish.
                    // According to https://disorder.dk/daoc/stealth/, it's possible that the range per level difference is supposed to be 50 even when Detect Hidden is cancelled out.
                    if (HasAbility(Abilities.DetectHidden) &&
                        !enemyPlayer.HasAbility(Abilities.DetectHidden) &&
                        !enemyPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.Camouflage) &&
                        !enemyPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.Vanish))
                    {
                        range = levelDiff * 50 + 250;
                    }
                    else
                        range = levelDiff * 20 + 125;

                    // See Hidden only works against non assassin classes, if they don't have Camouflage enabled.
                    if (HasAbilityType(typeof(AtlasOF_SeeHidden)) &&
                        !enemyPlayer.CharacterClass.IsAssassin &&
                        !enemyPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.Camouflage))
                    {
                        // https://forums.freddyshouse.com/threads/scouts-and-stealth.139740/
                        range = Math.Max(range, 2700 - 36 * enemyStealthLevel);
                    }

                    // Mastery of Stealth
                    // Disabled. This is NF MoS. OF Version does not add range, only movement speed.
                    /*RAPropertyEnhancer mos = GetAbility<MasteryOfStealthAbility>();
                    if (mos != null && !enemyHasCamouflage)
                    {
                        if (!HasAbility(Abilities.DetectHidden) || !enemy.HasAbility(Abilities.DetectHidden))
                            range += mos.GetAmountForLevel(CalculateSkillLevel(mos));
                    }*/

                    range += BaseBuffBonusCategory[eProperty.Skill_Stealth];

                    // //Buff (Stealth Detection)
                    // //Increases the target's ability to detect stealthed players and monsters.
                    // GameSpellEffect iVampiirEffect = SpellHandler.FindEffectOnTarget((GameLiving)this, "VampiirStealthDetection");
                    // if (iVampiirEffect != null)
                    //     range += (int)iVampiirEffect.Spell.Value;
                             //
                    // //Infill Only - Greater Chance to Detect Stealthed Enemies for 1 minute
                    // //after executing a klling blow on a realm opponent.
                    // GameSpellEffect HeightenedAwareness = SpellHandler.FindEffectOnTarget((GameLiving)this, "HeightenedAwareness");
                    // if (HeightenedAwareness != null)
                    //     range += (int)HeightenedAwareness.Spell.Value;
                    //
                    // //Nightshade Only - Greater chance of remaining hidden while stealthed for 1 minute
                    // //after executing a killing blow on a realm opponent.
                    // GameSpellEffect SubtleKills = SpellHandler.FindEffectOnTarget((GameLiving)enemy, "SubtleKills");
                    // if (SubtleKills != null)
                    // {
                    //     range -= (int)SubtleKills.Spell.Value;
                    //     if (range < 0) range = 0;
                    // }
                    //
                    // // Apply Blanket of camouflage effect
                    // GameSpellEffect iSpymasterEffect1 = SpellHandler.FindEffectOnTarget((GameLiving)enemy, "BlanketOfCamouflage");
                    // if (iSpymasterEffect1 != null)
                    // {
                    //     range -= (int)iSpymasterEffect1.Spell.Value;
                    //     if (range < 0) range = 0;
                    // }
                    //
                    // // Apply Lookout effect
                    // GameSpellEffect iSpymasterEffect2 = SpellHandler.FindEffectOnTarget((GameLiving)this, "Loockout");
                    // if (iSpymasterEffect2 != null)
                    //     range += (int)iSpymasterEffect2.Spell.Value;
                    //
                    // // Apply Prescience node effect
                    // GameSpellEffect iConvokerEffect = SpellHandler.FindEffectOnTarget((GameLiving)enemy, "Prescience");
                    // if (iConvokerEffect != null)
                    //     range += (int)iConvokerEffect.Spell.Value;

                    // Hard cap.
                    if (range > 1900)
                        range = 1900;

                    return IsWithinRadius(enemy, range);
                }
                case eGameObjectType.NPC:
                {
                    // Custom feature to make stealthed NPCs actually invisible.
                    // Currently disabled.
                    return true;
                    int detectionRange = Math.Clamp(1500 + (Level - enemy.Level) * 50, 500, 3000);
                    return IsWithinRadius(enemy, detectionRange);
                }
                default:
                    return true;
            }
        }

        #endregion

        #region Task

        /// <summary>
        /// Holding tasks of player
        /// </summary>
        AbstractTask m_task = null;

        /// <summary>
        /// Gets the tasklist of this player
        /// </summary>
        public AbstractTask GameTask
        {
            get { return m_task; }
            set { m_task = value; }
        }

        #endregion

        #region Mission

        private AbstractMission m_mission = null;

        /// <summary>
        /// Gets the personal mission
        /// </summary>
        public AbstractMission Mission
        {
            get { return m_mission; }
            set
            {
                m_mission = value;
                this.Out.SendQuestListUpdate();
                if (value != null) Out.SendMessage(m_mission.Description, eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        #endregion

        #region Quest

        /// <summary>
        /// Get the player ID used for quests.  Usually InternalID, provided for customization
        /// </summary>
        public virtual string QuestPlayerID
        {
            get { return InternalID; }
        }

        private List<AbstractQuest> _questListFinished = new();
        private readonly Lock _questListFinishedLock = new();
        public virtual ConcurrentDictionary<AbstractQuest, byte> QuestList { get; } = new(); // Value is the index to send to clients.
        public ConcurrentQueue<byte> AvailableQuestIndexes { get; } = new(); // If empty, 'QuestList.Count' will be used when adding a quest to 'QuestList'
        public ECSGameTimer QuestActionTimer;

        public List<AbstractQuest> GetFinishedQuests()
        {
            lock (_questListFinishedLock)
            {
                return _questListFinished.ToList();
            }
        }

        /// <summary>
        /// Checks if a player has done a specific quest type
        /// This is used for scripted quests
        /// </summary>
        /// <param name="questType">The quest type</param>
        /// <returns>the number of times the player did this quest</returns>
        public int HasFinishedQuest(Type questType)
        {
            int counter = 0;

            lock (_questListFinishedLock)
            {
                foreach (AbstractQuest quest in _questListFinished)
                {
                    if (quest is not DataQuest)
                    {
                        if (quest.GetType().Equals(questType))
                            counter++;
                    }
                }
            }

            return counter;
        }

        public bool HasFinishedQuest(AbstractQuest quest)
        {
            lock (_questListFinishedLock)
            {
                return _questListFinished.Contains(quest);
            }
        }

        /// <summary>
        /// Add a quest to the players finished list
        /// </summary>
        /// <param name="quest"></param>
        public void AddFinishedQuest(AbstractQuest quest)
        {
            lock (_questListFinishedLock)
            {
                _questListFinished.Add(quest);
            }
        }

        public void RemoveFinishedQuest(AbstractQuest quest)
        {
            lock (_questListFinishedLock)
            {
                _questListFinished.Remove(quest);
            }
        }

        public void RemoveFinishedQuests(Predicate<AbstractQuest> match)
        {
            lock (_questListFinishedLock)
            {
                for (int i = _questListFinished.Count - 1; i >= 0; i--)
                {
                    AbstractQuest quest = _questListFinished[i];

                    if (match(quest))
                    {
                        _questListFinished.SwapRemoveAt(i);
                        quest.DeleteFromDatabase();
                    }
                }
            }
        }

        /// <summary>
        /// Remove credit for this type of encounter.
        /// Used for scripted quests
        /// </summary>
        /// <param name="questType"></param>
        /// <returns></returns>
        public bool RemoveEncounterCredit(Type questType)
        {
            if (questType == null)
                return false;

            lock (_questListFinishedLock)
            {
                for (int i = _questListFinished.Count - 1; i >= 0; i--)
                {
                    AbstractQuest quest = _questListFinished[i];

                    if (quest is not DataQuest && quest.GetType().Equals(questType) && quest.Step == -1)
                    {
                        _questListFinished.SwapRemoveAt(i);
                        quest.DeleteFromDatabase();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Adds a quest to the players questlist
        /// Can be used by both scripted quests and data quests
        /// </summary>
        /// <param name="quest">The quest to add</param>
        /// <returns>true if added, false if player is already doing the quest!</returns>
        public bool AddQuest(AbstractQuest quest)
        {
            if (QuestList.Count > 25)

            if (IsDoingQuest(quest) != null)
                return false;

            if (!AvailableQuestIndexes.TryDequeue(out byte index))
                index = (byte) QuestList.Count;

            QuestList.TryAdd(quest, index);
            quest.OnQuestAssigned(this);
            Out.SendQuestUpdate(quest);
            return true;
        }

        /// <summary>
        /// Checks if this player is currently doing the specified quest
        /// Can be used by scripted and data quests
        /// </summary>
        /// <returns>the quest if player is doing the quest or null if not</returns>
        public AbstractQuest IsDoingQuest(AbstractQuest quest)
        {
            foreach (AbstractQuest questInList in QuestList.Keys)
            {
                if (questInList.GetType().Equals(quest.GetType()) && questInList.IsDoingQuest())
                    return questInList;
            }

            return null;
        }

        /// <summary>
        /// Checks if this player is currently doing the specified quest type
        /// This is used for scripted quests
        /// </summary>
        /// <param name="questType">The quest type</param>
        /// <returns>the quest if player is doing the quest or null if not</returns>
        public AbstractQuest IsDoingQuest(Type questType)
        {
            foreach (AbstractQuest quest in QuestList.Keys)
            {
                if (quest is not DataQuest)
                {
                    if (quest.GetType().Equals(questType))
                        return quest;
                }
            }

            return null;
        }

        #endregion

        #region Notify
        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            CharacterClass.Notify(e, sender, args);
            base.Notify(e, sender, args);

            foreach (AbstractQuest quest in QuestList.Keys)
            {
                // player forwards every single notify message to all active quests
                quest.Notify(e, sender, args);
            }

            if (GameTask != null)
                GameTask.Notify(e, sender, args);

            if (Mission != null)
                Mission.Notify(e, sender, args);

            if (Group != null && Group.Mission != null)
                Group.Mission.Notify(e, sender, args);

            //Realm mission will be handled on the capture event args
        }

        public override void Notify(DOLEvent e, object sender)
        {
            Notify(e, sender, null);
        }

        public override void Notify(DOLEvent e)
        {
            Notify(e, null, null);
        }

        public override void Notify(DOLEvent e, EventArgs args)
        {
            Notify(e, null, args);
        }
        #endregion

        #region Crafting

        public readonly Lock _craftingLock = new();

        /// <summary>
        /// Store all player crafting skill and their value (eCraftingSkill => Value)
        /// </summary>
        protected Dictionary<eCraftingSkill, int> m_craftingSkills = new Dictionary<eCraftingSkill, int>();

        /// <summary>
        /// Store the player primary crafting skill
        /// </summary>
        protected eCraftingSkill m_craftingPrimarySkill = 0;

        /// <summary>
        /// Get all player crafting skill and their value
        /// </summary>
        public Dictionary<eCraftingSkill, int> CraftingSkills
        {
            get { return m_craftingSkills; }
        }

        /// <summary>
        /// Store the player primary crafting skill
        /// </summary>
        public eCraftingSkill CraftingPrimarySkill
        {
            get { return m_craftingPrimarySkill; }
            set { m_craftingPrimarySkill = value; }
        }

        /// <summary>
        /// Get the specified player crafting skill value
        /// </summary>
        /// <param name="skill">The crafting skill to get value</param>
        /// <returns>the level in the specified crafting if valid and -1 if not</returns>
        public virtual int GetCraftingSkillValue(eCraftingSkill skill)
        {
            lock (_craftingLock)
            {
                return m_craftingSkills.TryGetValue(skill, out int value) ? value : -1;
            }
        }

        /// <summary>
        /// Increase the specified player crafting skill
        /// </summary>
        /// <param name="skill">Crafting skill to increase</param>
        /// <param name="count">How much increase or decrase</param>
        /// <returns>true if the skill is valid and -1 if not</returns>
        public virtual bool GainCraftingSkill(eCraftingSkill skill, int count)
        {
            if (skill == eCraftingSkill.NoCrafting) return false;

            lock (_craftingLock)
            {
                AbstractCraftingSkill craftingSkill = CraftingMgr.getSkillbyEnum(skill);
                if (craftingSkill != null && count >0)
                {
                    m_craftingSkills[skill] = count + m_craftingSkills[skill];
                    CraftingProgressMgr.TrackChange(this, m_craftingSkills);
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainCraftingSkill.GainSkill", craftingSkill.Name, m_craftingSkills[skill]), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    int currentSkillLevel = GetCraftingSkillValue(skill);
                    if (HasPlayerReachedNewCraftingTitle(currentSkillLevel))
                    {
                        GameEventMgr.Notify(GamePlayerEvent.NextCraftingTierReached, this,new NextCraftingTierReachedEventArgs(skill,currentSkillLevel) );
                    }
                    if (GameServer.ServerRules.CanGenerateNews(this) && currentSkillLevel >= 1000 && currentSkillLevel - count < 1000)
                    {
                        string message = string.Format(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.GainCraftingSkill.ReachedSkill", Name, craftingSkill.Name));
                        NewsMgr.CreateNews(message, Realm, eNewsType.PvE, true);
                    }
                }
                return true;
            }
        }


        protected bool m_isEligibleToGiveMeritPoints = true;

        /// <summary>
        /// Can actions done by this player reward merit points to the players guild?
        /// </summary>
        public virtual bool IsEligibleToGiveMeritPoints
        {
            get
            {
                if (Guild == null || Client.Account.PrivLevel > 1 || m_isEligibleToGiveMeritPoints == false)
                    return false;

                return true;
            }

            set { m_isEligibleToGiveMeritPoints = value; }
        }

        /// <summary>
        /// Get the crafting speed multiplier for this player
        /// This might be modified by region or equipment
        /// </summary>
        public virtual double CraftingSpeed
        {
            get
            {
                double speed = Properties.CRAFTING_SPEED;
                var craftSpeedBonus = false;
                ushort[] keepIDs = {50, 75, 100, 57, 111, 198}; // beno bled crauch and the strength relic keeps in each realm

                ushort[] strRelicKeepIDs = {};


                if (speed <= 0)
                    speed = 1.0;

                if (Guild != null && Guild.BonusType == Guild.eBonusType.CraftingHaste)
                {
                    speed *= (1.0 + Properties.GUILD_BUFF_CRAFTING * .01);
                }

                if (CurrentRegion.IsCapitalCity && Properties.CAPITAL_CITY_CRAFTING_SPEED_BONUS > 0)
                {
                    return speed * Properties.CAPITAL_CITY_CRAFTING_SPEED_BONUS;
                } 
                else if (CurrentZone.IsOF && m_currentAreas.Count > 0)
                {
                    foreach (var area in m_currentAreas)
                    {
                        if (area is KeepArea kA)
                        {
                            if (keepIDs.Contains(kA.Keep.KeepID) && kA.Keep.Realm == Realm)
                            {
                                craftSpeedBonus = true;
                                break;
                            }
                        }
                    }
                    if (craftSpeedBonus)
                    {
                        speed = speed * Properties.KEEP_CRAFTING_SPEED_BONUS;
                    }
                }
                //log.Warn($"Crafting speed bonus {craftSpeedBonus} for {Name} in {CurrentZone.Description} - crafting speed {Math.Round(speed*100)}%");

                return speed;
            }
        }

        /// <summary>
        /// Get the crafting skill bonus for this player.
        /// This might be modified by region or equipment
        /// Values represents a percent; 0 - 100
        /// </summary>
        public virtual int CraftingSkillBonus
        {
            get
            {
                if (CurrentRegion.IsCapitalCity)
                {
                    return Properties.CAPITAL_CITY_CRAFTING_SKILL_GAIN_BONUS;
                }

                return 0;
            }
        }

        protected virtual bool HasPlayerReachedNewCraftingTitle(int skillLevel)
        {
            // no titles after 1000 any more, checked in 1.97
            if (skillLevel <= 1000)
            {
                if (skillLevel % 100 == 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add a new crafting skill to the player
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="startValue"></param>
        /// <returns></returns>
        public virtual bool AddCraftingSkill(eCraftingSkill skill, int startValue)
        {
            if (skill == eCraftingSkill.NoCrafting) return false;

            if (CraftingPrimarySkill == eCraftingSkill.NoCrafting)
                CraftingPrimarySkill = eCraftingSkill.BasicCrafting;

            lock (_craftingLock)
            {
                if (m_craftingSkills.ContainsKey(skill))
                {
                    AbstractCraftingSkill craftingSkill = CraftingMgr.getSkillbyEnum(skill);
                    if (craftingSkill != null)
                    {
                        m_craftingSkills.Add(skill, startValue);
                        CraftingProgressMgr.TrackChange(this, m_craftingSkills);
                        Out.SendMessage("You gain skill in " + craftingSkill.Name + "! (" + startValue + ").", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// This is the timer used to count time when a player craft
        /// </summary>
        private ECSGameTimer m_crafttimer;

        /// <summary>
        /// Get and set the craft timer
        /// </summary>
        public ECSGameTimer CraftTimer
        {
            get { return m_crafttimer; }
            set { m_crafttimer = value; }
        }

        private CraftAction m_craftaction;
        public CraftAction CraftAction
        {
            get { return m_craftaction; }
            set { m_craftaction = value; }
        }

        /// <summary>
        /// Does the player is crafting
        /// </summary>
        public bool IsCrafting => (craftComponent != null && craftComponent.CraftState);

        /// <summary>
        /// Checks if a player is salvaging 
        /// </summary>
        public bool IsSalvagingOrRepairing
        {
            get { return (m_crafttimer != null && m_crafttimer.IsAlive); }
        }

        /// <summary>
        /// Get the craft title string of the player
        /// </summary>
        public virtual IPlayerTitle CraftTitle
        {
            get
            {
                var title = m_titles.FirstOrDefault(ttl => ttl is CraftTitle);
                if (title != null && title.IsSuitable(this))
                    return title;

                return PlayerTitleMgr.ClearTitle;
            }
        }

        /// <summary>
        /// This function is called each time a player tries to make a item
        /// </summary>
        public virtual void CraftItem(ushort itemID)
        {
            var recipe = RecipeDB.FindBy(itemID);

            if (recipe == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("[CRAFTING] Item " + itemID + " not found in recipe database!");
                return;
            }

            AbstractCraftingSkill skill = CraftingMgr.getSkillbyEnum(recipe.RequiredCraftingSkill);
            if (skill != null)
            {
                skill.CraftItem(this, recipe);
            }
            else
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CraftItem.DontHaveAbilityMake"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        /// <summary>
        /// This function is called each time a player try to salvage a item
        /// </summary>
        public virtual void SalvageItem(DbInventoryItem item)
        {
            Salvage.BeginWork(this, item);
        }

        public virtual void SalvageItemList(IList<DbInventoryItem> itemList)
        {
            Salvage.BeginWorkList(this, itemList);
        }

        /// <summary>
        /// This function is called each time a player try to repair a item
        /// </summary>
        public virtual void RepairItem(DbInventoryItem item)
        {
            Repair.BeginWork(this, item);
        }

        #endregion

        #region Housing

        /// <summary>
        /// Jumps the player out of the house he is in
        /// </summary>
        public void LeaveHouse()
        {
            if (CurrentHouse == null || (!InHouse))
            {
                InHouse = false;
                return;
            }

            House house = CurrentHouse;
            InHouse = false;
            CurrentHouse = null;

            house.Exit(this, false);
        }


        #endregion

        #region Trade

        /// <summary>
        /// Holds the trade window object
        /// </summary>
        protected ITradeWindow m_tradeWindow;

        /// <summary>
        /// Gets or sets the player trade windows
        /// </summary>
        public ITradeWindow TradeWindow
        {
            get { return m_tradeWindow; }
            set { m_tradeWindow = value; }
        }

        /// <summary>
        /// Opens the trade between two players
        /// </summary>
        /// <param name="tradePartner">GamePlayer to trade with</param>
        /// <returns>true if trade has started</returns>
        public bool OpenTrade(GamePlayer tradePartner)
        {
            lock (_tradeLock)
            {
                lock (tradePartner)
                {
                    if (tradePartner.TradeWindow != null)
                        return false;

                    Lock sync = new();
                    PlayerTradeWindow initiantWindow = new PlayerTradeWindow(tradePartner, false, sync);
                    PlayerTradeWindow recipientWindow = new PlayerTradeWindow(this, true, sync);

                    tradePartner.TradeWindow = initiantWindow;
                    TradeWindow = recipientWindow;

                    initiantWindow.PartnerWindow = recipientWindow;
                    recipientWindow.PartnerWindow = initiantWindow;

                    return true;
                }
            }
        }

        /// <summary>
        /// Opens the trade window for myself (combine, repair)
        /// </summary>
        /// <param name="item">The item to spell craft</param>
        /// <returns>true if trade has started</returns>
        public bool OpenSelfCraft(DbInventoryItem item)
        {
            if (item == null) return false;

            lock (_tradeLock)
            {
                if (TradeWindow != null)
                {
                    GamePlayer sourceTradePartner = TradeWindow.Partner;
                    if (sourceTradePartner == null)
                    {
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OpenSelfCraft.AlreadySelfCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        Out.SendMessage("You are still trading with " + sourceTradePartner.Name + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    return false;
                }

                if (item.SlotPosition < (int)eInventorySlot.FirstBackpack || item.SlotPosition > (int)eInventorySlot.LastBackpack)
                {
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.OpenSelfCraft.CanOnlyCraftBackpack"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                TradeWindow = new SelfCraftWindow(this, item);
                TradeWindow.TradeUpdate();

                return true;
            }
        }

        #endregion

        #region ControlledNpc

        public override bool AddControlledBrain(IControlledBrain controlledBrain)
        {
            return SetControlledBrain(controlledBrain);
        }

        public override bool RemoveControlledBrain(IControlledBrain controlledBrain)
        {
            if (ControlledBrain == controlledBrain)
                return SetControlledBrain(null);

            return false;
        }

        private bool SetControlledBrain(IControlledBrain controlledBrain)
        {
            try
            {
                CharacterClass.SetControlledBrain(controlledBrain);
                return true;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);

                return false;
            }
        }

        /// <summary>
        /// Releases controlled object
        /// (delegates to CharacterClass)
        /// </summary>
        public virtual void CommandNpcRelease()
        {
            CharacterClass.CommandNpcRelease();
        }

        /// <summary>
        /// Commands controlled object to attack
        /// </summary>
        public virtual void CommandNpcAttack()
        {
            IControlledBrain npc = ControlledBrain;

            if (npc == null || !GameServer.ServerRules.IsAllowedToAttack(this, TargetObject as GameLiving, false))
                return;

            if (!IsWithinRadius(TargetObject, 2000))
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcAttack.TooFarAwayForPet"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // The attack command did not require the target to be in view in 1.65. This was apparently changed in 1.70z.

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcAttack.KillTarget", npc.Body.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            npc.Attack(TargetObject);
        }

        /// <summary>
        /// Commands controlled object to follow
        /// </summary>
        public virtual void CommandNpcFollow()
        {
            IControlledBrain npc = ControlledBrain;

            if (npc == null)
                return;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcAttack.FollowYou", npc.Body.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            npc.CheckAggressionStateOnPlayerOrder();
            npc.Disengage();
            npc.Follow(this);
        }

        /// <summary>
        /// Commands controlled object to stay where it is
        /// </summary>
        public virtual void CommandNpcStay()
        {
            IControlledBrain npc = ControlledBrain;

            if (npc == null)
                return;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcAttack.Stay", npc.Body.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            npc.CheckAggressionStateOnPlayerOrder();
            npc.Disengage();
            npc.Stay();
        }

        /// <summary>
        /// Commands controlled object to go to players location
        /// </summary>
        public virtual void CommandNpcComeHere()
        {
            IControlledBrain npc = ControlledBrain;

            if (npc == null)
                return;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcAttack.ComeHere", npc.Body.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            npc.CheckAggressionStateOnPlayerOrder();
            npc.Disengage();
            npc.ComeHere();
        }

        /// <summary>
        /// Commands controlled object to go to target
        /// </summary>
        public virtual void CommandNpcGoTarget()
        {
            IControlledBrain npc = ControlledBrain;

            if (npc == null)
                return;

            GameObject target = TargetObject;

            if (target == null)
            {
                Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcGoTarget.MustSelectDestination"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (GetDistance(new Point2D(target.X, target.Y)) > 1250)
            {
                Out.SendMessage("Your target is too far away for your pet to reach!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcAttack.GoToTarget", npc.Body.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            npc.CheckAggressionStateOnPlayerOrder();
            npc.Disengage();
            npc.Goto(target);
        }

        /// <summary>
        /// Changes controlled object state to passive
        /// </summary>
        public virtual void CommandNpcPassive()
        {
            IControlledBrain npc = ControlledBrain;

            if (npc == null)
                return;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcAttack.Passive", npc.Body.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            npc.SetAggressionState(eAggressionState.Passive);
        }

        /// <summary>
        /// Changes controlled object state to aggressive
        /// </summary>
        public virtual void CommandNpcAgressive()
        {
            IControlledBrain npc = ControlledBrain;

            if (npc == null)
                return;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcAttack.Aggressive", npc.Body.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            npc.SetAggressionState(eAggressionState.Aggressive);
        }

        /// <summary>
        /// Changes controlled object state to defensive
        /// </summary>
        public virtual void CommandNpcDefensive()
        {
            IControlledBrain npc = ControlledBrain;

            if (npc == null)
                return;

            Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CommandNpcAttack.Defensive", npc.Body.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            npc.SetAggressionState(eAggressionState.Defensive);
        }
        #endregion

        #region Shade

        /// <summary>
        /// Gets flag indication whether player is in shade mode
        /// </summary>
        public bool HasShadeModel => Model == ShadeModel;

        /// <summary>
        /// The model ID used on character creation.
        /// </summary>
        public ushort CreationModel
        {
            get
            {
                return (ushort)m_client.Account.Characters[m_client.ActiveCharIndex].CreationModel;
            }
        }

        /// <summary>
        /// The model ID used for shade morphs.
        /// </summary>
        public ushort ShadeModel
        {
            get
            {
                // Aredhel: Bit fishy, necro in caster from could use
                // Traitor's Dagger... FIXME!

                if (CharacterClass.ID == (int)eCharacterClass.Necromancer)
                    return 822;

                switch (Race)
                {
                    // Albion Models.
                    case (int)eRace.Inconnu: return (ushort)(DBCharacter.Gender + 1351);
                    case (int)eRace.Briton: return (ushort)(DBCharacter.Gender + 1353);
                    case (int)eRace.Avalonian: return (ushort)(DBCharacter.Gender + 1359);
                    case (int)eRace.Highlander: return (ushort)(DBCharacter.Gender + 1355);
                    case (int)eRace.Saracen: return (ushort)(DBCharacter.Gender + 1357);
                    case (int)eRace.HalfOgre: return (ushort)(DBCharacter.Gender + 1361);

                    // Midgard Models.
                    case (int)eRace.Troll: return (ushort)(DBCharacter.Gender + 1363);
                    case (int)eRace.Dwarf: return (ushort)(DBCharacter.Gender + 1369);
                    case (int)eRace.Norseman: return (ushort)(DBCharacter.Gender + 1365);
                    case (int)eRace.Kobold: return (ushort)(DBCharacter.Gender + 1367);
                    case (int)eRace.Valkyn: return (ushort)(DBCharacter.Gender + 1371);
                    case (int)eRace.Frostalf: return (ushort)(DBCharacter.Gender + 1373);

                    // Hibernia Models.
                    case (int)eRace.Firbolg: return (ushort)(DBCharacter.Gender + 1375);
                    case (int)eRace.Celt: return (ushort)(DBCharacter.Gender + 1377);
                    case (int)eRace.Lurikeen: return (ushort)(DBCharacter.Gender + 1379);
                    case (int)eRace.Elf: return (ushort)(DBCharacter.Gender + 1381);
                    case (int)eRace.Sylvan: return (ushort)(DBCharacter.Gender + 1383);
                    case (int)eRace.Shar: return (ushort)(DBCharacter.Gender + 1385);

                    default: return Model;
                }
            }
        }

        /// <summary>
        /// Changes shade state of the player.
        /// </summary>
        /// <param name="state">The new state.</param>
        public virtual void Shade(bool state)
        {
            CharacterClass.Shade(state, out _);
        }
        #endregion

        #region Siege Weapon
        private GameSiegeWeapon m_siegeWeapon;

        public GameSiegeWeapon SiegeWeapon
        {
            get { return m_siegeWeapon; }
            set { m_siegeWeapon = value; }
        }
        public void SalvageSiegeWeapon(GameSiegeWeapon siegeWeapon)
        {
            if (siegeWeapon.Realm != this.Realm)
            {
                this.Out.SendMessage("You cannot salvage another realm's siege weapon!", eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                return;
            }
            Salvage.BeginWork(this, siegeWeapon);
        }
        #endregion

        #region Invulnerability

        /// <summary>
        /// The delegate for invulnerability expire callbacks
        /// </summary>
        public delegate void InvulnerabilityExpiredCallback(GamePlayer player);
        /// <summary>
        /// Holds the invulnerability timer
        /// </summary>
        protected InvulnerabilityTimer m_invulnerabilityTimer;
        /// <summary>
        /// Holds the invulnerability expiration tick
        /// </summary>
        protected long m_invulnerabilityTick;

        /// <summary>
        /// Starts the Invulnerability Timer
        /// </summary>
        /// <param name="duration">The invulnerability duration in milliseconds</param>
        /// <param name="callback">
        /// The callback for when invulnerability expires;
        /// not guaranteed to be called if overwriten by another invulnerability
        /// </param>
        /// <returns>true if invulnerability was set (smaller than old invulnerability)</returns>
        public virtual bool StartInvulnerabilityTimer(int duration, InvulnerabilityExpiredCallback callback)
        {
            if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_PvE)
                return false;

            if (duration < 1)
            {
                if (log.IsWarnEnabled)
                    log.Warn("GamePlayer.StartInvulnerabilityTimer(): Immunity duration cannot be less than 1ms");

                return false;
            }

            long newTick = CurrentRegion.Time + duration;

            if (newTick < m_invulnerabilityTick)
                return false;

            m_invulnerabilityTick = newTick;

            if (m_invulnerabilityTimer != null)
                m_invulnerabilityTimer.Stop();

            if (callback != null)
            {
                m_invulnerabilityTimer = new InvulnerabilityTimer(this, callback);
                m_invulnerabilityTimer.Start(duration);
            }
            else
                m_invulnerabilityTimer = null;

            return true;
        }

        /// <summary>
        /// True if player is invulnerable to any attack
        /// </summary>
        public virtual bool IsInvulnerableToAttack
        {
            get { return m_invulnerabilityTick > CurrentRegion.Time; }
        }

        /// <summary>
        /// The timer to call invulnerability expired callbacks
        /// </summary>
        protected class InvulnerabilityTimer : ECSGameTimerWrapperBase
        {
            /// <summary>
            /// Defines a logger for this class.
            /// </summary>
            private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

            /// <summary>
            /// Holds the callback
            /// </summary>
            private readonly InvulnerabilityExpiredCallback m_callback;

            /// <summary>
            /// Constructs a new InvulnerabilityTimer
            /// </summary>
            /// <param name="actionSource"></param>
            /// <param name="callback"></param>
            public InvulnerabilityTimer(GamePlayer actionSource, InvulnerabilityExpiredCallback callback) : base(actionSource)
            {
                if (callback == null)
                    throw new ArgumentNullException("callback");
                m_callback = callback;
            }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            protected override int OnTick(ECSGameTimer timer)
            {
                try
                {
                    m_callback((GamePlayer) timer.Owner);
                }
                catch (Exception e)
                {
                    log.Error("InvulnerabilityTimer callback", e);
                }

                return 0;
            }
        }

        #endregion

        #region Player Titles

        /// <summary>
        /// Holds all players titles.
        /// </summary>
        protected readonly ReaderWriterHashSet<IPlayerTitle> m_titles = new ReaderWriterHashSet<IPlayerTitle>();

        /// <summary>
        /// Holds current selected title.
        /// </summary>
        protected IPlayerTitle m_currentTitle = PlayerTitleMgr.ClearTitle;

        /// <summary>
        /// Gets all player's titles.
        /// </summary>
        public virtual ISet<IPlayerTitle> Titles
        {
            get { return m_titles; }
        }

        /// <summary>
        /// Gets/sets currently selected/active player title.
        /// </summary>
        public virtual IPlayerTitle CurrentTitle
        {
            get { return m_currentTitle; }
            set
            {
                if (value == null)
                    value = PlayerTitleMgr.ClearTitle;
                m_currentTitle = value;
                if (DBCharacter != null) DBCharacter.CurrentTitleType = value.GetType().FullName;

                //update newTitle for all players if client is playing
                if (ObjectState == eObjectState.Active)
                {
                    if (value == PlayerTitleMgr.ClearTitle)
                        Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CurrentTitle.TitleCleared"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    else
                        Out.SendMessage("Your title has been set to " + value.GetDescription(this) + '.', eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                UpdateCurrentTitle();
            }
        }

        /// <summary>
        /// Adds the title to player.
        /// </summary>
        /// <param name="title">The title to add.</param>
        /// <returns>true if added.</returns>
        public virtual bool AddTitle(IPlayerTitle title)
        {
            if (m_titles.Contains(title))
                return false;
            m_titles.Add(title);
            title.OnTitleGained(this);
            return true;
        }

        /// <summary>
        /// Removes the title from player.
        /// </summary>
        /// <param name="title">The title to remove.</param>
        /// <returns>true if removed.</returns>
        public virtual bool RemoveTitle(IPlayerTitle title)
        {
            if (!m_titles.Contains(title))
                return false;
            if (CurrentTitle == title)
                CurrentTitle = PlayerTitleMgr.ClearTitle;
            m_titles.Remove(title);
            title.OnTitleLost(this);
            return true;
        }

        /// <summary>
        /// Updates player's current title to him and everyone around.
        /// </summary>
        public virtual void UpdateCurrentTitle()
        {
            if (ObjectState == eObjectState.Active)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player == null) continue;
                    if (player != this)
                    {
                        //						player.Out.SendRemoveObject(this);
                        //						player.Out.SendPlayerCreate(this);
                        //						player.Out.SendLivingEquipementUpdate(this);
                        player.Out.SendPlayerTitleUpdate(this);
                    }
                }
                Out.SendUpdatePlayer();
            }
        }

        #endregion

        #region Statistics

        public virtual int KillsAlbionPlayers
        {
            get => DBCharacter != null ? DBCharacter.KillsAlbionPlayers : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsAlbionPlayers = value;

                Notify(GamePlayerEvent.KillsAlbionPlayersChanged, this);
                Notify(GamePlayerEvent.KillsTotalPlayersChanged, this);
            }
        }

        public virtual int KillsMidgardPlayers
        {
            get => DBCharacter != null ? DBCharacter.KillsMidgardPlayers : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsMidgardPlayers = value;

                Notify(GamePlayerEvent.KillsMidgardPlayersChanged, this);
                Notify(GamePlayerEvent.KillsTotalPlayersChanged, this);
            }
        }

        public virtual int KillsHiberniaPlayers
        {
            get => DBCharacter != null ? DBCharacter.KillsHiberniaPlayers : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsHiberniaPlayers = value;

                Notify(GamePlayerEvent.KillsHiberniaPlayersChanged, this);
                Notify(GamePlayerEvent.KillsTotalPlayersChanged, this);
            }
        }

        public virtual int KillsAlbionDeathBlows
        {
            get => DBCharacter != null ? DBCharacter.KillsAlbionDeathBlows : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsAlbionDeathBlows = value;

                Notify(GamePlayerEvent.KillsTotalDeathBlowsChanged, this);
            }
        }

        public virtual int KillsMidgardDeathBlows
        {
            get => DBCharacter != null ? DBCharacter.KillsMidgardDeathBlows : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsMidgardDeathBlows = value;

                Notify(GamePlayerEvent.KillsTotalDeathBlowsChanged, this);
            }
        }

        public virtual int KillsHiberniaDeathBlows
        {
            get => DBCharacter != null ? DBCharacter.KillsHiberniaDeathBlows : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsHiberniaDeathBlows = value;
                Notify(GamePlayerEvent.KillsTotalDeathBlowsChanged, this);
            }
        }

        public virtual int KillsAlbionSolo
        {
            get => DBCharacter != null ? DBCharacter.KillsAlbionSolo : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsAlbionSolo = value;

                Notify(GamePlayerEvent.KillsTotalSoloChanged, this);
            }
        }

        public virtual int KillsMidgardSolo
        {
            get => DBCharacter != null ? DBCharacter.KillsMidgardSolo : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsMidgardSolo = value;

                Notify(GamePlayerEvent.KillsTotalSoloChanged, this);
            }
        }

        public virtual int KillsHiberniaSolo
        {
            get => DBCharacter != null ? DBCharacter.KillsHiberniaSolo : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsHiberniaSolo = value;

                Notify(GamePlayerEvent.KillsTotalSoloChanged, this);
            }
        }

        public virtual int CapturedKeeps
        {
            get => DBCharacter != null ? DBCharacter.CapturedKeeps : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.CapturedKeeps = value;

                Notify(GamePlayerEvent.CapturedKeepsChanged, this);
            }
        }

        public virtual int CapturedTowers
        {
            get => DBCharacter != null ? DBCharacter.CapturedTowers : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.CapturedTowers = value;

                Notify(GamePlayerEvent.CapturedTowersChanged, this);
            }
        }

        public virtual int CapturedRelics
        {
            get => DBCharacter != null ? DBCharacter.CapturedRelics : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.CapturedRelics = value;

                Notify(GamePlayerEvent.CapturedRelicsChanged, this);
            }
        }

        public virtual int KillsDragon
        {
            get => DBCharacter != null ? DBCharacter.KillsDragon : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsDragon = value;

                Notify(GamePlayerEvent.KillsDragonChanged, this);
            }
        }

        public virtual int DeathsPvP
        {
            get => DBCharacter != null ? DBCharacter.DeathsPvP : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.DeathsPvP = value;
            }
        }

        public virtual int KillsLegion
        {
            get => DBCharacter != null ? DBCharacter.KillsLegion : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsLegion = value;

                Notify(GamePlayerEvent.KillsLegionChanged, this);
            }
        }

        public virtual int KillsEpicBoss
        {
            get => DBCharacter != null ? DBCharacter.KillsEpicBoss : 0;
            set
            {
                if (DBCharacter != null)
                    DBCharacter.KillsEpicBoss = value;

                Notify(GamePlayerEvent.KillsEpicBossChanged, this);
            }
        }

        private readonly Lock _killStatsOnPlayerKillLock = new();

        public void UpdateKillStatsOnPlayerKill(eRealm killedPlayerRealm, bool deathBlow, bool soloKill, int realmPointsEarned)
        {
            lock (_killStatsOnPlayerKillLock)
            {
                if (realmPointsEarned > 0)
                    m_statistics.AddToRealmPointsEarnedFromKills((uint) realmPointsEarned);

                switch (killedPlayerRealm)
                {
                    case eRealm.Albion:
                    {
                        KillsAlbionPlayers++;

                        if (deathBlow)
                        {
                            KillsAlbionDeathBlows++;
                            m_statistics.AddToDeathblows();
                        }

                        if (soloKill)
                            KillsAlbionSolo++;

                        break;
                    }
                    case eRealm.Hibernia:
                    {
                        KillsHiberniaPlayers++;

                        if (deathBlow)
                        {
                            KillsHiberniaDeathBlows++;
                            m_statistics.AddToDeathblows();
                        }

                        if (soloKill)
                            KillsHiberniaSolo++;

                        break;
                    }
                    case eRealm.Midgard:
                    {
                        KillsMidgardPlayers++;

                        if (deathBlow)
                        {
                            KillsMidgardDeathBlows++;
                            m_statistics.AddToDeathblows();
                        }

                        if (soloKill)
                            KillsMidgardSolo++;

                        break;
                    }
                }
            }
        }

        #endregion

        #region Controlled Mount

        protected ECSGameTimer m_whistleMountTimer;
        protected ControlledHorse m_controlledHorse;

        public bool HasHorse
        {
            get
            {
                if (ActiveHorse == null || ActiveHorse.ID == 0)
                    return false;
                return true;
            }
        }

        public bool IsSummoningMount
        {
            get { return m_whistleMountTimer != null && m_whistleMountTimer.IsAlive; }
        }

        protected bool m_isOnHorse;
        public virtual bool IsOnHorse
        {
            get { return m_isOnHorse; }
            set
            {
                if (m_whistleMountTimer != null)
                    StopWhistleTimers();
                m_isOnHorse = value;
                Out.SendControlledHorse(this, value); // fix very rare bug when this player not in GetPlayersInRadius;
                foreach (GamePlayer plr in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (plr == null) continue;
                    if (plr == this)
                        continue;
                    plr.Out.SendControlledHorse(this, value);
                }
                if (m_isOnHorse)
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.IsOnHorse.MountSteed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                else
                    Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.IsOnHorse.DismountSteed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                Out.SendUpdateMaxSpeed();
            }
        }

        protected void StopWhistleTimers()
        {
            if (m_whistleMountTimer != null)
            {
                m_whistleMountTimer.Stop();
                Out.SendCloseTimerWindow();
            }
            m_whistleMountTimer = null;
        }

        protected int WhistleMountTimerCallback(ECSGameTimer callingTimer)
        {
            StopWhistleTimers();
            IsOnHorse = true;
            return 0;
        }

        /// <summary>
        /// What horse saddle bags are active on this player?
        /// </summary>
        public virtual byte ActiveSaddleBags
        {
            get { return DBCharacter != null ? DBCharacter.ActiveSaddleBags : (byte)0; }
            set { if (DBCharacter != null) DBCharacter.ActiveSaddleBags = value; }
        }

        public ControlledHorse ActiveHorse
        {
            get { return m_controlledHorse; }
        }

        public class ControlledHorse
        {
            protected byte m_id;
            protected byte m_bardingId;
            protected ushort m_bardingColor;
            protected byte m_saddleId;
            protected byte m_saddleColor;
            protected byte m_slots;
            protected byte m_armor;
            protected string m_name;
            protected int m_level;
            protected GamePlayer m_player;

            public ControlledHorse(GamePlayer player)
            {
                m_name = string.Empty;
                m_player = player;
            }

            public byte ID
            {
                get { return m_id; }
                set
                {
                    m_id = value;
                    DbInventoryItem item = m_player.Inventory.GetItem(eInventorySlot.Horse);
                    if (item != null)
                        m_level = item.Level;
                    else
                        m_level = 35;//base horse by default
                    m_player.Out.SendSetControlledHorse(m_player);
                }
            }

            public byte Barding
            {
                get
                {
                    DbInventoryItem barding = m_player.Inventory.GetItem(eInventorySlot.HorseBarding);
                    if (barding != null)
                        return (byte)barding.DPS_AF;
                    return m_bardingId;
                }
                set
                {
                    m_bardingId = value;
                    m_player.Out.SendSetControlledHorse(m_player);
                }
            }

            public ushort BardingColor
            {
                get
                {
                    DbInventoryItem barding = m_player.Inventory.GetItem(eInventorySlot.HorseBarding);
                    if (barding != null)
                        return (ushort)barding.Color;
                    return m_bardingColor;
                }
                set
                {
                    m_bardingColor = value;
                    m_player.Out.SendSetControlledHorse(m_player);
                }
            }

            public byte Saddle
            {
                get
                {
                    DbInventoryItem armor = m_player.Inventory.GetItem(eInventorySlot.HorseArmor);
                    if (armor != null)
                        return (byte)armor.DPS_AF;
                    return m_saddleId;
                }
                set
                {
                    m_saddleId = value;
                    m_player.Out.SendSetControlledHorse(m_player);
                }
            }

            public byte SaddleColor
            {
                get
                {
                    DbInventoryItem armor = m_player.Inventory.GetItem(eInventorySlot.HorseArmor);
                    if (armor != null)
                        return (byte)armor.Color;
                    return m_saddleColor;
                }
                set
                {
                    m_saddleColor = value;
                    m_player.Out.SendSetControlledHorse(m_player);
                }
            }

            public byte Slots
            {
                get { return m_player.ActiveSaddleBags; }
            }

            public byte Armor
            {
                get { return m_armor; }
            }

            public string Name
            {
                get { return m_name; }
                set
                {
                    m_name = value;
                    DbInventoryItem item = m_player.Inventory.GetItem(eInventorySlot.Horse);
                    if (item != null)
                        item.Creator = Name;
                    m_player.Out.SendSetControlledHorse(m_player);
                }
            }

            public short Speed
            {
                get
                {
                    if (m_level <= 35)
                        return ServerProperties.Properties.MOUNT_UNDER_LEVEL_35_SPEED;
                    else
                        return ServerProperties.Properties.MOUNT_OVER_LEVEL_35_SPEED;
                }
            }

            public bool IsSummonRvR
            {
                get
                {
                    if (m_level <= 35)
                        return false;
                    else
                        return true;
                }
            }

            public bool IsCombatHorse
            {
                get
                {
                    return false;
                }
            }
        }
        #endregion

        #region GuildBanner
        protected GuildBanner m_guildBanner = null;

        /// <summary>
        /// Gets/Sets the visibility of the carryable RvrGuildBanner. Wont work if the player has no guild.
        /// </summary>
        public GuildBanner GuildBanner
        {
            get { return m_guildBanner; }
            set
            {
                //cant send guildbanner for players without guild.
                if (Guild == null)
                    return;

                m_guildBanner = value;

                if (value != null)
                {
                    foreach (GamePlayer playerToUpdate in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        if (playerToUpdate == null) continue;

                        if (playerToUpdate != null && playerToUpdate.Client.IsPlaying)
                        {
                            playerToUpdate.Out.SendRvRGuildBanner(this, true);
                        }
                    }
                }
                else
                {
                    foreach (GamePlayer playerToUpdate in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        if (playerToUpdate == null) continue;

                        if (playerToUpdate != null && playerToUpdate.Client.IsPlaying)
                        {
                            playerToUpdate.Out.SendRvRGuildBanner(this, false);
                        }
                    }
                }
            }
        }

        #endregion

        #region Champion Levels
        /// <summary>
        /// The maximum champion level a player can reach
        /// </summary>
        public const int CL_MAX_LEVEL = 10;

        /// <summary>
        /// A table that holds the required XP/Level
        /// </summary>
        public static readonly long[] CLXPLevel =
        {
            0, //xp tp level 0
            32000, //xp to level 1
            64000, // xp to level 2
            96000, // xp to level 3
            128000, // xp to level 4
            160000, // xp to level 5
            192000, // xp to level 6
            224000, // xp to level 7
            256000, // xp to level 8
            288000, // xp to level 9
            320000, // xp to level 10
            640000, // xp to level 11
            640000, // xp to level 12
            640000, // xp to level 13
            640000, // xp to level 14
            640000, // xp to level 15
        };

        /// <summary>
        /// Get the CL title string of the player
        /// </summary>
        public virtual IPlayerTitle CLTitle
        {
            get
            {
                var title = m_titles.FirstOrDefault(ttl => ttl is ChampionlevelTitle);

                if (title != null && title.IsSuitable(this))
                    return title;

                return PlayerTitleMgr.ClearTitle;
            }
        }

        /// <summary>
        /// Is Champion level activated
        /// </summary>
        public virtual bool Champion
        {
            get { return DBCharacter != null ? DBCharacter.Champion : false; }
            set { if (DBCharacter != null) DBCharacter.Champion = value; }
        }

        /// <summary>
        /// Champion level
        /// </summary>
        public virtual int ChampionLevel
        {
            get { return DBCharacter != null ? DBCharacter.ChampionLevel : 0; }
            set { if (DBCharacter != null) DBCharacter.ChampionLevel = value; }
        }

        /// <summary>
        /// Max champion level for the player
        /// </summary>
        public virtual int ChampionMaxLevel
        {
            get { return CL_MAX_LEVEL; }
        }

        /// <summary>
        /// Champion Experience
        /// </summary>
        public virtual long ChampionExperience
        {
            get { return DBCharacter != null ? DBCharacter.ChampionExperience : 0; }
            set { if (DBCharacter != null) DBCharacter.ChampionExperience = value; }
        }

        /// <summary>
        /// Champion Available speciality points
        /// </summary>
        public virtual int ChampionSpecialtyPoints
        {
            get { return ChampionLevel - GetSpecList().Where(sp => sp is LiveChampionsLineSpec).Sum(sp => sp.Level); }
        }

        /// <summary>
        /// Returns how far into the champion level we have progressed
        /// A value between 0 and 1000 (1 bubble = 100)
        /// </summary>
        public virtual ushort ChampionLevelPermill
        {
            get
            {
                //No progress if we haven't even reached current level!
                if (ChampionExperience <= ChampionExperienceForCurrentLevel)
                    return 0;
                //No progess after maximum level
                if (ChampionLevel > CL_MAX_LEVEL) // needed to get exp after 10
                    return 0;
                if ((ChampionExperienceForNextLevel - ChampionExperienceForCurrentLevel) > 0)
                    return (ushort)(1000 * (ChampionExperience - ChampionExperienceForCurrentLevel) / (ChampionExperienceForNextLevel - ChampionExperienceForCurrentLevel));
                else return 0;

            }
        }

        /// <summary>
        /// Returns the xp that are needed for the next level
        /// </summary>
        public virtual long ChampionExperienceForNextLevel
        {
            get { return GetChampionExperienceForLevel(ChampionLevel + 1); }
        }

        /// <summary>
        /// Returns the xp that were needed for the current level
        /// </summary>
        public virtual long ChampionExperienceForCurrentLevel
        {
            get { return GetChampionExperienceForLevel(ChampionLevel); }
        }

        /// <summary>
        /// Gets/Sets amount of champion skills respecs
        /// (delegate to PlayerCharacter)
        /// </summary>
        public virtual int RespecAmountChampionSkill
        {
            get { return DBCharacter != null ? DBCharacter.RespecAmountChampionSkill : 0; }
            set { if (DBCharacter != null) DBCharacter.RespecAmountChampionSkill = value; }
        }

        /// <summary>
        /// Returns the xp that are needed for the specified level
        /// </summary>
        public virtual long GetChampionExperienceForLevel(int level)
        {
            if (level > CL_MAX_LEVEL)
                return CLXPLevel[GamePlayer.CL_MAX_LEVEL]; // exp for level 51, needed to get exp after 50
            if (level <= 0)
                return CLXPLevel[0];
            return CLXPLevel[level];
        }

        public void GainChampionExperience(long experience)
        {
            GainChampionExperience(experience, eXPSource.Other);
        }

        /// <summary>
        /// The process that gains exp
        /// </summary>
        /// <param name="experience">Amount of Experience</param>
        public virtual void GainChampionExperience(long experience, eXPSource source)
        {
            if (ChampionExperience >= 320000)
            {
                ChampionExperience = 320000;
                return;
            }

            // Do not gain experience:
            // - if champion not activated
            // - if champion max level reached
            // - if experience is negative
            // - if praying at your grave
            if (!Champion || ChampionLevel == CL_MAX_LEVEL || experience <= 0 || IsPraying)
                return;

            if (source != eXPSource.GM && source != eXPSource.Quest)
            {
                double modifier = ServerProperties.Properties.CL_XP_RATE;

                // 1 CLXP point per 333K normal XP
                if (this.CurrentRegion.IsRvR)
                {
                    experience = (long)((double)experience * modifier / 333000);
                }
                else // 1 CLXP point per 2 Million normal XP
                {
                    experience = (long)((double)experience * modifier / 2000000);
                }
            }

            System.Globalization.NumberFormatInfo format = System.Globalization.NumberFormatInfo.InvariantInfo;
            Out.SendMessage("You get " + experience.ToString("N0", format) + " champion experience points.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            ChampionExperience += experience;
            Out.SendUpdatePoints();
        }

        /// <summary>
        /// Reset all Champion skills for this player
        /// </summary>
        public virtual void RespecChampionSkills()
        {
            foreach (var spec in GetSpecList().Where(sp => sp is LiveChampionsLineSpec))
            {
                RemoveSpecialization(spec.KeyName);
            }

            RefreshSpecDependantSkills(false);
            Out.SendUpdatePlayer();
            Out.SendUpdatePoints();
            Out.SendUpdatePlayerSkills(true);
            UpdatePlayerStatus();
        }

        /// <summary>
        /// Remove all Champion levels and XP from this character.
        /// </summary>
        public virtual void RemoveChampionLevels()
        {
            ChampionExperience = 0;
            ChampionLevel = 0;

            RespecChampionSkills();
        }

        /// <summary>
        /// Holds what happens when your champion level goes up;
        /// </summary>
        public virtual void ChampionLevelUp()
        {
            ChampionLevel++;

            // If this is a pure tank then give them full power when reaching champ level 1
            if (ChampionLevel == 1 && CharacterClass.ClassType == eClassType.PureTank)
            {
                Mana = CalculateMaxMana(Level, 0);
            }

            RefreshSpecDependantSkills(true);
            Out.SendUpdatePlayerSkills(true);

            Notify(GamePlayerEvent.ChampionLevelUp, this);
            Out.SendMessage("You have gained one champion level!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            Out.SendUpdatePlayer();
            Out.SendUpdatePoints();
            UpdatePlayerStatus();
        }

        #endregion

        #region Master levels

        /// <summary>
        /// The maximum ML level a player can reach
        /// </summary>
        public const int ML_MAX_LEVEL = 10;

        /// <summary>
        /// Amount of MLXP required for ML validation. MLXP reset at every ML.
        /// </summary>
        private static readonly long[] MLXPLevel =
        {
            0, //xp tp level 0
            32000, //xp to level 1
            32000, // xp to level 2
            32000, // xp to level 3
            32000, // xp to level 4
            32000, // xp to level 5
            32000, // xp to level 6
            32000, // xp to level 7
            32000, // xp to level 8
            32000, // xp to level 9
            32000, // xp to level 10
        };

        /// <summary>
        /// Get the amount of XP needed for a ML
        /// </summary>
        /// <param name="ml"></param>
        /// <returns></returns>
        public virtual long GetXPForML(byte ml)
        {
            if (ml > MLXPLevel.Length - 1)
                return 0;

            return MLXPLevel[ml];
        }

        /// <summary>
        /// How many steps for each Master Level
        /// </summary>
        private static readonly byte[] MLStepsForLevel =
        {
            10, // ML0 == ML1
            10, // ML1
            11,
            11,
            11,
            11,
            11,
            11,
            11,
            11,
            5, // ML10
        };

        /// <summary>
        /// Get the number of steps required for a ML
        /// </summary>
        /// <param name="ml"></param>
        /// <returns></returns>
        public virtual byte GetStepCountForML(byte ml)
        {
            if (ml > MLStepsForLevel.Length - 1)
                return 0;

            return MLStepsForLevel[ml];
        }

        /// <summary>
        /// True if player has started Master Levels
        /// </summary>
        public virtual bool MLGranted
        {
            get { return DBCharacter != null ? DBCharacter.MLGranted : false; }
            set { if (DBCharacter != null) DBCharacter.MLGranted = value; }
        }

        /// <summary>
        /// What ML line has this character chosen
        /// </summary>
        public virtual byte MLLine
        {
            get { return DBCharacter != null ? DBCharacter.ML : (byte)0; }
            set { if (DBCharacter != null) DBCharacter.ML = value; }
        }

        /// <summary>
        /// Gets and sets the last ML the player has completed.
        /// MLLevel is advanced once all steps are completed.
        /// </summary>
        public virtual int MLLevel
        {
            get { return DBCharacter != null ? DBCharacter.MLLevel : 0; }
            set { if (DBCharacter != null) DBCharacter.MLLevel = value; }
        }

        /// <summary>
        /// Gets and sets ML Experience for the current ML level
        /// </summary>
        public virtual long MLExperience
        {
            get { return DBCharacter != null ? DBCharacter.MLExperience : 0; }
            set { if (DBCharacter != null) DBCharacter.MLExperience = value; }
        }

        /// <summary>
        /// Get the number of steps completed for a ML
        /// </summary>
        /// <param name="ml"></param>
        /// <returns></returns>
        public virtual byte GetCountMLStepsCompleted(byte ml)
        {
            byte count = 0;
            int steps = GetStepCountForML(ml);

            for (byte i = 1; i <= steps; i++)
            {
                if (HasFinishedMLStep(ml, i))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Check ML step completition.
        /// Arbiter checks this to see if player is eligible to advance to the next Master Level.
        /// </summary>
        public virtual bool HasFinishedMLStep(int mlLevel, int step)
        {
            // No steps registered so false
            if (m_mlSteps == null) return false;

            // Current ML Level >= required ML, so true
            if (MLLevel >= mlLevel) return true;

            // Check current registered steps
            foreach (DbCharacterXMasterLevel mlStep in m_mlSteps)
            {
                // Found so return value
                if (mlStep.MLLevel == mlLevel && mlStep.MLStep == step)
                    return mlStep.StepCompleted;
            }

            // Not found so false
            return false;
        }

        /// <summary>
        /// Sets an ML step to finished or clears it
        /// </summary>
        /// <param name="mlLevel"></param>
        /// <param name="step"></param>
        /// <param name="setFinished">(optional) false will remove the finished entry for this step</param>
        public virtual void SetFinishedMLStep(int mlLevel, int step, bool setFinished = true)
        {
            // Check current registered steps in case of previous GM rollback command
            if (m_mlSteps != null)
            {
                foreach (DbCharacterXMasterLevel mlStep in m_mlSteps)
                {
                    if (mlStep.MLLevel == mlLevel && mlStep.MLStep == step)
                    {
                        mlStep.StepCompleted = setFinished;
                        return;
                    }
                }
            }

            if (setFinished)
            {
                // Register new step
                DbCharacterXMasterLevel newStep = new DbCharacterXMasterLevel();
                newStep.Character_ID = QuestPlayerID;
                newStep.MLLevel = mlLevel;
                newStep.MLStep = step;
                newStep.StepCompleted = true;
                newStep.ValidationDate = DateTime.Now;
                m_mlSteps.Add(newStep);

                // Add it in DB
                try
                {
                    GameServer.Database.AddObject(newStep);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Error adding player " + Name + " ml step!", e);
                }

                // Refresh Window
                Out.SendMasterLevelWindow((byte)mlLevel);
            }
        }

        /// <summary>
        /// Returns the xp that are needed for the specified level
        /// </summary>
        public virtual long GetMLExperienceForLevel(int level)
        {
            if (level >= ML_MAX_LEVEL)
                return MLXPLevel[GamePlayer.ML_MAX_LEVEL - 1]; // exp for level 9, needed to get exp after 9
            if (level <= 0)
                return MLXPLevel[0];
            return MLXPLevel[level];
        }

        /// <summary>
        /// Get the Masterlevel window text for a ML and Step
        /// </summary>
        /// <param name="ml"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public virtual string GetMLStepDescription(byte ml, int step)
        {
            string description = " ";

            if (HasFinishedMLStep(ml, step))
                description = LanguageMgr.GetTranslation(Client.Account.Language, String.Format("SendMasterLevelWindow.Complete.ML{0}.Step{1}", ml, step));
            else
                description = LanguageMgr.GetTranslation(Client.Account.Language, String.Format("SendMasterLevelWindow.Uncomplete.ML{0}.Step{1}", ml, step));

            return description;
        }

        /// <summary>
        /// Get the ML title string of the player
        /// </summary>
        public virtual IPlayerTitle MLTitle
        {
            get
            {
                var title = m_titles.FirstOrDefault(ttl => ttl is MasterlevelTitle);

                if (title != null && title.IsSuitable(this))
                    return title;

                return PlayerTitleMgr.ClearTitle;
            }
        }

        #endregion

        #region Minotaur Relics
        protected MinotaurRelic m_minoRelic = null;

        /// <summary>
        /// sets or sets the Minotaur Relic of this Player
        /// </summary>
        public MinotaurRelic MinotaurRelic
        {
            get { return m_minoRelic; }
            set { m_minoRelic = value; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Returns the string representation of the GamePlayer
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new StringBuilder(base.ToString())
                .Append(" class=").Append(CharacterClass.Name)
                .Append('(').Append(CharacterClass.ID.ToString()).Append(')')
                .ToString();
        }

        public static GamePlayer CreateTestableGamePlayer() { return CreateTestableGamePlayer(new DefaultCharacterClass()); }

        public static GamePlayer CreateTestableGamePlayer(ICharacterClass charClass) { return new GamePlayer(charClass); }

        private GamePlayer(ICharacterClass charClass) : base()
        {
            m_characterClass = charClass;
        }

        /// <summary>
        /// Creates a new player
        /// </summary>
        /// <param name="client">The GameClient for this player</param>
        /// <param name="dbChar">The character for this player</param>
        public GamePlayer(GameClient client, DbCoreCharacter dbChar) : base()
        {
            movementComponent ??= base.movementComponent as PlayerMovementComponent;
            styleComponent ??= base.styleComponent as PlayerStyleComponent;

            m_steed = new WeakRef(null);
            m_client = client;
            m_dbCharacter = dbChar;
            m_controlledHorse = new ControlledHorse(this);

            CreateInventory();
            AccountVault = new(this, 0, AccountVaultKeeper.GetDummyVaultItem(this));
            LosCheckHandler = new(this);

            m_characterClass = new DefaultCharacterClass();
            m_groupIndex = 0xFF;
            m_saveInDB = true;

            CreateStatistics();

            m_combatTimer = new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(_ =>
            {
                Out.SendUpdateMaxSpeed();
                return 0;
            }));

            m_holdBreathTimer = new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(_ =>
            {
                UpdateWaterBreathState(eWaterBreath.Drowning);
                return 0;
            }));

            m_drowningTimer = new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DrowningTimerCallback));
            InitializeRandomDecks();
        }

        /// <summary>
        /// Create this players inventory
        /// </summary>
        protected virtual void CreateInventory()
        {
            m_inventory = new GamePlayerInventory(this);
        }
        #endregion

        #region Delving
        /// <summary>
        /// Player is delving an item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="delveInfo"></param>
        /// <returns>false if delve not handled</returns>
        public virtual bool DelveItem<T>(T item, List<string> delveInfo)
        {
            if (item is IGameInventoryItem)
            {
                (item as IGameInventoryItem).Delve(delveInfo, this);
            }
            else if (item is DbInventoryItem)
            {
                GameInventoryItem tempItem = GameInventoryItem.Create(item as DbInventoryItem);
                tempItem.Delve(delveInfo, this);
            }
            else if (item is DbItemTemplate)
            {
                GameInventoryItem tempItem = GameInventoryItem.Create(item as DbItemTemplate);
                tempItem.Delve(delveInfo, this);
            }
            else
            {
                delveInfo.Add("Error, unable to delve this item!");
                log.ErrorFormat("Error delving item of ClassType {0}", item.GetType().FullName);
            }

            return true;
        }

        /// <summary>
        /// Player is delving a spell
        /// </summary>
        /// <param name="output"></param>
        /// <param name="spell"></param>
        /// <param name="spellLine"></param>
        /// <returns>false if not handled here, use default delve</returns>
        public virtual bool DelveSpell(IList<string> output, Spell spell, SpellLine spellLine)
        {
            return false;
        }


        ///// <summary>
        ///// Delve a weapon style for this player
        ///// </summary>
        ///// <param name="delveInfo"></param>
        ///// <param name="style"></param>
        ///// <returns></returns>
        //public virtual void DelveWeaponStyle(IList<string> delveInfo, Style style)
        //{
        //	StyleProcessor.DelveWeaponStyle(delveInfo, style, this);
        //}

        /// <summary>
        /// Get a list of bonuses that effect this player
        /// </summary>
        public virtual ICollection<string> GetBonuses()
        {
            return this.GetBonusesInfo();
        }
        #endregion

        #region Bodyguard
        /// <summary>
        /// True, if the player has been standing still for at least 3 seconds,
        /// else false.
        /// </summary>
        private bool IsStandingStill
        {
            get
            {
                if (IsMoving)
                    return false;

                long lastMovementTick = TempProperties.GetProperty<long>("PLAYERPOSITION_LASTMOVEMENTTICK");
                return (CurrentRegion.Time - lastMovementTick > 3000);
            }
        }

        /// <summary>
        /// This player's bodyguard (ML ability) or null, if there is none.
        /// </summary>
        public GamePlayer Bodyguard
        {
            get
            {
                var bodyguardEffects = EffectList.GetAllOfType<BodyguardEffect>();

                BodyguardEffect bodyguardEffect = bodyguardEffects.FirstOrDefault();
                if (bodyguardEffect == null || bodyguardEffect.GuardTarget != this)
                    return null;

                GamePlayer guard = bodyguardEffect.GuardSource;
                GamePlayer guardee = this;

                return (guard.IsAlive && guard.IsWithinRadius(guardee, BodyguardAbilityHandler.BODYGUARD_DISTANCE) &&
                        !guard.IsCasting && guardee.IsStandingStill)
                    ? guard
                    : null;
            }
        }
        #endregion
    }

    public class PlayerObjectCache
    {
        public readonly Dictionary<GameNPC, ClientService.CachedNpcValues> NpcUpdateCache = new();
        public readonly Dictionary<GameStaticItem, ClientService.CachedItemValues> ItemUpdateCache = new();
        public readonly Dictionary<GameDoorBase, long> DoorUpdateCache = new();
        public readonly Dictionary<House, long> HouseUpdateCache = new();
        public readonly HashSet<GameNPC> NpcInRangeCache = new();
        public readonly HashSet<GameStaticItem> ItemInRangeCache = new();
        public readonly HashSet<GameDoorBase> DoorInRangeCache = new();
        public readonly HashSet<House> HouseInRangeCache = new();
        public readonly Lock NpcUpdateCacheLock = new();
        public readonly Lock ItemUpdateCacheLock = new();
        public readonly Lock DoorUpdateCacheLock = new();
        public readonly Lock HouseUpdateCacheLock = new();

        public void Clear()
        {
            NpcUpdateCache.Clear();
            ItemUpdateCache.Clear();
            DoorUpdateCache.Clear();
            HouseUpdateCache.Clear();
            NpcInRangeCache.Clear();
            ItemInRangeCache.Clear();
            DoorUpdateCache.Clear();
            HouseUpdateCache.Clear();
        }
    }
}
