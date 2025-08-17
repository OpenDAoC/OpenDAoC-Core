using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Appeal
{
    public static class AppealMgr
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const string MUTE_PROPERTY = "AppealMute";

        private static bool _initialized = false;

        // Thread-safe cache using account names as keys
        private static readonly ConcurrentDictionary<string, DbAppeal> _appealCache = new();
        private static readonly Lock  _cacheLock = new();
        private static NotifyTimer _notifyTimer;

        // Collections for tracking changes.
        private static readonly HashSet<DbAppeal> _appealsToSave = new();
        private static readonly HashSet<DbAppeal> _appealsToDelete = new();

        public static int Count => _appealCache.Count;

        public static bool Init()
        {
            if (_initialized)
                return false;

            lock (_cacheLock)
            {
                if (_initialized)
                    return false;

                try
                {
                    _appealCache.Clear();
                    IList<DbAppeal> allAppeals = GameServer.Database.SelectAllObjects<DbAppeal>();

                    foreach (DbAppeal appeal in allAppeals)
                        _appealCache.TryAdd(appeal.Account, appeal);

                    _initialized = true;

                    if (log.IsInfoEnabled)
                        log.Info($"AppealMgr initialized with {_appealCache.Count} appeals loaded");
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed to initialize AppealMgr", e);
                }
            }

            _notifyTimer = new NotifyTimer(); // Start the notification timer.
            return true;
        }

        public static void Shutdown()
        {
            lock (_cacheLock)
            {
                _appealCache.Clear();
                _appealsToSave.Clear();
                _appealsToDelete.Clear();
                _initialized = false;
            }
        }

        public static int Save()
        {
            if (!_initialized)
                return 0;

            int count = 0;
            List<DbAppeal> toSave;
            List<DbAppeal> toDelete;

            lock (_cacheLock)
            {
                toSave = _appealsToSave.ToList();
                toDelete = _appealsToDelete.ToList();
                _appealsToSave.Clear();
                _appealsToDelete.Clear();
            }

            // Process saves.
            foreach (DbAppeal appeal in toSave)
            {
                try
                {
                    if (appeal.IsPersisted)
                        GameServer.Database.SaveObject(appeal);
                    else
                        GameServer.Database.AddObject(appeal);

                    count++;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed to save appeal for account {appeal.Account}", e);
                }
            }

            // Process deletions.
            foreach (DbAppeal appeal in toDelete)
            {
                try
                {
                    GameServer.Database.DeleteObject(appeal);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed to delete appeal for account {appeal.Account}", e);
                }
            }

            return count;
        }

        public static DbAppeal GetAppeal(GamePlayer player)
        {
            return GetAppeal(player.Client);
        }

        public static DbAppeal GetAppeal(GameClient client)
        {
            return GetAppeal(client.Account);
        }

        public static DbAppeal GetAppeal(DbAccount account)
        {
            return GetAppealByAccountName(account.Name);
        }

        public static DbAppeal GetAppealByAccountName(string accountName)
        {
            if (!_initialized)
                return null;

            _appealCache.TryGetValue(accountName, out DbAppeal appeal);
            return appeal;
        }

        public static DbAppeal GetAppealByPlayerName(string playerName)
        {
            // Prefer `GetAppealByAccountName`.

            if (!_initialized)
                return null;

            return _appealCache.Values.Where((appeal) => appeal.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public static List<DbAppeal> GetAppeals(bool includeOffline)
        {
            if (!_initialized)
                return new();

            if (includeOffline)
                return _appealCache.Values.ToList();

            List<DbAppeal> result = new();
            List<GamePlayer> onlinePlayers = ClientService.Instance.GetPlayers();

            foreach (GamePlayer player in onlinePlayers)
            {
                DbAppeal appeal = GetAppeal(player.Client.Account);

                if (appeal != null)
                    result.Add(appeal);
            }

            return result;
        }

        public static void CreateAppeal(GamePlayer player, int severity, string status, string text)
        {
            if (!_initialized)
                return;

            if (player.IsMuted)
            {
                player.Out.SendMessage($"[Appeals]: {LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.YouAreMuted")}", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
                return;
            }

            DbAppeal existingAppeal = GetAppeal(player);

            if (existingAppeal != null)
            {
                player.Out.SendMessage($"[Appeals]: {LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.AlreadyActiveAppeal")}", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
                return;
            }

            string timeStamp = $"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}";
            string escapedText = GameServer.Database.Escape(text);
            DbAppeal appeal = new(player.Name, player.Client.Account.Name, severity, status, timeStamp, escapedText);

            lock (_cacheLock)
            {
                _appealCache.TryAdd(player.Client.Account.Name, appeal);
                _appealsToSave.Add(appeal);
            }

            player.Out.SendMessage($"[Appeals]: {LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.AppealSubmitted")}", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
            player.Out.SendMessage($"[Appeals]: {LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.IfYouLogOut")}", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
            player.Out.SendPlaySound(eSoundType.Craft, 0x04);
            _notifyTimer.Start(1);
        }

        public static void ChangeStatus(string staffName, GamePlayer player, DbAppeal appeal, string status)
        {
            if (!_initialized)
                return;

            appeal.Status = status;
            appeal.Dirty = true;

            lock (_cacheLock)
            {
                _appealsToSave.Add(appeal);
            }

            MessageToAllStaff($"Staff member {staffName} has changed the status of {player.Name}'s appeal to {status}.");
            player.Out.SendMessage("[Appeals]: " + LanguageMgr.GetTranslation(player.Client, "Scripts.Players.Appeal.StaffChangedStatus", staffName, status), eChatType.CT_Important, eChatLoc.CL_ChatWindow);
        }

        public static void CloseAppealOnline(string staffName, GamePlayer player, DbAppeal appeal)
        {
            if (!_initialized)
                return;

            lock (_cacheLock)
            {
                _appealCache.TryRemove(player.Client.Account.Name, out _);
                _appealsToDelete.Add(appeal);
                _appealsToSave.Remove(appeal);
            }

            MessageToAllStaff($"[Appeals]: Staff member {staffName} has just closed {player.Name}'s appeal.");
            player.Out.SendMessage($"[Appeals]: {LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.StaffClosedYourAppeal", staffName)}", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
            player.Out.SendPlaySound(eSoundType.Craft, 0x02);
        }

        public static void CloseAppealOffline(string staffName, DbAppeal appeal)
        {
            if (!_initialized)
                return;

            lock (_cacheLock)
            {
                _appealCache.TryRemove(appeal.Account, out _);
                _appealsToDelete.Add(appeal);
                _appealsToSave.Remove(appeal);
            }

            MessageToAllStaff($"[Appeals]: Staff member {staffName} has just closed {appeal.Name}'s (offline) appeal.");
        }

        public static void CancelAppeal(GamePlayer player, DbAppeal appeal)
        {
            if (!_initialized)
                return;

            lock (_cacheLock)
            {
                _appealCache.TryRemove(player.Client.Account.Name, out _);
                _appealsToDelete.Add(appeal);
                _appealsToSave.Remove(appeal); // Remove from save queue if it was there
            }

            MessageToAllStaff($"[Appeals]: {player.Name} has canceled their appeal.");
            player.Out.SendMessage($"[Appeals]: {LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.CanceledYourAppeal")}", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
            player.Out.SendPlaySound(eSoundType.Craft, 0x02);
        }

        public static List<GamePlayer> GetAvailableStaffMembers()
        {
            return ClientService.Instance.GetGmPlayers<object>(static (player, arg) => !player.TempProperties.GetProperty<bool>(MUTE_PROPERTY));
        }

        public static void MessageToAllStaff(string message)
        {
            foreach (GamePlayer staffPlayer in GetAvailableStaffMembers())
                MessageToClient(staffPlayer.Client, message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public static void MessageToClient(GameClient client, string message, eChatType chatType = eChatType.CT_Important, eChatLoc chatLoc = eChatLoc.CL_ChatWindow)
        {
            client.Player.Out.SendMessage($"[Appeals]: {message}", chatType, chatLoc);
        }

        public static void OnPlayerEnter(GamePlayer player)
        {
            if ((ePrivLevel) player.Client.Account.PrivLevel > ePrivLevel.Player && Count > 0)
                player.Out.SendMessage($"[Appeals]: There are {Count} appeals in the queue.", eChatType.CT_Important, eChatLoc.CL_ChatWindow);

            // Check if there is an existing appeal belonging to this player.
            DbAppeal appeal = GetAppealByAccountName(player.Client.Account.Name);

            if (appeal == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.LoginMessage"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // We don't allow players to have more than one appeal per account. Update the name for convenience.
            if (appeal.Name != player.Name)
                appeal.Name = player.Name;

            player.Out.SendMessage($"[Appeals]: {LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.YouHavePendingAppeal")}", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
        }

        private static void NotifyStaffMembers()
        {
            List<DbAppeal> appeals = GetAppeals(false);

            int low = 0;
            int med = 0;
            int high = 0;
            int crit = 0;

            foreach (DbAppeal appeal in appeals)
            {
                switch ((Severity) appeal.Severity)
                {
                    case Severity.Low:
                    {
                        low++;
                        break;
                    }
                    case Severity.Medium:
                    {
                        med++;
                        break;
                    }
                    case Severity.High:
                    {
                        high++;
                        break;
                    }
                    case Severity.Critical:
                    {
                        crit++;
                        break;
                    }
                }
            }

            // Send notifications.
            string countMessage = appeals.Count == 1 
                ? $"There is {appeals.Count} appeal in the queue."
                : $"There are {appeals.Count} appeals in the queue.";
            string detailMessage = $"Crit:{crit}, High:{high}, Med:{med}, Low:{low}. [use /gmappeal]";
            MessageToAllStaff(countMessage);
            MessageToAllStaff(detailMessage);
        }

        public class NotifyTimer : ECSGameTimerWrapperBase
        {
            private const int INTERVAL = 60000; // 10 minutes.

            public NotifyTimer() : base(null)
            {
                Start(INTERVAL);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (!_initialized || Count == 0)
                    return INTERVAL;

                NotifyStaffMembers();
                return INTERVAL;
            }
        }

        public enum Severity
        {
            Low = 1,
            Medium = 2,
            High = 3,
            Critical = 4
        }
    }
}
