using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public abstract class GameLoopThreadPool : IDisposable
    {
        public static GameLoopSynchronizationContext Context { get; } = new();
        [ThreadStatic] private static TickObjectPoolManager _tickObjectPoolManager;
        [ThreadStatic] private static TickListPoolManager _tickListPoolManager;
        [ThreadStatic] private static long _lastResetTick;

        public virtual void Init()
        {
            InitThreadStatics();
        }

        public abstract void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action);

        public abstract void Dispose();

        public T GetObjectForTick<T>() where T : IPooledObject<T>, new()
        {
            return _tickObjectPoolManager != null ? _tickObjectPoolManager.GetForTick<T>() : new();
        }

        public List<T> GetListForTick<T>() where T : IPooledList<T>
        {
            return _tickListPoolManager != null ? _tickListPoolManager.GetForTick<T>() : new();
        }

        protected virtual void InitWorker(object obj)
        {
            InitThreadStatics();
        }

        private void InitThreadStatics()
        {
            SynchronizationContext.SetSynchronizationContext(Context);
            _tickObjectPoolManager = new();
            _tickListPoolManager = new();
            _lastResetTick = -1;
        }

        protected void CheckResetTick()
        {
            if (_lastResetTick == GameLoop.GameLoopTime)
                return;

            _tickObjectPoolManager.Reset();
            _tickListPoolManager.Reset();
            _lastResetTick = GameLoop.GameLoopTime;
        }
    }
}
