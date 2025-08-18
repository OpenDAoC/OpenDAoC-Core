using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;

namespace DOL.GS
{
    public abstract class GameServiceBase : IGameService
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public int EntityCount; // Used for diagnostics.
        private readonly ConcurrentQueue<IPostedAction> _actions = new();
        private readonly List<IPostedAction> _work = new();
        private bool _hasActions;

        public static GameServiceBase Instance { get; private set; }
        public string ServiceName { get; }

        protected GameServiceBase()
        {
            Instance = this;
            ServiceName = GetType().Name;
        }

        public void Post<TState>(Action<TState> action, TState state)
        {
            _actions.Enqueue(new PostedAction<TState>(action, state));
            Volatile.Write(ref _hasActions, true);
        }

        protected void ProcessPostedActions()
        {
            if (!Interlocked.Exchange(ref _hasActions, false))
                return;

            while (_actions.TryDequeue(out IPostedAction action))
                _work.Add(action);

            if (_work.Count <= 0)
                return;

            GameLoop.ExecuteForEach(_work, _work.Count, ProcessPostedActionInternal);
            _work.Clear();
        }

        private static void ProcessPostedActionInternal(IPostedAction action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Error executing posted action in {Instance.ServiceName}", e);
            }
        }

        public virtual void BeginTick() { }
        public virtual void Tick() { }
        public virtual void EndTick() { }

        private readonly struct PostedAction<T> : IPostedAction
        {
            private readonly Action<T> _action;
            private readonly T _state;

            public PostedAction(Action<T> action, T state)
            {
                _action = action;
                _state = state;
            }

            public void Invoke()
            {
                _action(_state);
            }
        }

        private interface IPostedAction
        {
            void Invoke();
        }
    }

    public interface IGameService
    {
        string ServiceName { get; }
        void Post<TState>(Action<TState> action, TState state);

        void BeginTick() { }
        void Tick() { }
        void EndTick() { }
    }
}
