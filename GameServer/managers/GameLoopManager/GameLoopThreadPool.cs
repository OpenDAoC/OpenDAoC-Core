using System;
using System.Reflection;
using System.Threading;

namespace DOL.GS
{
    // A very specialized and barebone thread pool, meant to be used by the game loop exclusively.
    // The reasons why we're using this thread pool despite being way less robust than TPL:
    // * The memory overhead of `Parallel.For` or `Parallel.ForEach` isn't negligible when it's called hundreds of times per second.
    // * A lot easier to debug.
    public sealed class GameLoopThreadPool : IGameLoopThreadPool
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // The ratio of work that will be chunked (50 = 50%). The rest will be shared.
        // Chunked work = work that is split into chunks and assigned to each worker.
        // Shared work = Tail work that is shared between the workers.
        // 50 tends to give good results, but the sweet spot probably varies depending on the workload.
        private const int CHUNKED_WORK_RATIO = 50;

        private int _degreeOfParallelism; // The desired degree of parallelism. This is the number of threads we want to use, including the caller thread.
        private int _workerCount; // Should always be one less than `_degreeOfParallelism`.
        private Thread[] _workers; // The worker threads. Does not include the caller thread.
        private Thread _watchdog; // The watchdog thread is used to monitor the worker threads and restart them if they die.
        private CountdownEvent _workerStartCountDownEvent; // The countdown event is used to wait for all the workers to start before proceeding.
        private SemaphoreSlim[] _workReady; // One for each worker appears to offer better performance than a shared one.
        private Barrier _barrier; // The barrier is used to synchronize the workers and the main thread.
        private Action<int> _action; // The actual work item.
        private int _workCount; // The number of work items to do. This is the total number of items, not the number of items per worker.
        private int _chunk; // The size of the chunk of work each worker will do.
        private int _remainder; // The remainder of the chunked work.
        private int _sharedWorkCurrentIndex; // The current index of the shared work, modified by the workers.

        public GameLoopThreadPool(int degreeOfParallelism)
        {
            _degreeOfParallelism = degreeOfParallelism;
        }

        public void Init()
        {
            _workerCount = _degreeOfParallelism - 1;
            _workers = new Thread[_workerCount];
            _workerStartCountDownEvent = new(_workerCount);
            _workReady = new SemaphoreSlim[_workerCount];
            _barrier = new(0);

            for (int i = 0; i < _workerCount; i++)
            {
                Thread worker = new(new ParameterizedThreadStart(InitWorker))
                {
                    Name = $"{GameLoop.THREAD_NAME}_Worker_{i}",
                    IsBackground = true
                };
                worker.Start(i);
            }

            _workerStartCountDownEvent.Wait(); // If for some reason a thread fails to start, we'll be waiting here forever.
            _watchdog = new(WatchdogLoop)
            {
                Name = $"{GameLoop.THREAD_NAME}_Watchdog",
                Priority = ThreadPriority.Lowest,
                IsBackground = true
            };
            _watchdog.Start();
        }

        public void Work(int count, Action<int> action)
        {
            if (count <= 0)
                return;

            _action = action;
            _workCount = count;
            int workersToStart;
            int barrierParticipants;

            if (count < _degreeOfParallelism)
            {
                // If the count is less than the degree of parallelism, only signal the required number of workers.
                // Every signaled worker will have a slice of the work to do, and no work will be shared.
                // The caller thread will also be used, so we need to subtract one from the worker count.

                barrierParticipants = count;
                workersToStart = count - 1;
            }
            else
            {
                // if the count is greater than the degree of parallelism, we split the work into two parts:
                // 1. The first part is split into chunks, and each worker will work on its own chunk.
                // 2. The second part is shared work, where each worker will take a piece of work from the shared pool in a first-come-first-served manner.

                if (count != _degreeOfParallelism)
                    count = count * CHUNKED_WORK_RATIO / 100;

                barrierParticipants = _degreeOfParallelism;
                workersToStart = _workerCount;
            }

            _chunk = count / _degreeOfParallelism;
            _remainder = count % _degreeOfParallelism;
            _sharedWorkCurrentIndex = count;
            _barrier.AddParticipants(barrierParticipants);

            for (int i = 0; i < workersToStart; i++)
                _workReady[i].Release();

            // Assign an id to the caller thread, so it can be used as a worker too.
            RunMainThread(workersToStart);

            // Clean up the barrier for the next work call.
            // Remove ourself first to free the workers, then remove the rest if any.
            // Attempting to remove everyone at once will throw an exception.
            _barrier.RemoveParticipant();

            if (barrierParticipants > 1)
                _barrier.RemoveParticipants(barrierParticipants - 1);
        }

