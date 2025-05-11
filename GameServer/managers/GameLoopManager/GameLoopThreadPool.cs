using System;
using System.Reflection;
using System.Threading;

namespace DOL.GS
{
    // A very specialized and barebone thread pool, meant to be used by the game loop exclusively.
    // Changing thread amount, restarting threads, scheduling async work, and so on isn't supported.
    // The reasons why we're using this thread pool despite being way less robust than TPL:
    // * The memory overhead of `Parallel.For` or `Parallel.ForEach` isn't negligible when it's called 300 times per second.
    // * A lot easier to debug.
    public class GameLoopThreadPool : IGameLoopThreadPool
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly int _threadCount;
        private readonly Thread[] _threads;
        private readonly Barrier _barrier;
        private readonly ManualResetEventSlim _workReady = new();
        private readonly ManualResetEventSlim _workDone = new();
        private int _workCount;
        private Action<int> _action;
        private int _chunk;
        private int _remainder;
        private int _workDoneCount;

        public GameLoopThreadPool(int threadCount)
        {
            _threadCount = threadCount;
            _threads = new Thread[threadCount];
            _barrier = new(_threadCount);

            for (int i = 0; i < threadCount; i++)
            {
                _threads[i] = new Thread(new ParameterizedThreadStart(WorkerLoop))
                {
                    IsBackground = true,
                    Name = $"GameLoopWorkerThread-{i}"
                };
                _threads[i].Start(i);
            }
        }

        public void Run(int count, Action<int> action)
        {
            if (count < 0)
                return;

            _workCount = count;
            _action = action;
            _chunk = _workCount / _threadCount;
            _remainder = _workCount % _threadCount;
            _workReady.Set();
            _workDone.Wait();
            _workDone.Reset();
        }

        private void WorkerLoop(object obj)
        {
            int threadIndex = (int) obj;
            int start;
            int end;

            while (true)
            {
                try
                {
                    _workReady.Wait();
                    start = threadIndex * _chunk + Math.Min(threadIndex, _remainder);
                    end = start + _chunk;

                    if (threadIndex < _remainder)
                        end++;

                    for (int i = start; i < end; i++)
                        _action(i);
                }
                catch (ThreadInterruptedException)
                {
                    log.Info($"\"{Thread.CurrentThread.Name}\" was interrupted");
                    return;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"\"{Thread.CurrentThread.Name}\" exception: {e.Message}", e);
                }
                finally
                {
                    if (Interlocked.Increment(ref _workDoneCount) == _threadCount)
                    {
                        _workReady.Reset();
                        _workDone.Set();
                        _workDoneCount = 0;
                        _barrier.SignalAndWait();
                    }
                    else
                        _barrier.SignalAndWait();
                }
            }
        }
    }

    public class GameLoopThreadPoolSingleThreaded : IGameLoopThreadPool
    {
        public GameLoopThreadPoolSingleThreaded() { }

        public void Run(int count, Action<int> action)
        {
            for (int i = 0; i < count; i++)
                action(i);
        }
    }

    public interface IGameLoopThreadPool
    {
        public void Run(int count, Action<int> action);
    }
}
