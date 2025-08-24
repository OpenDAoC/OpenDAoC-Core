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
        private readonly ConcurrentBag<PostedAction> _actionPool = new();
        private readonly ConcurrentQueue<PostedAction> _actions = new();
        private readonly List<PostedAction> _work = new();
        private bool _hasActions;

        public string ServiceName { get; }

        protected GameServiceBase()
        {
            ServiceName = GetType().Name;
        }

        public void Post<TState>(Action<TState> action, TState state)
        {
            // Posting across services is allowed, but can deadlock if the caller blocks on a Task.
            // Example: a Task continuation is posted to a different service while the original
            // service waits for it to complete. Since the target service cannot process posted
            // actions until it ticks, neither side can make progress.

            if (!_actionPool.TryTake(out PostedAction pooledAction))
                pooledAction = new PostedAction();

            pooledAction.Init(this, action, state, Invoker<TState>.Invoke);
            _actions.Enqueue(pooledAction);
            Volatile.Write(ref _hasActions, true);
        }

        public void ProcessPostedActions()
        {
            if (!Interlocked.Exchange(ref _hasActions, false))
                return;

            while (_actions.TryDequeue(out PostedAction action))
                ProcessPostedActionInternal(action);
        }

        protected void ProcessPostedActionsParallel()
        {
            if (!Interlocked.Exchange(ref _hasActions, false))
                return;

            while (_actions.TryDequeue(out PostedAction action))
                _work.Add(action);

            if (_work.Count <= 0)
                return;

            GameLoop.ExecuteForEach(_work, _work.Count, ProcessPostedActionInternal);
            _work.Clear();
        }

        private static void ProcessPostedActionInternal(PostedAction action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Error executing posted action in {action.Service.ServiceName}", e);
            }
            finally
            {
                GameServiceBase service = action.Service;
                action.Reset();
                service._actionPool.Add(action);
            }
        }

        public virtual void BeginTick() { }
        public virtual void Tick() { }
        public virtual void EndTick() { }

        private static class Invoker<T>
        {
            public static readonly Action<object, object> Invoke = static (action, state) => ((Action<T>) action)((T) state);
        }

        private sealed class PostedAction
        {
            private object _action;
            private object _state;
            private Action<object, object> _invoker;

            public GameServiceBase Service { get; private set; }

            public void Init<TState>(GameServiceBase service, Action<TState> action, TState state, Action<object, object> invoker)
            {
                Service = service;
                _action = action;
                _state = state;
                _invoker = invoker;
            }

            public void Invoke()
            {
                _invoker(_action, _state);
            }

            public void Reset()
            {
                Service = null;
                _action = null;
                _state = null;
                _invoker = null;
            }
        }
    }

    public interface IGameService
    {
        string ServiceName { get; }
        void Post<TState>(Action<TState> action, TState state);
        void ProcessPostedActions();

        void BeginTick() { }
        void Tick() { }
        void EndTick() { }
    }
}
