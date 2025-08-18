using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public abstract class GameLoopThreadPool : IDisposable
    {
        private static readonly GameLoopSynchronizationContext _context = new();
        [ThreadStatic] private static GameLoopTickObjectPool _tickObjectPool;
        [ThreadStatic] private static long _lastResetTick;

        public virtual void Init()
        {
            InitThreadStatics();
        }

        public abstract void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action);

        public abstract void Dispose();

        public T GetForTick<T>(PooledObjectKey key, Action<T> initializer) where T : IPooledObject<T>, new()
        {
            T result = _tickObjectPool != null ? _tickObjectPool.GetForTick<T>(key) : new();
            initializer?.Invoke(result);
            return result;
        }

        protected virtual void InitWorker(object obj)
        {
            InitThreadStatics();
        }

        private void InitThreadStatics()
        {
            SynchronizationContext.SetSynchronizationContext(_context);
            _tickObjectPool = new();
            _lastResetTick = -1;
        }

        protected void CheckResetTick()
        {
            if (_lastResetTick == GameLoop.GameLoopTime)
                return;

            _tickObjectPool.Reset();
            _lastResetTick = GameLoop.GameLoopTime;
        }
    }
}
