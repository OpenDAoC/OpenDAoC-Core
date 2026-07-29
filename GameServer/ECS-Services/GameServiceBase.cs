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

        private List<PostedAction> _actions = new();
        [ThreadStatic] private static Stack<List<PostedAction>> _spareLists;
        private readonly Lock _lock = new();

        public int EntityCount; // Used for diagnostics.
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

            if (!ActionPool<TState>.Pool.TryTake(out var pooledAction))
                pooledAction = new();

            pooledAction.Init(this, action, state);

            lock (_lock)
                _actions.Add(pooledAction);
        }

        public void ProcessPostedActions()
        {
            List<PostedAction> batch = TakeBatch();

            if (batch == null)
                return;

            try
            {
                foreach (PostedAction action in batch)
                    ProcessPostedActionInternal(action);
            }
            finally
            {
                ReturnList(batch);
            }
        }

        protected void ProcessPostedActionsParallel()
        {
            List<PostedAction> batch = TakeBatch();

            if (batch == null)
                return;

            try
            {
                GameLoop.ExecuteForEach(batch, batch.Count, ProcessPostedActionInternal);
            }
            finally
            {
                ReturnList(batch);
            }
        }

        private List<PostedAction> TakeBatch()
        {
            lock (_lock)
            {
                if (_actions.Count == 0)
                    return null;

                List<PostedAction> batch = _actions;
                _actions = RentList();
                return batch;
            }
        }

        private static List<PostedAction> RentList()
        {
            var stack = _spareLists ??= new();
            return stack.Count > 0 ? stack.Pop() : new();
        }

        private static void ReturnList(List<PostedAction> list)
        {
            list.Clear();
            (_spareLists ??= new()).Push(list);
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
                action.ReturnToPool();
            }
        }

        public virtual void BeginTick() { }
        public virtual void Tick() { }
        public virtual void EndTick() { }

        private static class ActionPool<TState>
        {
            public static ConcurrentBag<PostedAction<TState>> Pool { get; } = new();
        }

        private abstract class PostedAction
        {
            public GameServiceBase Service { get; protected set; }
            public abstract void Invoke();
            public abstract void ReturnToPool();
        }

        private sealed class PostedAction<TState> : PostedAction
        {
            private Action<TState> _action;
            private TState _state;

            public void Init(GameServiceBase service, Action<TState> action, TState state)
            {
                Service = service;
                _action = action;
                _state = state;
            }

            public override void Invoke()
            {
                _action(_state);
            }

            public override void ReturnToPool()
            {
                Service = null;
                _action = null;
                _state = default;
                ActionPool<TState>.Pool.Add(this);
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
