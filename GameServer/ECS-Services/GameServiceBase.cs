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

        private readonly ConcurrentQueue<IPostedAction> _actions = new();
        private readonly List<IPostedAction> _work = new();
        private bool _hasActions;

        public string ServiceName { get; }

        protected GameServiceBase()
        {
            ServiceName = GetType().Name;
        }

        public void Post<TState>(Action<TState> action, TState state)
        {
            _actions.Enqueue(new PostedAction<TState>(action, state));
            Volatile.Write(ref _hasActions, true);
        }

        protected void ProcessPostedActions()
        {
            if (!Volatile.Read(ref _hasActions))
                return;

            // Use Interlocked to be safe, though only the game loop thread should call this
            if (Interlocked.Exchange(ref _hasActions, false))
            {
                while (_actions.TryDequeue(out IPostedAction action))
                    _work.Add(action);

                if (_work.Count > 0)
                {
                    GameLoop.ExecuteWork(_work.Count, i =>
                    {
                        try
                        {
                            _work[i].Invoke();
                        }
                        catch (Exception e)
                        {
                            if (log.IsErrorEnabled)
                                log.Error($"Error executing posted action in {ServiceName}", e);
                        }
                    });

                    _work.Clear();
                }
            }
        }

        public virtual void BeginTick() { }
        public virtual void Tick() { }
        public virtual void EndTick() { }

        private interface IPostedAction
        {
            void Invoke();
        }

        private readonly struct PostedAction<T> : IPostedAction
        {
            private readonly Action<T> _action;
            private readonly T _state;

            public PostedAction(Action<T> action, T state)
            {
                _action = action;
                _state = state;
            }

            public void Invoke() => _action(_state);
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
