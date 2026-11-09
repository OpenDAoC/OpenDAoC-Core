using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public readonly record struct TickExecutorInitParams(Action<Exception> OnFatalError, Func<long> TickSource);

    public abstract class TickExecutor : ITickExecutor, IDisposable
    {
        [ThreadStatic] private static TickObjectPoolManager _tickObjectPoolManager;
        [ThreadStatic] private static TickListPoolManager _tickListPoolManager;
        [ThreadStatic] private static long _lastResetTick;

        protected readonly Action<Exception> _onFatalError;
        private readonly Func<long> _tickSource;

        protected bool _running;

        protected TickExecutor(TickExecutorInitParams initParams)
        {
            ArgumentNullException.ThrowIfNull(initParams.OnFatalError);
            ArgumentNullException.ThrowIfNull(initParams.TickSource);

            _onFatalError = initParams.OnFatalError;
            _tickSource = initParams.TickSource;
        }

        public virtual bool Start()
        {
            // Must be called from the thread that will dispatch work.

            if (Interlocked.CompareExchange(ref _running, true, false))
                return false;

            InitThreadStatics();
            return true;
        }

        public abstract void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action);
        public abstract void ExecuteForEachSharded<T>(List<T>[] shards, int[] shardStartIndices, int totalCount, Action<T> action);

        public void Dispose()
        {
            bool disposing = Interlocked.Exchange(ref _running, false);
            Dispose(disposing);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        public T GetObjectForTick<T>() where T : ITickPooledObject<T>, new()
        {
            return _tickObjectPoolManager != null ? _tickObjectPoolManager.GetForTick<T>() : new();
        }

        public List<T> GetListForTick<T>() where T : ITickPooledList<T>
        {
            return _tickListPoolManager != null ? _tickListPoolManager.GetForTick<T>() : new();
        }

        protected virtual void InitWorker(object obj)
        {
            InitThreadStatics();
        }

        private void InitThreadStatics()
        {
            _tickObjectPoolManager = new();
            _tickListPoolManager = new();
            _lastResetTick = -1;
        }

        protected void CheckResetTick()
        {
            long tick = _tickSource();

            if (_lastResetTick == tick)
                return;

            _tickObjectPoolManager.Reset();
            _tickListPoolManager.Reset();
            _lastResetTick = tick;
        }
    }

    public interface ITickExecutor
    {
        void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action);
        void ExecuteForEachSharded<T>(List<T>[] shards, int[] shardStartIndices, int totalCount, Action<T> action);
    }
}
