using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class ParallelTickExecutor : TickExecutor
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // Shapes an inverse power-law curve for work distribution.
        // Higher values -> smaller, more numerous chunks, better for uneven workloads.
        // Lower values -> larger chunks, better for uniform workloads.
        // 2.5 is a good balance found through empirical testing.
        private const double WORK_SPLIT_BIAS_FACTOR = 2.5;
        private const int MAX_DEGREE_OF_PARALLELISM = 128;

        // Thread pool configuration and state.
        private readonly int _degreeOfParallelism;      // Total threads, including caller.
        private readonly int _workerCount;              // Number of dedicated worker threads (= _degreeOfParallelism - 1).
        private readonly string _threadNamePrefix;
        private readonly double[] _workSplitBiasTable;  // Lookup table for chunk size biasing.

        // Thread management.
        private readonly Thread[] _workers;             // Worker threads (excludes caller).
        private readonly long[] _workerCycle;           // Worker cycle phase, used to detect stuck threads.
        private TickWorkerWatchdog _watchdog;           // Monitors worker health; restarts if needed.
        private readonly CancellationTokenSource _shutdownToken;

        // Work coordination.
        private CountdownEvent _workerStartLatch;       // Signals when all workers are initialized.
        private readonly SemaphoreSlim  _workReady;     // Signals when work is available for workers to process.
        private readonly ConcurrentQueue<(int Start, int End)> _orphanedRanges = new();
        private uint _orphanedCount;

        // Work processing.
        private WorkProcessor _workProcessor;           // Current work processor instance, reused for each execution.
        private readonly WorkState _workState = new();  // Shared state for work distribution and completion tracking.
        private SynchronizationContext _callerContext;  // Context of the caller thread.

        [StructLayout(LayoutKind.Explicit)]
        private class WorkState
        {
            [FieldOffset(0)]   public int RemainingWork;         // Total items left to process.
            [FieldOffset(128)] public int CompletedWorkerCount;  // Count of workers finished for current iteration.
        }

        public ParallelTickExecutor(int degreeOfParallelism, string threadNamePrefix, TickExecutorInitParams initParams)
            : base(initParams)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(degreeOfParallelism, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(degreeOfParallelism, MAX_DEGREE_OF_PARALLELISM);

            _degreeOfParallelism = degreeOfParallelism;
            _workerCount = _degreeOfParallelism - 1;
            _threadNamePrefix = threadNamePrefix ?? nameof(ParallelTickExecutor);

            _workers = new Thread[_workerCount];
            _workerCycle = new long[_workerCount];
            _workerStartLatch = new(_workerCount);
            _workReady = new(0, _workerCount);
            _shutdownToken = new();
            _workSplitBiasTable = BuildChunkDivisorTable(_degreeOfParallelism);
        }

        public override bool Start()
        {
            if (!base.Start())
                return false;

            StartWorkers();
            StartWatchdog();
            return true;
        }

        private static double[] BuildChunkDivisorTable(int degreeOfParallelism)
        {
            double[] workSplitBiasTable = new double[degreeOfParallelism + 1];

            for (int i = 1; i <= degreeOfParallelism; i++)
                workSplitBiasTable[i] = Math.Pow(i, WORK_SPLIT_BIAS_FACTOR);

            workSplitBiasTable[0] = 1; // Prevent division by zero, fallback.
            return workSplitBiasTable;
        }

        private void CreateWorker(int id, bool isRestart)
        {
            Thread worker = new(new ParameterizedThreadStart(InitWorker))
            {
                Name = $"{_threadNamePrefix}_Worker_{id}",
                IsBackground = true
            };
            worker.Start((id, isRestart));
        }

        private void StartWorkers()
        {
            for (int i = 0; i < _workerCount; i++)
                CreateWorker(i, false);

            _workerStartLatch.Wait(); // If for some reason a thread fails to start, we'll be waiting here forever.
        }

        private void StartWatchdog()
        {
            _watchdog = new(_workers, _workerCycle, RestartWorkers);
            _watchdog.Start();
        }

        public override void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action)
        {
            try
            {
                int count = Math.Min(items.Count, toExclusive);

                if (count <= 0)
                    return;

                _workProcessor = WorkProcessorCache<T>.Instance.Set(items, action);
                ExecuteForEachInternal(count);
            }
            catch (Exception e)
            {
                HandleFatal(e);
            }
            finally
            {
                _workProcessor?.Clear();
            }
        }

        public override void ExecuteForEachSharded<T>(List<T>[] shards, int[] shardStartIndices, int totalCount, Action<T> action)
        {
            try
            {
                if (totalCount <= 0)
                    return;

                _workProcessor = ShardedWorkProcessorCache<T>.Instance.Set(shards, shardStartIndices, totalCount, action);
                ExecuteForEachInternal(totalCount);
            }
            catch (Exception e)
            {
                HandleFatal(e);
            }
            finally
            {
                _workProcessor?.Clear();
            }
        }

        private void ExecuteForEachInternal(int count)
        {
            _callerContext = SynchronizationContext.Current;
            _workState.RemainingWork = count;
            _workState.CompletedWorkerCount = 0;

            // If the count is less than the degree of parallelism, only signal the required number of workers.
            // The caller thread will also be used, so in this case we need to subtract one from the amount of workers to start.
            int workersToStart = count < _degreeOfParallelism ? count - 1 : _workerCount;

            if (workersToStart > 0)
                _workReady.Release(workersToStart);

            ProcessWorkActions();
            Interlocked.Increment(ref _workState.CompletedWorkerCount);

            // Spin very tightly until all the workers have completed their work.
            // We could adjust the spin wait time if we get here early, but this is hard to predict.
            // However we really don't want to yield the CPU here, as this could delay the return by a lot.
            while (Volatile.Read(ref _workState.CompletedWorkerCount) < workersToStart + 1)
                Thread.SpinWait(1);

            // If any workers were interrupted while processing, we need to process the orphaned ranges here on the caller thread.
            if (Volatile.Read(ref _orphanedCount) > 0)
                DrainOrphanedRanges();
        }

        private void DrainOrphanedRanges()
        {
            try
            {
                while (_orphanedRanges.TryDequeue(out var range))
                {
                    int inFlight = range.Start - 1;
                    _workProcessor.Process(range.Start, range.End, ref inFlight);
                }
            }
            catch
            {
                _orphanedRanges.Clear();
                throw;
            }
            finally
            {
                Volatile.Write(ref _orphanedCount, 0);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _watchdog.Stop();
            _workerStartLatch.Wait(); // Make sure any worker being (re)started has finished.
            _shutdownToken.Cancel();

            for (int i = 0; i < _workers.Length; i++)
            {
                Thread worker = _workers[i];

                if (worker != null && Thread.CurrentThread != worker && worker.IsAlive)
                    worker.Join();
            }

            _workReady.Dispose();
        }

        protected override void InitWorker(object obj)
        {
            (int Id, _) = ((int, bool)) obj;
            _workers[Id] = Thread.CurrentThread;
            _workerCycle[Id] = TickWorkerWatchdog.IDLE_CYCLE;
            base.InitWorker(obj);
            _workerStartLatch.Signal();
            RunWorkerLoop(Id, _shutdownToken.Token);
        }

        private void RestartWorkers(List<int> _workersToRestart)
        {
            _workerStartLatch = new(_workersToRestart.Count);

            foreach (int id in _workersToRestart)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Restarting thread \"{_workers[id].Name}\"");

                _workers[id] = null;
                CreateWorker(id, true);
            }
        }

        private void RunWorkerLoop(int id, CancellationToken cancellationToken)
        {
            ref long workerCycle = ref _workerCycle[id];
            long cycle = TickWorkerWatchdog.IDLE_CYCLE;

            while (Volatile.Read(ref _running))
            {
                bool acquiredWork = false;

                try
                {
                    _workReady.Wait(cancellationToken);
                    acquiredWork = true;
                    workerCycle = ++cycle;
                    ProcessWorkActions();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Thread \"{Thread.CurrentThread.Name}\" was interrupted");

                    break;
                }
                catch (Exception e)
                {
                    HandleFatal(e);
                    break;
                }
                finally
                {
                    workerCycle = TickWorkerWatchdog.IDLE_CYCLE;

                    if (acquiredWork)
                        Interlocked.Increment(ref _workState.CompletedWorkerCount);
                }
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }

        private void ProcessWorkActions()
        {
            SynchronizationContext previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(_callerContext);

            try
            {
                CheckResetTick();
                int remainingWork = Volatile.Read(ref _workState.RemainingWork);

                while (remainingWork > 0)
                {
                    int workersRemaining = _degreeOfParallelism - Volatile.Read(ref _workState.CompletedWorkerCount);
                    int chunkSize = (int) (remainingWork / _workSplitBiasTable[workersRemaining]);

                    if (chunkSize < 1)
                        chunkSize = 1;

                    int start = Interlocked.Add(ref _workState.RemainingWork, -chunkSize);
                    int end = start + chunkSize;

                    if (end < 1)
                        break;

                    if (start < 0)
                        start = 0;

                    int inFlight = start - 1;

                    try
                    {
                        _workProcessor.Process(start, end, ref inFlight);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        // Skip the in-flight item, orphan everything after it.
                        if (inFlight + 1 < end)
                        {
                            _orphanedRanges.Enqueue((inFlight + 1, end));
                            Interlocked.Increment(ref _orphanedCount);
                        }

                        if (log.IsWarnEnabled)
                            log.Warn($"Interrupted while processing item {inFlight} of [{start}, {end})", e);

                        throw;
                    }

                    remainingWork = start - 1;
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        private void HandleFatal(Exception e)
        {
            if (log.IsFatalEnabled)
                log.Fatal($"Critical error encountered in \"{nameof(ParallelTickExecutor)}\"", e);

            _onFatalError(e);
        }

        private static class WorkProcessorCache<T>
        {
            public static readonly WorkProcessor<T> Instance = new();
        }

        private static class ShardedWorkProcessorCache<T>
        {
            public static readonly ShardedWorkProcessor<T> Instance = new();
        }

        private sealed class WorkProcessor<T> : WorkProcessor
        {
            private List<T> _items;
            private Action<T> _action;

            public WorkProcessor() { }

            public WorkProcessor<T> Set(List<T> items, Action<T> action)
            {
                _items = items;
                _action = action;
                return this;
            }

            public override void Process(int start, int end, ref int inFlight)
            {
                int current = start;

                try
                {
                    for (; current < end; current++)
                        _action(_items[current]);
                }
                catch
                {
                    inFlight = current;
                    throw;
                }
            }

            public override void Clear()
            {
                _items = null;
                _action = null;
            }
        }

        private sealed class ShardedWorkProcessor<T> : WorkProcessor
        {
            private List<T>[] _shards;
            private int[] _shardStartIndices;
            private int _totalCount;
            private Action<T> _action;

            public ShardedWorkProcessor<T> Set(
                List<T>[] shards,
                int[] shardStartIndices,
                int totalCount,
                Action<T> action)
            {
                _shards = shards;
                _shardStartIndices = shardStartIndices;
                _totalCount = totalCount;
                _action = action;
                return this;
            }

            public override void Process(int start, int end, ref int inFlight)
            {
                int current = start;

                try
                {
                    int currentShardIdx = _shardStartIndices.Length - 1;

                    while (currentShardIdx > 0 && start < _shardStartIndices[currentShardIdx])
                        currentShardIdx--;

                    int itemsLeft = end - start;
                    int localIdx = start - _shardStartIndices[currentShardIdx];

                    while (itemsLeft > 0)
                    {
                        // Derive the exact number of valid items in this shard using the boundaries.
                        int shardValidCount;

                        if (currentShardIdx < _shardStartIndices.Length - 1)
                            shardValidCount =  _shardStartIndices[currentShardIdx + 1] - _shardStartIndices[currentShardIdx];
                        else
                            shardValidCount =  _totalCount - _shardStartIndices[currentShardIdx];

                        int itemsToProcess = Math.Min(itemsLeft, shardValidCount - localIdx);

                        if (itemsToProcess <= 0)
                        {
                            currentShardIdx++;
                            localIdx = 0;
                            continue;
                        }

                        List<T> currentShard = _shards[currentShardIdx];

                        for (int i = 0; i < itemsToProcess; i++)
                        {
                            _action(currentShard[localIdx + i]);
                            current++;
                        }

                        itemsLeft -= itemsToProcess;
                        currentShardIdx++;
                        localIdx = 0;
                    }
                }
                catch
                {
                    inFlight = current;
                    throw;
                }
            }

            public override void Clear()
            {
                _shards = null;
                _shardStartIndices = null;
                _action = null;
            }
        }

        private abstract class WorkProcessor
        {
            public abstract void Process(int start, int end, ref int inFlight);
            public abstract void Clear();
        }
    }
}