        private void InitWorker(object obj)
        {
            int workerId = (int) obj;
            _workers[workerId] = Thread.CurrentThread;
            _workReady[workerId]?.Dispose();
            _workReady[workerId] = new SemaphoreSlim(0, 1);
            _workerStartCountDownEvent.Signal();
            RunWorker(workerId);
        }

        private void RunWorker(int workerId)
        {
            do
            {
                try
                {
                    _workReady[workerId].Wait();
                }
                catch (ThreadInterruptedException)
                {
                    if (log.IsInfoEnabled)
                        log.Info($"Thread \"{Thread.CurrentThread.Name}\" was interrupted during work wait");

                    return;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Thread \"{Thread.CurrentThread.Name}\" exception: {e.Message}", e);
                }

                PerformWork(workerId);

                // The workers signal the barrier when they're done with their work.
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
            } while (true);
        }

        private void RunMainThread(int workerId)
        {
            PerformWork(workerId);

            // The main thread spins until the workers are done.
            if (_barrier.ParticipantsRemaining > 1)
            {
                SpinWait spinWait = new();

                while (_barrier.ParticipantsRemaining > 1)
                    spinWait.SpinOnce(-1);
            }
        }

        private void PerformWork(int workerId)
        {
            int work;

            // Perform the chunked work.
            int start = workerId * _chunk + Math.Min(workerId, _remainder);
            int end = start + _chunk;

            if (workerId < _remainder)
                end++;

            for (work = start; work < end; work++)
                _action(work);

            // Attempt an early exit. May save some CPU cycles.
            if (Volatile.Read(ref _sharedWorkCurrentIndex) >= _workCount)
                return;

            // Perform the shared work.
            do
            {
                if ((work = Interlocked.Increment(ref _sharedWorkCurrentIndex) - 1) >= _workCount)
                    break;

                _action(work);
            } while (true);
        }

        private void WatchdogLoop()
        {
            while (true)
            {
                try
                {
                    // Remove the dead threads from the dictionary, then replace them.
                    for (int i = 0; i < _workers.Length; i++)
                    {
                        Thread worker = _workers[i];

                        // Thread is null if it was removed by the watchdog. At this point it's already being replaced, hopefully.
                        if (worker == null || !worker.Join(100))
                            continue;

                        if (log.IsWarnEnabled)
                            log.Warn($"Watchdog: Thread \"{worker.Name}\" is dead. Restarting...");

                        _workerStartCountDownEvent.AddCount();
                        _workers[i] = null;
                        Thread newThread = new(new ParameterizedThreadStart(InitWorker))
                        {
                            Name = $"{GameLoop.THREAD_NAME}_Worker_{i}",
                            IsBackground = true,
                        };
                        newThread.Start(i);
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
        }

        public void Dispose()
        {
            _watchdog.Interrupt();
            _watchdog.Join();
            _workerStartCountDownEvent.Wait(); // Make sure any worker being (re)started has finished.

            for (int i = 0; i < _workers.Length; i++)
            {
                Thread worker = _workers[i];

                if (worker != null && Thread.CurrentThread != worker && worker.IsAlive)
                {
                    worker.Interrupt();
                    worker.Join();
                }
            }
        }
    }

    public sealed class GameLoopThreadPoolSingleThreaded : IGameLoopThreadPool
    {
        public GameLoopThreadPoolSingleThreaded() { }

        public void Init() { }

        public void Work(int count, Action<int> action)
        {
            for (int i = 0; i < count; i++)
                action(i);
        }

        public void Dispose() { }
    }

    public interface IGameLoopThreadPool : IDisposable
    {
        public void Init();
        public void Work(int count, Action<int> action);
    }
}
