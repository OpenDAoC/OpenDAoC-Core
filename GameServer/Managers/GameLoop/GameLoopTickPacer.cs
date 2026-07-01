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
        private int _estimatedSleepOvershootMs;
        private long _estimatedSleepOvershootTicks;

        private double _gameLoopTimeMs;
        private long _nextTickDeadlineTicks;
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
            _nextTickDeadlineTicks = _stopwatch.ElapsedTicks + _tickDurationTicks;
        }

        public void Stop()
        {
            _running = false;

            if (_busyWaitThresholdThread == null)
                return;

            if (_busyWaitThresholdThread != Thread.CurrentThread && _busyWaitThresholdThread.IsAlive)
                _busyWaitThresholdThread.Interrupt(); // This thread sleeps for a long time, let's not wait for it to finish.

            _busyWaitThresholdThread = null;
        }

        public long WaitForNextTick()
        {
            long currentTicks = _stopwatch.ElapsedTicks;
            long ticksUntilDeadline = _nextTickDeadlineTicks - currentTicks;

            // Only sleep if enough time remains that the OS overshoot won't carry us past the deadline.
            if (ticksUntilDeadline >= _estimatedSleepOvershootTicks)
            {
                int sleepDurationMs = (int) (ticksUntilDeadline * 1000.0 / Stopwatch.Frequency) - _estimatedSleepOvershootMs;

                if (sleepDurationMs > 0)
                    Thread.Sleep(sleepDurationMs);
            }

            while (_stopwatch.ElapsedTicks < _nextTickDeadlineTicks)
                Thread.SpinWait(10);

            currentTicks = _stopwatch.ElapsedTicks;

            // Drop the missed frames if this tick took longer than intended.
            if (currentTicks - _nextTickDeadlineTicks >= _tickDurationTicks)
                _nextTickDeadlineTicks = currentTicks + _tickDurationTicks;
            else
                _nextTickDeadlineTicks += _tickDurationTicks;

            // Game loop time advances by exactly by the target tick duration.
            _gameLoopTimeMs += _tickDurationMs;

            // Record tick using real elapsed time.
            double _realElapsedTimeMs = _stopwatch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
            Stats.RecordTick(_realElapsedTimeMs);

            return (long) _gameLoopTimeMs;
        }

        private void UpdateBusyWaitThreshold()
        {
            // Periodically measures how much Thread.Sleep(1) overshoots on this machine/OS.

            const int MAX_ITERATIONS = 10;
            const int SLEEP_FOR_MS = 1;
            const int RECALIBRATION_INTERVAL_MS = 10000;

            Stopwatch stopwatch = new();
            stopwatch.Start();

            try
            {
                while (Volatile.Read(ref _running))
                {
                    double start;
                    double sleepOvershootMs;
                    double maxOversleepMs = 0;

                    for (int i = 0; i < MAX_ITERATIONS; i++)
                    {
                        start = stopwatch.Elapsed.TotalMilliseconds;
                        Thread.Sleep(SLEEP_FOR_MS);
                        sleepOvershootMs = stopwatch.Elapsed.TotalMilliseconds - start - SLEEP_FOR_MS;

                        if (maxOversleepMs < sleepOvershootMs)
                            maxOversleepMs = sleepOvershootMs;
                    }

                    _estimatedSleepOvershootMs = (int) Math.Max(0, Math.Ceiling(maxOversleepMs));
                    _estimatedSleepOvershootTicks = (long) (_estimatedSleepOvershootMs * Stopwatch.Frequency / 1000.0);
                    Thread.Sleep(RECALIBRATION_INTERVAL_MS);
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
