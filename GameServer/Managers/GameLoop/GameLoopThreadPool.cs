using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    // A very specialized thread pool, meant to be used by the game loop and its thread exclusively.
    // If you need more than one thread pool, you should be using the TPL instead.
    // The reasons why we're using this thread pool despite being way less robust, versatile, and sometimes a bit slower than the TPL:
    // * The memory overhead of the TPL isn't negligible when it's called hundreds of times per second.
    // * A lot easier to debug.
    public sealed class GameLoopThreadPoolMultiThreaded : GameLoopThreadPool
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // Used to calculate the work distribution base.
        // The higher the number, the less work each worker will do per iteration.
        // Too high, and the chunk size will need to be adjusted too frequently, potentially wasting CPU time.
        // Too low, and a worker may reserve too much work, causing the other workers to wait for it.
        private const int WORK_DISTRIBUTION_BASE_FACTOR = 6;

        // Thread pool lifecycle.
        private bool _running;
        private int _degreeOfParallelism;               // Total threads, including caller.
        private int _workerCount;                       // Number of dedicated worker threads (= _degreeOfParallelism - 1).
        private int _workDistributionBase;              // Used to calculate chunk size per worker.

        // Thread management.
        private Thread[] _workers;                      // Worker threads (excludes caller).
        private Thread _watchdog;                       // Monitors worker health; restarts if needed.
        private CancellationTokenSource _workersCancellationTokenSource;

        // Startup synchronization.
        private CountdownEvent _workerStartLatch;       // Signals when all workers are initialized.
        private SemaphoreSlim[] _workReady;             // Per-worker semaphores to trigger work.

        // Work dispatch.
        private Action<int> _action;                    // Work delegate.
        private int _workLeft;                          // Total items left to process.
        private int _workerCompletionCount;             // Count of workers finished for current iteration.

        public GameLoopThreadPoolMultiThreaded(int degreeOfParallelism)
        {
            _degreeOfParallelism = degreeOfParallelism;
        }

        public override void Init()
        {
            if (Interlocked.CompareExchange(ref _running, true, false))
                return;

            _workerCount = _degreeOfParallelism - 1;
            _workDistributionBase = _degreeOfParallelism * WORK_DISTRIBUTION_BASE_FACTOR;
            _workers = new Thread[_workerCount];
            _workerStartLatch = new(_workerCount);
            _workReady = new SemaphoreSlim[_workerCount];
            _workersCancellationTokenSource = new();
            base.Init();

            for (int i = 0; i < _workerCount; i++)
            {
                Thread worker = new(new ParameterizedThreadStart(InitWorker))
                {
                    Name = $"{GameLoop.THREAD_NAME}_Worker_{i}",
                    IsBackground = true
                };
                worker.Start((i, false));
            }

            _workerStartLatch.Wait(); // If for some reason a thread fails to start, we'll be waiting here forever.
            _watchdog = new(WatchdogLoop)
            {
                Name = $"{GameLoop.THREAD_NAME}_Watchdog",
                Priority = ThreadPriority.Lowest,
                IsBackground = true
            };
            _watchdog.Start();
        }

        public override void Work(int count, Action<int> action)
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
                    log.Error($"Critical error encountered in \"{nameof(GameLoopThreadPoolMultiThreaded)}\"", e);

                GameServer.Instance.Stop();
                return;
            }
        }

        public override void PrepareForNextTick()
        {
            Work(_degreeOfParallelism, static _ => _localPools.Reset());
        }

        public override void Dispose()
        {
            if (!Interlocked.CompareExchange(ref _running, false, true))
                return;

            if (_watchdog.IsAlive)
                _watchdog.Join();

            _workerStartLatch.Wait(); // Make sure any worker being (re)started has finished.
            _workersCancellationTokenSource.Cancel();

            for (int i = 0; i < _workers.Length; i++)
            {
                Thread worker = _workers[i];

                if (worker != null && Thread.CurrentThread != worker && worker.IsAlive)
                    worker.Join();
            }
        }

        protected override void InitWorker(object obj)
        {
            (int Id, bool Restart) = ((int, bool)) obj;
            _workers[Id] = Thread.CurrentThread;
            _workReady[Id]?.Dispose();
            _workReady[Id] = new SemaphoreSlim(0, 1);
            base.InitWorker(obj);
            _workerStartLatch.Signal();

            // If this is a restart, we need to free the caller thread.
            if (Restart)
                Interlocked.Increment(ref _workerCompletionCount);

            RunWorker(_workReady[Id], _workersCancellationTokenSource.Token);
        }

        private void RunWorker(SemaphoreSlim workReady, CancellationToken cancellationToken)
        {
            while (Volatile.Read(ref _running))
            {
                try
                {
                    workReady.Wait(cancellationToken);
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
                    end = start + chunkSize;

                    if (start < 0)
                        start = 0;

                    for (int i = start; i < end; i++)
                        _action(i);
                } while (start > 0);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Critical error encountered in \"{nameof(GameLoopThreadPoolMultiThreaded)}\"", e);

                GameServer.Instance.Stop();
                return;
            }
        }

        private void WatchdogLoop()
        {
            List<int> _workersToRestart = new();

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

                        _workersToRestart.Add(i);
                    }

                    if (_workersToRestart.Count == 0)
                        continue;

                    // Initialize the countdown event before starting any thread.
                    _workerStartLatch = new(_workersToRestart.Count);

                    foreach (int id in _workersToRestart)
                    {
                        _workers[id] = null;
                        Thread newThread = new(new ParameterizedThreadStart(InitWorker))
                        {
                            Name = $"{GameLoop.THREAD_NAME}_Worker_{id}",
                            IsBackground = true,
                        };
                        newThread.Start((id, true));
                    }

                    _workersToRestart.Clear();
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
    }

    public sealed class GameLoopThreadPoolSingleThreaded : GameLoopThreadPool
    {
        public GameLoopThreadPoolSingleThreaded() { }

        public override void Work(int count, Action<int> action)
        {
            for (int i = 0; i < count; i++)
                action(i);
        }

        public override void PrepareForNextTick()
        {
            _localPools.Reset();
        }

        public override void Dispose() { }
    }

    public abstract class GameLoopThreadPool : IDisposable
    {
        [ThreadStatic]
        protected static LocalPools _localPools;

        public virtual void Init()
        {
            _localPools = new();
        }

        public abstract void Work(int count, Action<int> action);

        public abstract void PrepareForNextTick();

        public abstract void Dispose();

        public T Rent<T>(PooledObjectKey key, Action<T> initializer) where T : IPooledObject<T>, new()
        {
            T result = _localPools != null ? _localPools.Rent<T>(key) : new();
            initializer?.Invoke(result);
            return result;
        }

        protected virtual void InitWorker(object obj)
        {
            _localPools = new();
        }

        protected sealed class LocalPools
        {
            private Dictionary<PooledObjectKey, IFrameListPool> _localPools = new()
            {
                { PooledObjectKey.InPacket, new FrameListPool<GSPacketIn>() },
                { PooledObjectKey.TcpOutPacket, new FrameListPool<GSTCPPacketOut>() },
                { PooledObjectKey.UdpOutPacket, new FrameListPool<GSUDPPacketOut>() }
            };

            public T Rent<T>(PooledObjectKey key) where T : new()
            {
                return (_localPools[key] as FrameListPool<T>).Rent();
            }

            public void Reset()
            {
                foreach (var pair in _localPools)
                    pair.Value.Reset();
            }

            private sealed class FrameListPool<T> : IFrameListPool where T : new()
            {
                private List<T> _items = new();
                private int _used;

                public T Rent()
                {
                    if (_used < _items.Count)
                        return _items[_used++];

                    T item = new();
                    _items.Add(item);
                    _used++;
                    return item;
                }

                public void Reset()
                {
                    _used = 0;
                }
            }

            private interface IFrameListPool
            {
                void Reset();
            }
        }
    }

    public enum PooledObjectKey
    {
        InPacket,
        TcpOutPacket,
        UdpOutPacket
    }

    public interface IPooledObject<T>
    {
        static abstract PooledObjectKey PooledObjectKey { get; }
        static abstract T Rent(Action<T> initializer);
    }
}
