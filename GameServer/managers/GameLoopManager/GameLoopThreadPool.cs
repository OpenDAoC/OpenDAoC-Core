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

        private int _requestedThreadCount;
        private ConcurrentDictionary<int, Thread> _threads = new();
        private Thread _watchdog;
        private CountdownEvent _threadStartCountDownEvent;
        private ManualResetEventSlim _workReady = new();
        private ManualResetEventSlim _workDone = new();
        private Barrier _barrier;

        private int _activeThreadCount; // The amount of active threads at the time `Run()` was called.
        private int _workCount;
        private Action<int> _action;
        private int _chunk;
        private int _remainder;

        public GameLoopThreadPool(int threadCount)
        {
            _requestedThreadCount = threadCount;
            _threadStartCountDownEvent = new(_requestedThreadCount);
            _barrier = new(_requestedThreadCount, PostPhaseAction);
            StartWorkerThreads();
            _threadStartCountDownEvent.Wait(); // If for some reason a thread fails to start, we'll be waiting forever.
            _threadStartCountDownEvent = null; // Not needed anymore.
            _watchdog = StartWatchdog();

            void StartWorkerThreads()
            {
                for (int i = 0; i < threadCount; i++)
                {
                    _threads[i] = new Thread(new ParameterizedThreadStart(WorkerLoop))
                    {
                        IsBackground = true,
                        Name = $"{GameLoop.THREAD_NAME}_Worker_{i}"
                    };
                    _threads[i].Start(i);
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

            _activeThreadCount = _barrier.ParticipantCount;

            if (_activeThreadCount == 0)
                return;

            _workCount = count;
            _action = action;
            _chunk = _workCount / _activeThreadCount;
            _remainder = _workCount % _activeThreadCount;

            // Signal the threads to start working and wait for them to finish.
            _workDone.Reset();
            _workReady.Set();
            _workDone.Wait();
        }

        private void WorkerLoop(object obj)
        {
            int threadIndex = (int) obj;
            _threads[threadIndex] = Thread.CurrentThread;
            _threadStartCountDownEvent?.Signal(); // Null if this thread is started from the watchdog.

            while (true)
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

                    // `_activeThreadCount` represents the number of participants in the barrier at the time `Run()` was called.
                    // If a thread was recreated by the watchdog, the number of threads actually active can be superior to that number.
                    // We must prevent those threads from working during this phase since `_activeThreadCount` is used to calculate the chunk size.
                    if (_activeThreadCount > threadIndex)
                    {
                        int start = threadIndex * _chunk + Math.Min(threadIndex, _remainder);
                        int end = start + _chunk;

                        if (threadIndex < _remainder)
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
            }
        }

        private void PostPhaseAction(Barrier barrier)
        {
            try
            {
                // This method is called by the barrier when all threads have finished their work.
                // It signals the thread in `Run()` to continue.
                _workReady.Reset();
                _workDone.Set();
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
                    foreach (var pair in _threads)
                    {
                        int threadId = pair.Key;
                        Thread thread = pair.Value;

                        if (!thread.Join(100))
                            continue;

                        if (log.IsWarnEnabled)
                            log.Warn($"Watchdog: Thread \"{thread.Name}\" is dead. Restarting...");

                        _threads.TryRemove(threadId, out _);
                        _barrier.RemoveParticipant(); // Free the other threads immediately.
                        Thread newThread = new(new ParameterizedThreadStart(WorkerLoopRestart))
                        {
                            Name = $"{GameLoop.THREAD_NAME}_Worker_{threadId}",
                            IsBackground = true,
                        };
                        newThread.Start(threadId);
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
                WorkerLoop(obj);
            }
        }

        public void Dispose()
        {
            _watchdog.Interrupt();
            _watchdog.Join();
            _watchdog = null;

            foreach (var pair in _threads)
            {
                Thread thread = pair.Value;

                if (Thread.CurrentThread != thread && thread.IsAlive)
                {
                    thread.Interrupt();
                    thread.Join();
                }
            }

            _threads.Clear();
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
