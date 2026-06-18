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

        private readonly double _tickDurationMs;
        private readonly long _tickDurationTicks;
        private bool _running;
        private Thread _busyWaitThresholdThread;
        private int _busyWaitThresholdMs;
        private long _busyWaitThresholdTicks;

        private double _gameLoopTime;
        private double _totalElapsedTimeMs;
        private Stopwatch _stopwatch;

        public GameLoopTickPacerStats Stats { get; private set; }

        public GameLoopTickPacer(double tickDurationMs)
        {
            if (tickDurationMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickDurationMs), "Tick duration must be a positive value.");

            _tickDurationMs = tickDurationMs;
            _tickDurationTicks = (long) (_tickDurationMs * Stopwatch.Frequency / 1000.0);
        }

        public void Start()
        {
            if (_running)
                return;

            _running = true;

            if (DYNAMIC_BUSY_WAIT_THRESHOLD && Environment.ProcessorCount > 1)
            {
                _busyWaitThresholdThread = new(new ThreadStart(UpdateBusyWaitThreshold))
                {
                    Name = $"{GameLoop.THREAD_NAME}_BusyWaitThreshold",
                    Priority = ThreadPriority.AboveNormal,
                    IsBackground = true
                };
                _busyWaitThresholdThread.Start();
            }

            Stats = new([60000, 30000, 10000], 1000.0 / _tickDurationMs);
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
            long startTicks = _stopwatch.ElapsedTicks;
            long ticksRemaining = _tickDurationTicks - startTicks;

            if (ticksRemaining >= _busyWaitThresholdTicks)
            {
                int sleepForMs = (int) (ticksRemaining * 1000.0 / Stopwatch.Frequency) - _busyWaitThresholdMs;

                if (sleepForMs > 0)
                    Thread.Sleep(sleepForMs);
            }

            // Any small number will do here. Technically, this could be 0.
            // If the game loop appears to overshoot the tick duration for no reason, this can be reduced even further.
            while (_stopwatch.ElapsedTicks < _tickDurationTicks)
                Thread.SpinWait(10);

            double elapsedTime = _stopwatch.Elapsed.TotalMilliseconds;
            _totalElapsedTimeMs += elapsedTime;
            _stopwatch.Restart();

            // In case the game loop is running faster than the tick rate. We don't want things to run faster than intended.
            _gameLoopTime += elapsedTime < _tickDurationMs ? elapsedTime : _tickDurationMs;
            Stats.RecordTick(_totalElapsedTimeMs);
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

                    _busyWaitThresholdMs = (int) Math.Max(0, Math.Ceiling(highest));
                    _busyWaitThresholdTicks = (long) (_busyWaitThresholdMs * Stopwatch.Frequency / 1000.0);
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
