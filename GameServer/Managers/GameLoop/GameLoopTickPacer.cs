using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class GameLoopTickPacer
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const bool DYNAMIC_BUSY_WAIT_THRESHOLD = true;

        private double _tickDuration;
        private bool _running;
        private Thread _busyWaitThresholdThread;
        private int _busyWaitThreshold;

        private double _gameLoopTime;
        private double _totalElapsedTime;
        private Stopwatch _stopwatch;

        public GameLoopTickPacerStats Stats { get; private set; }

        public GameLoopTickPacer(double tickDuration)
        {
            if (tickDuration <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickDuration), "Tick duration must be a positive value.");

            _tickDuration = tickDuration;
        }

        public void Start()
        {
            if (_running)
                return;

            _running = true;

            if (DYNAMIC_BUSY_WAIT_THRESHOLD)
            {
                _busyWaitThresholdThread = new(new ThreadStart(UpdateBusyWaitThreshold))
                {
                    Name = $"{GameLoop.THREAD_NAME}_BusyWaitThreshold",
                    Priority = ThreadPriority.AboveNormal,
                    IsBackground = true
                };
                _busyWaitThresholdThread.Start();
            }

            Stats = new([60000, 30000, 10000], 1000.0 / _tickDuration);
            _stopwatch = Stopwatch.StartNew();
        }

        public void Stop()
        {
            if (_busyWaitThresholdThread == null)
                return;

            _running = false;

            if (_busyWaitThresholdThread != Thread.CurrentThread && _busyWaitThresholdThread.IsAlive)
                _busyWaitThresholdThread.Interrupt(); // This thread sleeps for a long time, let's not wait for it to finish.

            _busyWaitThresholdThread = null;
        }

        public long WaitForNextTick()
        {
            int sleepFor = (int) (_tickDuration - _stopwatch.Elapsed.TotalMilliseconds);
            int busyWaitThreshold = _busyWaitThreshold;

            if (sleepFor >= busyWaitThreshold)
                Thread.Sleep(sleepFor - busyWaitThreshold);
            else
                Thread.Yield();

            if (_tickDuration > _stopwatch.Elapsed.TotalMilliseconds)
            {
                SpinWait spinWait = new();

                while (_tickDuration > _stopwatch.Elapsed.TotalMilliseconds)
                    spinWait.SpinOnce(-1);
            }

            double elapsedTime = _stopwatch.Elapsed.TotalMilliseconds;
            _totalElapsedTime += elapsedTime;
            _stopwatch.Restart();

            // In case the game loop is running faster than the tick rate. We don't want things to run faster than intended.
            _gameLoopTime += elapsedTime < _tickDuration ? elapsedTime : _tickDuration;
            Stats.RecordTick(_totalElapsedTime);
            return (long) _gameLoopTime;
        }

        private void UpdateBusyWaitThreshold()
        {
            int maxIteration = 10;
            int sleepFor = 1;
            int pauseFor = 10000;
            Stopwatch stopwatch = new();
            stopwatch.Start();

            try
            {
                while (Volatile.Read(ref _running))
                {
                    double start;
                    double overSleptFor;
                    double highest = 0;

                    for (int i = 0; i < maxIteration; i++)
                    {
                        start = stopwatch.Elapsed.TotalMilliseconds;
                        Thread.Sleep(sleepFor);
                        overSleptFor = stopwatch.Elapsed.TotalMilliseconds - start - sleepFor;

                        if (highest < overSleptFor)
                            highest = overSleptFor;
                    }

                    _busyWaitThreshold = Math.Max(0, (int) highest);
                    Thread.Sleep(pauseFor);
                }
            }
            catch (ThreadInterruptedException)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Thread \"{Thread.CurrentThread.Name}\" was interrupted");
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Critical error encountered in {nameof(GameLoopTickPacer)}: {e}");
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }
    }
}
