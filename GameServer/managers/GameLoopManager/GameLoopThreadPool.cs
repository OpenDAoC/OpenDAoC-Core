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

        // The ratio of work that will be chunked (50 = 50%). The rest will be shared.
        // Chunked work = work that is split into chunks and assigned to each worker.
        // Shared work = Tail work that is shared between the workers.
        // 50 tends to give good results, but the sweet spot probably varies depending on the workload.
        private const int CHUNKED_WORK_RATIO = 50;

        private int _degreeOfParallelism; // The desired degree of parallelism. This is the number of threads we want to use, including the caller thread.
        private int _workerCount; // Should always be one less than `_degreeOfParallelism`.
        private Thread[] _workers; // The worker threads. Does not include the caller thread.
        private Thread _watchdog; // The watchdog thread is used to monitor the worker threads and restart them if they die.
        private CountdownEvent _workerStartCountDownEvent; // Used to wait for the workers to be initialized.
        private SemaphoreSlim[] _workReady; // One for each worker appears to offer better performance than a shared one.
        private CancellationTokenSource _workersCancellationTokenSource;
        private Action<int> _action; // The actual work item.
        private int _workCount; // The number of work items to do. This is the total number of items, not the number of items per worker.
        private int _chunk; // The size of the chunk of work each worker will do.
        private int _remainder; // The chunked work that couldn't be evenly divided between the workers.
        private int _sharedWorkCurrentIndex; // The current index of the shared work, modified by the workers.
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
                _workCount = count;
                _workerCompletionCount = 0;
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

                for (int i = 0; i < workersToStart; i++)
                    _workReady[i].Release();

                // Use the last id for the caller thread.
                PerformWork(workersToStart);

                // Spin very tightly until all the workers have completed their work.
                // We could adjust the spin wait time if we get here early, but this is hard to predict.
                // However we really don't want to yield the CPU here, as this could delay the return by a lot.
                // `Volatile.Read` is fine here because workers use `Interlocked.Increment`, which provides release semantics on write.
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

                PerformWork(workerId);
                Interlocked.Increment(ref _workerCompletionCount);
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }

        private void PerformWork(int workerId)
        {
            try
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
                        return;

                    _action(work);
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
