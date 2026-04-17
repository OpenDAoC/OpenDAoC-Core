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

            foreach (DbScheduledRollover dbRollover in dbRollovers)
            {
                foreach (IntervalTracker tracker in _trackers)
                {
                    if (tracker.Key != (IntervalKey) dbRollover.RolloverIntervalKey)
                        continue;

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
                CalculateNextRollover = calculateNextRollover,
                NextRollover = calculateNextRollover(DateTime.Now)
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
            DbScheduledRollover dbRollover = GameServer.Database.SelectObject<DbScheduledRollover>(DB.Column("RolloverIntervalKey").IsEqualTo((int) tracker.Key));

            if (dbRollover != null)
            {
                dbRollover.LastRollover = now;
                GameServer.Database.SaveObject(dbRollover);
            }
            else
            {
                dbRollover = new()
                {
                    LastRollover = now,
                    RolloverIntervalKey = (int) tracker.Key
                };

                GameServer.Database.AddObject(dbRollover);
            }

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
            public IntervalKey Key { get; set; }
            public DateTime LastRollover { get; set; }
            public DateTime NextRollover { get; set; }
            public Func<DateTime, DateTime> CalculateNextRollover { get; set; }
            public List<Action> Callbacks { get; set; } = new();
        }
    }
}
