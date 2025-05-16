using System;
using System.Reflection;
using System.Threading;

namespace DOL.GS
{
    // A very specialized thread pool, meant to be used by the game loop exclusively.
    // If you need more than one thread pool, you should be using the TPL instead.
    // The reasons why we're using this thread pool despite being way less robust, versatile, and sometimes a bit slower than the TPL:
    // * The memory overhead of the TPL isn't negligible when it's called hundreds of times per second.
    // * A lot easier to debug.
    public sealed class GameLoopThreadPool : IGameLoopThreadPool
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // Used to calculate the work distribution base.
        // The higher the number, the less work each worker will do per iteration.
        // Too high, and the chunk size will need to be adjusted too frequently, potentially wasting CPU time.
        // Too low, and a worker may reserve too much work, causing the other workers to wait for it.
        private const int WORK_DISTRIBUTION_BASE_FACTOR = 6;

        private int _degreeOfParallelism; // The desired degree of parallelism. This is the number of threads we want to use, including the caller thread.
        private int _workerCount; // Should always be one less than `_degreeOfParallelism`.
        private int _workDistributionBase; // Used to dynamically calculate the chunk size for each worker.
        private Thread[] _workers; // The worker threads. Does not include the caller thread.
        private Thread _watchdog; // The watchdog thread is used to monitor the worker threads and restart them if they die.
        private CountdownEvent _workerStartCountDownEvent; // Used to wait for the workers to be initialized.
        private SemaphoreSlim[] _workReady; // One for each worker appears to offer better performance than a shared one.
        private CancellationTokenSource _workersCancellationTokenSource;
        private Action<int> _action; // The actual work item.
        private int _workLeft; // The number of work items to do. This is the total number of items, not the number of items per worker.
        private int _workerCompletionCount; // The number of workers that have completed their work.
        private bool _running;

        public GameLoopThreadPool(int degreeOfParallelism)
        {
            _degreeOfParallelism = degreeOfParallelism;
        }

        public void Init()
        {
            if (Interlocked.CompareExchange(ref _running, true, false))
                return;

            _workerCount = _degreeOfParallelism - 1;
            _workDistributionBase = _degreeOfParallelism * WORK_DISTRIBUTION_BASE_FACTOR;
            _workers = new Thread[_workerCount];
            _workerStartCountDownEvent = new(_workerCount);
            _workReady = new SemaphoreSlim[_workerCount];
            _workersCancellationTokenSource = new();

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
            try
            {
                if (count <= 0)
                    return;

                _action = action;
                _workLeft = count;
                _workerCompletionCount = 0;
                int workersToStart;
                int barrierParticipants;

                // If the count is less than the degree of parallelism, only signal the required number of workers.
                // The caller thread will also be used, so in this case we need to subtract one from the amount of workers to start.
                if (count < _degreeOfParallelism)
                {
                    barrierParticipants = count;
                    workersToStart = count - 1;
                }
                else
                {
                    barrierParticipants = _degreeOfParallelism;
                    workersToStart = _workerCount;
                }

                for (int i = 0; i < workersToStart; i++)
                    _workReady[i].Release();

                PerformWork();

                // Spin very tightly until all the workers have completed their work.
                // We could adjust the spin wait time if we get here early, but this is hard to predict.
                // However we really don't want to yield the CPU here, as this could delay the return by a lot.
                // `Volatile.Read` is fine here because workers use `Interlocked`, which provides release semantics on write.
                while (Volatile.Read(ref _workerCompletionCount) < workersToStart)
                    Thread.SpinWait(1);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Critical error encountered in \"{nameof(GameLoopThreadPool)}\"", e);

                GameServer.Instance.Stop();
                return;
            }
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
            CancellationToken cancellationToken = _workersCancellationTokenSource.Token;

            while (Volatile.Read(ref _running))
            {
                try
                {
                    _workReady[workerId].Wait(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (log.IsInfoEnabled)
                        log.Info($"Thread \"{Thread.CurrentThread.Name}\" was cancelled");

                    return;
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
                        log.Error($"Thread \"{Thread.CurrentThread.Name}\"", e);
                }

                PerformWork();
                Interlocked.Increment(ref _workerCompletionCount);
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }

        private void PerformWork()
        {
            int nextWorkLeftOnLastIteration = 0; // Used to calculate the chunk size. May be inaccurate, but it's fine.
            int chunkSize;
            int start = 0;
            int end;

            try
            {
                do
                {
                    nextWorkLeftOnLastIteration = start - 1;

                    if (nextWorkLeftOnLastIteration == -1)
                        chunkSize = 1; // First iteration, we need to do at least one item.
                    else
                    {
                        chunkSize = nextWorkLeftOnLastIteration / (_workDistributionBase - Volatile.Read(ref _workerCompletionCount));

                        // Prevent infinite loops.
                        if (chunkSize < 1)
                            chunkSize = 1;
                    }

                    start = Interlocked.Add(ref _workLeft, -chunkSize);

                    if (start < 0)
                        break;

                    end = start + chunkSize;

                    for (int i = start; i < end; i++)
                        _action(i);
                } while (true);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Critical error encountered in \"{nameof(GameLoopThreadPool)}\"", e);

                GameServer.Instance.Stop();
                return;
            }
        }

        private void WatchdogLoop()
        {
            while (Volatile.Read(ref _running))
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
                        log.Error($"Thread \"{Thread.CurrentThread.Name}\"", e);
                }
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }

        public void Dispose()
        {
            if (!Interlocked.CompareExchange(ref _running, false, true))
                return;

            if (_watchdog.IsAlive)
                _watchdog.Join();

            _workerStartCountDownEvent.Wait(); // Make sure any worker being (re)started has finished.
            _workersCancellationTokenSource.Cancel();

            for (int i = 0; i < _workers.Length; i++)
            {
                Thread worker = _workers[i];

                if (worker != null && Thread.CurrentThread != worker && worker.IsAlive)
                    worker.Join();
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
