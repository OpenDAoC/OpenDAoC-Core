using System;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;

namespace DOL.GS
{
    // A very specialized and barebone thread pool, meant to be used by the game loop exclusively.
    // The reasons why we're using this thread pool despite being way less robust than TPL:
    // * The memory overhead of `Parallel.For` or `Parallel.ForEach` isn't negligible when it's called hundreds of times per second.
    // * A lot easier to debug.
    public sealed class GameLoopThreadPool : IGameLoopThreadPool
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private int _degreeOfParallelism;
        private int _workerCount;
        private ConcurrentDictionary<int, Thread> _workers = new();
        private Thread _watchdog;
        private CountdownEvent _workerStartCountDownEvent;
        private ManualResetEventSlim _workReady = new();
        private Barrier _barrier;

        private int _activeWorkerCount; // The amount of active threads at the time `Run()` was called.
        private int _workCount;
        private Action<int> _action;
        private int _chunk;
        private int _remainder;

        public GameLoopThreadPool(int degreeOfParallelism)
        {
            _degreeOfParallelism = degreeOfParallelism;
            _workerCount = _degreeOfParallelism - 1; // We spawn one less thread because the caller thread will also be used.
            _workerStartCountDownEvent = new(_workerCount);
            _barrier = new(_degreeOfParallelism, PostPhaseAction);
            StartWorkerThreads();
            _workerStartCountDownEvent.Wait(); // If for some reason a thread fails to start, we'll be waiting here forever.
            _workerStartCountDownEvent = null; // Not needed anymore.
            _watchdog = StartWatchdog();

            void StartWorkerThreads()
            {
                for (int i = 0; i < _workerCount; i++)
                {
                    _workers[i] = new Thread(new ParameterizedThreadStart(InitWorker))
                    {
                        IsBackground = true,
                        Name = $"{GameLoop.THREAD_NAME}_Worker_{i}"
                    };
                    _workers[i].Start(i);
                }
            }

            Thread StartWatchdog()
            {
                Thread watchdog = new(WatchdogLoop)
                {
                    Name = $"{GameLoop.THREAD_NAME}_Watchdog",
                    Priority = ThreadPriority.Lowest,
                    IsBackground = true
                };
                watchdog.Start();
                return watchdog;
            }
        }

        public void Work(int count, Action<int> action)
        {
            if (count < 0)
                return;

            _activeWorkerCount = _barrier.ParticipantCount;

            if (_activeWorkerCount == 0)
                return;

            _workCount = count;
            _action = action;
            _chunk = _workCount / _activeWorkerCount;
            _remainder = _workCount % _activeWorkerCount;

            // Signal the workers to start working and have this thread join them.
            _workReady.Set();
            RunWorker(_degreeOfParallelism);
        }

        private void InitWorker(object obj)
        {
            int workerId = (int) obj;
            _workers[workerId] = Thread.CurrentThread;
            _workerStartCountDownEvent?.Signal(); // Null if this thread is started from the watchdog.
            RunWorker(workerId);
        }

        private void RunWorker(int workerId)
        {
            bool isWorkerThread = workerId < _degreeOfParallelism;

            do
            {
                try
                {
                    try
                    {
                        _workReady.Wait();
                    }
                    catch (ThreadInterruptedException)
                    {
                        if (log.IsInfoEnabled)
                            log.Info($"Thread \"{Thread.CurrentThread.Name}\" was interrupted during work wait");

                        return;
                    }

                    // `_activeWorkerCount` represents the number of participants in the barrier at the time `Run()` was called.
                    // If a worker was recreated by the watchdog, the number of workers actually active can be superior to that number.
                    // We must prevent those from working during this phase since `_activeWorkerCount` is used to calculate the chunk size.
                    if (workerId < _activeWorkerCount)
                    {
                        int start = workerId * _chunk + Math.Min(workerId, _remainder);
                        int end = start + _chunk;

                        if (workerId < _remainder)
                            end++;

                        for (int i = start; i < end; i++)
                            _action(i);
                    }
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Thread \"{Thread.CurrentThread.Name}\" exception: {e.Message}", e);
                }

                try
                {
                    _barrier.SignalAndWait();
                }
                catch (ThreadInterruptedException)
                {
                    if (log.IsInfoEnabled)
                        log.Info($"Thread \"{Thread.CurrentThread.Name}\" was interrupted during barrier");

                    return;
                }
            } while (isWorkerThread);
        }

        private void PostPhaseAction(Barrier barrier)
        {
            try
            {
                // This method is called by the barrier when all threads have finished their work.
                _workReady.Reset();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Critical error encountered in {nameof(GameLoopThreadPool)}: {e}");

                GameServer.Instance.Stop();
            }
        }

        private void WatchdogLoop()
        {
            while (true)
            {
                try
                {
                    // Remove the dead threads from the dictionary, unregister them from the barrier, then replace them.
                    foreach (var pair in _workers)
                    {
                        int workerId = pair.Key;
                        Thread worker = pair.Value;

                        if (!worker.Join(100))
                            continue;

                        if (log.IsWarnEnabled)
                            log.Warn($"Watchdog: Thread \"{worker.Name}\" is dead. Restarting...");

                        _workers.TryRemove(workerId, out _);
                        _barrier.RemoveParticipant(); // Free the other threads immediately.
                        Thread newThread = new(new ParameterizedThreadStart(WorkerLoopRestart))
                        {
                            Name = $"{GameLoop.THREAD_NAME}_Worker_{workerId}",
                            IsBackground = true,
                        };
                        newThread.Start(workerId);
                    }
                }
                catch (ThreadInterruptedException)
                {
                    if (log.IsInfoEnabled)
                        log.Info($"Thread \"{Thread.CurrentThread.Name}\" was interrupted");

                    return;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Thread \"{Thread.CurrentThread.Name}\" exception: {e.Message}", e);
                }
            }

            void WorkerLoopRestart(object obj)
            {
                // This method is called by the watchdog when a thread dies.
                // We make sure a barrier phase is completed before we resume work, otherwise we take the risk of performing the same work twice.
                // This means any work that wasn't completed by the dead thread is lost. But keeping track of it would be too expensive.
                _barrier.AddParticipant();
                _barrier.SignalAndWait();
                InitWorker(obj);
            }
        }

        public void Dispose()
        {
            _watchdog.Interrupt();
            _watchdog.Join();
            _watchdog = null;

            foreach (var pair in _workers)
            {
                Thread worker = pair.Value;

                if (Thread.CurrentThread != worker && worker.IsAlive)
                {
                    worker.Interrupt();
                    worker.Join();
                }
            }

            _workers.Clear();
        }
    }

    public sealed class GameLoopThreadPoolSingleThreaded : IGameLoopThreadPool
    {
        public GameLoopThreadPoolSingleThreaded() { }

        public void Work(int count, Action<int> action)
        {
            for (int i = 0; i < count; i++)
                action(i);
        }

        public void Dispose() { }
    }

    public interface IGameLoopThreadPool : IDisposable
    {
        public void Work(int count, Action<int> action);
    }
}
