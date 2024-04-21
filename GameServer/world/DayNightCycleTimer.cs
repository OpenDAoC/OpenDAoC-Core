using System;
using log4net;
using System.Reflection;

namespace DOL.GS
{
    // Recent clients seem to have faster nights than days (25% faster). This was tested on 1.127. Unsure about older ones. For best results, `DayIncrement` should be a multiple of 4.
    // This timer updates `CurrentTime` every `UPDATE_INTERVAL` and resyncs clients every `CLIENT_RESYNC_INTERVAL`.
    public class DayNightCycleTimer(GameObject owner) : ECSGameTimerWrapperBase(owner)
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const uint DAY = 24 * 60 * 60 * 1000; // This shouldn't be changed.
        private const uint HALF_OF_A_DAY = DAY / 2;
        private const uint QUARTER_OF_A_DAY = DAY / 4;
        private const double NIGHT_INCREMENT_FACTOR = 1.25; // May change depending on client version.
        private const uint CLIENT_RESYNC_INTERVAL = 15 * 60 * 1000;
        private int _updateInterval;
        private uint _nightIncrement;
        private long _dayStartTime;
        private long _nextClientResync;
        private object _lock = new();
        public uint CurrentGameTime { get; private set; }
        public uint DayIncrement { get; private set; }

        public void Initialize()
        {
            InitializeInternal(ServerProperties.Properties.WORLD_DAY_INCREMENT, DAY / 2); // Start at midnight.
        }

        private void InitializeInternal(uint dayIncrement, uint dayStart)
        {
            lock (_lock)
            {
                DayIncrement = Math.Max(1, dayIncrement); // Clients do odd things if we try to stop time.
                _updateInterval = (int) (30000.0 / DayIncrement); // Aim for two updates per in-game minute to avoid breaking scripts expecting minute-accurate timings.

                if (_updateInterval < GameLoop.TickRate && log.IsWarnEnabled)
                {
                    log.Warn($"The update interval ({_updateInterval}) had to be set to a value lower than the server tick rate ({GameLoop.TickRate}) when trying to ensure two updates happen per in-game minute. " +
                             $"This may break scripts expecting minute-accurate timings. Consider using a lower day increment ({dayIncrement})\".");
                }

                _nightIncrement = (uint) (DayIncrement * NIGHT_INCREMENT_FACTOR);
                _dayStartTime = GameLoop.GameLoopTime - dayStart / DayIncrement;
                _nextClientResync = 0;
                Start(0);
            }
        }

        protected override int OnTick(ECSGameTimer timer)
        {
            lock (_lock)
            {
                UpdateGameTime();

                if (ServiceUtils.ShouldTickAdjust(ref _nextClientResync))
                {
                    ResyncClients();
                    _nextClientResync += CLIENT_RESYNC_INTERVAL;
                }
            }

            return _updateInterval;

            void UpdateGameTime()
            {
                // Can these overflow?
                double delta = GameLoop.GameLoopTime - _dayStartTime;
                double newGameTime = 0;

                // X complete iterations means X days have passed since `_dayStartTime`.
                while (delta > 0)
                {
                    // From midnight to 6am.
                    newGameTime += Math.Min(QUARTER_OF_A_DAY, delta * _nightIncrement);
                    delta -= QUARTER_OF_A_DAY / _nightIncrement;

                    if (delta <= 0)
                        break;

                    // From 6am to 6pm.
                    newGameTime += Math.Min(HALF_OF_A_DAY, delta * DayIncrement);
                    delta -= HALF_OF_A_DAY / DayIncrement;

                    if (delta <= 0)
                        break;

                    // From 6pm to midnight.
                    newGameTime += Math.Min(QUARTER_OF_A_DAY, delta * _nightIncrement);
                    delta -= QUARTER_OF_A_DAY / _nightIncrement;

                    if (delta <= 0)
                        break;

                    // The remainder now represents how much time has passed since the start of the current day.
                    _dayStartTime = (long) (GameLoop.GameLoopTime - delta);
                }

                CurrentGameTime = (uint) (newGameTime % DAY);
            }

            void ResyncClients()
            {
                foreach (GamePlayer player in ClientService.GetPlayers<object>(Predicate))
                    player.Out.SendTime();

                static bool Predicate(GamePlayer player, object unused)
                {
                    return player.CurrentRegion?.UseTimeManager == true;
                }
            }
        }

        public void ChangeGameTime(uint newDayIncrement, double startTimePercent)
        {
            InitializeInternal(newDayIncrement, (uint) (startTimePercent * DAY));
        }
    }
}
