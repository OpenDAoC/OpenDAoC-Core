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
        private static readonly Lock _cacheLock = new();
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

            lock (_cacheLock)
            {
                _appealCache.TryGetValue(accountName, out DbAppeal appeal);
                return appeal;
            }
        }

        public static DbAppeal GetAppealByPlayerName(string playerName)
        {
            // Prefer `GetAppealByAccountName`, the cache is keyed by account name.

            if (!_initialized)
                return null;

            lock (_cacheLock)
            {
                return _appealCache.Values.FirstOrDefault(appeal => appeal.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static List<DbAppeal> GetAppeals(bool includeOffline)
        {

            List<DbAppeal> result = new();
            GetAppeals(includeOffline, result);
            return result;
        }

        public static void GetAppeals(bool includeOffline, List<DbAppeal> list)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (!_initialized)
                return;

            list.Clear();

            lock (_cacheLock)
            {
                if (includeOffline)
                    list.AddRange(_appealCache.Values);
                else
                {
                    foreach (GamePlayer player in ClientService.Instance.GetPlayers())
                    {
                        if (_appealCache.TryGetValue(player.Client.Account.Name, out DbAppeal appeal))
                            list.Add(appeal);
                    }
                }
            }
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
                if (_appealCache.TryAdd(player.Client.Account.Name, appeal))
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
                if (_appealCache.TryRemove(player.Client.Account.Name, out _))
                {
                    _appealsToDelete.Add(appeal);
                    _appealsToSave.Remove(appeal);
                }
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
                if (_appealCache.TryRemove(appeal.Account, out _))
                {
                    _appealsToDelete.Add(appeal);
                    _appealsToSave.Remove(appeal);
                }
            }

            MessageToAllStaff($"[Appeals]: Staff member {staffName} has just closed {appeal.Name}'s (offline) appeal.");
        }

        public static void CancelAppeal(GamePlayer player, DbAppeal appeal)
        {
            if (!_initialized)
                return;

            lock (_cacheLock)
            {
                if (_appealCache.TryRemove(player.Client.Account.Name, out _))
                {
                    _appealsToDelete.Add(appeal);
                    _appealsToSave.Remove(appeal);
                }
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

            DbAppeal appeal = GetAppealByAccountName(player.Client.Account.Name);

            if (appeal == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.LoginMessage"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            appeal.CurrentCharacterName = player.Name;
            player.Out.SendMessage($"[Appeals]: {LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.YouHavePendingAppeal")}", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
        }

        private static void NotifyStaffMembers(List<DbAppeal> onlineAppeals)
        {
            int low = 0, med = 0, high = 0, crit = 0;

            foreach (DbAppeal appeal in onlineAppeals)
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

            int offlineCount = Math.Max(0, _appealCache.Count - onlineAppeals.Count);
            string offlineMsg = offlineCount > 0 ? $", {offlineCount} Offline" : string.Empty;
            string detailMsg = $"Appeals Pending: {onlineAppeals.Count} Online (Crit:{crit}, High:{high}, Med:{med}, Low:{low}){offlineMsg} [use /gmappeal]";
            MessageToAllStaff(detailMsg);
        }

        private class NotifyTimer : ECSGameTimerWrapperBase
        {
            private const int INTERVAL = 120000; // 2 minutes.
            private readonly List<DbAppeal> _appeals = new();

            public NotifyTimer() : base(null)
            {
                Start(INTERVAL);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (!_initialized)
                    return INTERVAL;

                GetAppeals(false, _appeals);

                // Only notify if there are appeals from online players.
                if (_appeals.Count > 0)
                    NotifyStaffMembers(_appeals);

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
