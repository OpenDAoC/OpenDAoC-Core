using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    // A very specialized thread pool, meant to be used by the game loop and its dedicated thread exclusively.
    public sealed class GameLoopThreadPoolMultiThreaded : GameLoopThreadPool
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const int MAX_DEGREE_OF_PARALLELISM = 128;

        // Bias factor that shapes an inverse power-law curve used to scale chunk sizes.
        // Higher values favor smaller, safer chunks for better load balancing;
        // lower values produce larger chunks for faster throughput when work is even.
        // 2.5 appears to be a good default for real-world skew.
        private const double WORK_SPLIT_BIAS_FACTOR = 2.5;

        // Thread pool configuration and state.
        private bool _running;
        private int _degreeOfParallelism;               // Total threads, including caller.
        private int _workerCount;                       // Number of dedicated worker threads (= _degreeOfParallelism - 1).
        private double[] _workSplitBiasTable;           // Lookup table for chunk size biasing.

        // Thread management.
        private Thread[] _workers;                      // Worker threads (excludes caller).
        private Thread _watchdog;                       // Monitors worker health; restarts if needed.
        private CancellationTokenSource _shutdownToken;

        // Work coordination.
        private CountdownEvent _workerStartLatch;       // Signals when all workers are initialized.
        private ManualResetEventSlim[] _workReady;      // Per-worker event to trigger work.

        // Work dispatch.
        private Action<int> _workAction;                // Per-item work action.
        private Action _workerRoutine;                  // Worker thread routine.
        private int _workLeft;                          // Total items left to process.
        private int _workerCompletionCount;             // Count of workers finished for current iteration.

        public GameLoopThreadPoolMultiThreaded(int degreeOfParallelism)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(degreeOfParallelism, MAX_DEGREE_OF_PARALLELISM);
            _degreeOfParallelism = degreeOfParallelism;
        }

        public override void Init()
        {
            if (Interlocked.CompareExchange(ref _running, true, false))
                return;

            Configure();
            BuildChunkDivisorTable();
            StartWorkers();
            StartWatchdog();

            void Configure()
            {
                _workerCount = _degreeOfParallelism - 1;
                _workers = new Thread[_workerCount];
                _workerStartLatch = new(_workerCount);
                _workReady = new ManualResetEventSlim[_workerCount];
                _shutdownToken = new();
                _workerRoutine = ProcessWorkActions;
                base.Init();
            }

            void BuildChunkDivisorTable()
            {
                _workSplitBiasTable = new double[_degreeOfParallelism + 1];

                for (int i = 1; i <= _degreeOfParallelism; i++)
                    _workSplitBiasTable[i] = Math.Pow(i, WORK_SPLIT_BIAS_FACTOR);

                _workSplitBiasTable[0] = 1; // Prevent division by zero, fallback.
            }

            void StartWorkers()
            {
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
            }

            void StartWatchdog()
            {
                _watchdog = new(WatchdogLoop)
                {
                    Name = $"{GameLoop.THREAD_NAME}_Watchdog",
                    Priority = ThreadPriority.Lowest,
                    IsBackground = true
                };
                _watchdog.Start();
            }
        }

        public override void ExecuteWork(int count, Action<int> workAction)
        {
            try
            {
                if (count <= 0)
                    return;

                _workAction = workAction;
                _workLeft = count;
                _workerCompletionCount = 0;

                // If the count is less than the degree of parallelism, only signal the required number of workers.
                // The caller thread will also be used, so in this case we need to subtract one from the amount of workers to start.
                int workersToStart = count < _degreeOfParallelism ? count - 1 : _workerCount;

                for (int i = 0; i < workersToStart; i++)
                    _workReady[i].Set();

                _workerRoutine();
                Interlocked.Increment(ref _workerCompletionCount);

                // Spin very tightly until all the workers have completed their work.
                // We could adjust the spin wait time if we get here early, but this is hard to predict.
                // However we really don't want to yield the CPU here, as this could delay the return by a lot.
                while (Volatile.Read(ref _workerCompletionCount) < workersToStart + 1)
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
            _workerRoutine = static () => _tickLocalPools.Reset();
            ExecuteWork(_degreeOfParallelism, null);
            _workerRoutine = ProcessWorkActions;
        }

        public override void Dispose()
        {
            if (!Interlocked.CompareExchange(ref _running, false, true))
                return;

            if (_watchdog.IsAlive)
                _watchdog.Join();

            _workerStartLatch.Wait(); // Make sure any worker being (re)started has finished.
            _shutdownToken.Cancel();

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
            _workReady[Id] = new ManualResetEventSlim();
            base.InitWorker(obj);
            _workerStartLatch.Signal();

            // If this is a restart, we need to free the caller thread.
            if (Restart)
                Interlocked.Increment(ref _workerCompletionCount);

            RunWorkerLoop(_workReady[Id], _shutdownToken.Token);
        }

        private void RunWorkerLoop(ManualResetEventSlim workReady, CancellationToken cancellationToken)
        {
            while (Volatile.Read(ref _running))
            {
                try
                {
                    workReady.Wait(cancellationToken);
                    workReady.Reset();
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

                _workerRoutine();
                 Interlocked.Increment(ref _workerCompletionCount); // Not in a finally block on purpose.
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }

        private void ProcessWorkActions()
        {
            try
            {
                int remainingWork = Volatile.Read(ref _workLeft);

                while (remainingWork > 0)
                {
                    int workersRemaining = _degreeOfParallelism - Volatile.Read(ref _workerCompletionCount);
                    int chunkSize = (int) (remainingWork / _workSplitBiasTable[workersRemaining]);

                    // Prevent infinite loops.
                    if (chunkSize < 1)
                        chunkSize = 1;

                    int start = Interlocked.Add(ref _workLeft, -chunkSize);
                    int end = start + chunkSize;

                    if (end < 1)
                        break;

                    if (start < 0)
                        start = 0;

                    for (int i = start; i < end; i++)
                        _workAction(i);

                    remainingWork = start - 1;
                }
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

        public override void ExecuteWork(int count, Action<int> workAction)
        {
            for (int i = 0; i < count; i++)
                workAction(i);
        }

        public override void PrepareForNextTick()
        {
            _tickLocalPools.Reset();
        }

        public override void Dispose() { }
    }

    public abstract class GameLoopThreadPool : IDisposable
    {
        [ThreadStatic]
        protected static TickLocalPools _tickLocalPools;

        public virtual void Init()
        {
            _tickLocalPools = new();
        }

        public abstract void ExecuteWork(int count, Action<int> workAction);

        public abstract void PrepareForNextTick();

        public abstract void Dispose();

        public T GetForTick<T>(PooledObjectKey key, Action<T> initializer) where T : IPooledObject<T>, new()
        {
            T result = _tickLocalPools != null ? _tickLocalPools.GetForTick<T>(key) : new();
            initializer?.Invoke(result);
            return result;
        }

        protected virtual void InitWorker(object obj)
        {
            _tickLocalPools = new();
        }

        protected sealed class TickLocalPools
        {
            private Dictionary<PooledObjectKey, ITickObjectPool> _localPools = new()
            {
                { PooledObjectKey.InPacket, new TickObjectPool<GSPacketIn>() },
                { PooledObjectKey.TcpOutPacket, new TickObjectPool<GSTCPPacketOut>() },
                { PooledObjectKey.UdpOutPacket, new TickObjectPool<GSUDPPacketOut>() }
            };

            public T GetForTick<T>(PooledObjectKey key) where T : IPooledObject<T>, new()
            {
                return (_localPools[key] as TickObjectPool<T>).GetForTick();
            }

            public void Reset()
            {
                foreach (var pair in _localPools)
                    pair.Value.Reset();
            }

            private sealed class TickObjectPool<T> : ITickObjectPool where T : IPooledObject<T>, new()
            {
                private T[] _items = new T[64];
                private int _used;
                private int _highWaterIndex;

                public T GetForTick()
                {
                    T item;

                    if (_used < _highWaterIndex && _items[_used] != null)
                    {
                        item = _items[_used];
                        _items[_used] = default;
                        _used++;
                        item.IssuedTimestamp = GameLoop.GameLoopTime;
                    }
                    else
                    {
                        item = new();
                        item.IssuedTimestamp = GameLoop.GameLoopTime;

                        if (_used >= _items.Length)
                            Array.Resize(ref _items, _items.Length * 2);

                        _items[_used++] = item;
                        _highWaterIndex = Math.Max(_highWaterIndex, _used);
                    }

                    return item;
                }

                public void Reset()
                {
                    // Gradual shrinking by nulling from the end.
                    if (_highWaterIndex > _used * 2 + 100)
                    {
                        int shrinkEnd = Math.Max(_used * 2, _highWaterIndex - 10);

                        for (int i = shrinkEnd; i < _highWaterIndex; i++)
                            _items[i] = default;

                        _highWaterIndex = shrinkEnd;
                    }

                    _used = 0;
                }
            }

            private interface ITickObjectPool
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
        static abstract T GetForTick(Action<T> initializer);

        // The game loop tick timestamp when this object was issued.
        // Will be 0 if created outside the game loop (e.g., by a .NET worker thread without local object pools).
        long IssuedTimestamp { get; set; }
    }

    public static class PooledObjectExtensions
    {
        public static bool IsValidForTick<T>(this IPooledObject<T> obj)
        {
            return obj.IssuedTimestamp == GameLoop.GameLoopTime;
        }
    }
}
