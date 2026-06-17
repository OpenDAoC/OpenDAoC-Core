using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class RolloverSchedulerService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public static RolloverSchedulerService Instance { get; }

        private readonly List<IntervalTracker> _trackers = new();

        static RolloverSchedulerService()
        {
            Instance = new();
        }

        private RolloverSchedulerService()
        {
            RegisterInterval(IntervalKey.Hourly, static last => last.Date.AddHours(last.Hour + 1));
            RegisterInterval(IntervalKey.Daily, static last => last.Date.AddDays(1));

            RegisterInterval(IntervalKey.Weekly, static last =>
            {
                int daysToAdd = ((int) DayOfWeek.Monday - (int) last.DayOfWeek + 7) % 7;
                return last.Date.AddDays(daysToAdd == 0 ? 7 : daysToAdd);
            });

            RegisterInterval(IntervalKey.Monthly, static last => new DateTime(last.Year, last.Month, 1).AddMonths(1));

            IList<DbScheduledRollover> dbRollovers = GameServer.Database.SelectAllObjects<DbScheduledRollover>();

            foreach (IntervalTracker tracker in _trackers)
            {
                DbScheduledRollover dbRollover = null;

                foreach (DbScheduledRollover db in dbRollovers)
                {
                    if ((IntervalKey) db.RolloverIntervalKey == tracker.Key)
                    {
                        dbRollover = db;
                        break;
                    }
                }

                if (dbRollover != null)
                {
                    tracker.DbRecord = dbRollover;
                    tracker.LastRollover = dbRollover.LastRollover;
                    tracker.NextRollover = tracker.CalculateNextRollover(dbRollover.LastRollover);
                }
                else
                {
                    // If the DB record doesn't exist, create it immediately.
                    // This ensures that even if the server restarts before the next threshold, the timestamp is safely stored in the database.
                    dbRollover = new()
                    {
                        RolloverIntervalKey = (int) tracker.Key,
                        LastRollover = DateTime.Now
                    };

                    GameServer.Database.AddObject(dbRollover);

                    tracker.DbRecord = dbRollover;
                    tracker.LastRollover = dbRollover.LastRollover;
                    tracker.NextRollover = tracker.CalculateNextRollover(dbRollover.LastRollover);
                }
            }

            SortTrackers();
        }

        private void RegisterInterval(IntervalKey key, Func<DateTime, DateTime> calculateNextRollover)
        {
            _trackers.Add(new()
            {
                Key = key,
                CalculateNextRollover = calculateNextRollover
                // NextRollover initialization is handled safely in the constructor logic.
            });
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();

            DateTime now = DateTime.Now;
            bool trackersUpdated = false;

            // The earliest rollover is always at index 0.
            // Do not parallelize this.
            for (int i = 0; i < _trackers.Count; i++)
            {
                IntervalTracker tracker = _trackers[i];

                if (now < tracker.NextRollover)
                    break;

                ExecuteRollover(tracker, now);
                trackersUpdated = true;
            }

            if (trackersUpdated)
                SortTrackers();
        }

        public void Subscribe(IntervalKey intervalKey, Action callback)
        {
            foreach (IntervalTracker tracker in _trackers)
            {
                if (tracker.Key != intervalKey)
                    continue;

                tracker.Callbacks.Add(callback);
                break;
            }
        }

        public DateTime GetLastRollover(IntervalKey intervalKey)
        {
            foreach (IntervalTracker tracker in _trackers)
            {
                if (tracker.Key != intervalKey)
                    continue;

                return tracker.LastRollover;
            }

            return DateTime.MinValue;
        }

        public void ForceExecute(IntervalKey intervalKey)
        {
            DateTime now = DateTime.Now;
            bool trackersUpdated = false;

            for (int i = 0; i < _trackers.Count; i++)
            {
                IntervalTracker tracker = _trackers[i];

                if (tracker.Key != intervalKey)
                    continue;

                ExecuteRollover(tracker, now);
                trackersUpdated = true;
                break;
            }

            if (trackersUpdated)
                SortTrackers();
        }

        public void ForceExecuteAll()
        {
            DateTime now = DateTime.Now;

            for (int i = 0; i < _trackers.Count; i++)
                ExecuteRollover(_trackers[i], now);

            SortTrackers();
        }

        private static void ExecuteRollover(IntervalTracker tracker, DateTime now)
        {
            tracker.DbRecord.LastRollover = now;
            GameServer.Database.SaveObject(tracker.DbRecord);

            tracker.LastRollover = now;
            tracker.NextRollover = tracker.CalculateNextRollover(now);

            foreach (Action callback in tracker.Callbacks)
            {
                try
                {
                    callback?.Invoke();
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Error executing callback for interval {tracker.Key}", e);
                }
            }
        }

        private void SortTrackers()
        {
            _trackers.Sort(static (a, b) => a.NextRollover.CompareTo(b.NextRollover));
        }

        public enum IntervalKey : int
        {
            // Database keys.
            Hourly = 1,
            Daily = 2,
            Weekly = 3,
            Monthly = 4
        }

        private class IntervalTracker
        {
            public DbScheduledRollover DbRecord { get; set; }
            public IntervalKey Key { get; set; }
            public DateTime LastRollover { get; set; }
            public DateTime NextRollover { get; set; }
            public Func<DateTime, DateTime> CalculateNextRollover { get; set; }
            public List<Action> Callbacks { get; set; } = new();
        }
    }
}
