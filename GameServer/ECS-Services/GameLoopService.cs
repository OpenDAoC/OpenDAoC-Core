using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public static class GameLoopService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(GameLoopService);
        private const string SERVICE_NAME_BEGIN = $"{SERVICE_NAME}_Begin";
        private const string SERVICE_NAME_END = $"{SERVICE_NAME}_End";

        // Queues for actions that are posted to be executed before and after the game loop tick.
        private static readonly ConcurrentQueue<IPostedAction> _preTickActions = new();
        private static readonly ConcurrentQueue<IPostedAction> _postTickActions = new();

        // Used to avoid having to call `TryPeek` or `IsEmpty` on the concurrent queues, which can be particularly slow under contention.
        private static bool _hasPreTickActions;
        private static bool _hasPostTickActions;

        // Lists to drain the concurrent queues into, to avoid contention during execution.
        private static readonly List<IPostedAction> _preTickWork = new();
        private static readonly List<IPostedAction> _postTickWork = new();

        public static void BeginTick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME_BEGIN;
            Diagnostics.StartPerfCounter(SERVICE_NAME_BEGIN);

            if (Interlocked.Exchange(ref _hasPreTickActions, false))
            {
                while (_preTickActions.TryDequeue(out IPostedAction action))
                    _preTickWork.Add(action);

                GameLoop.ExecuteWork(_preTickWork.Count, static i =>
                {
                    try
                    {
                        _preTickWork[i].Invoke();
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error($"Critical error encountered in {SERVICE_NAME_BEGIN}: {e}");
                    }
                });

                _preTickWork.Clear();
            }

            GameLoop.PrepareForNextTick();
            Diagnostics.StopPerfCounter(SERVICE_NAME_BEGIN);
        }

        public static void EndTick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME_END;
            Diagnostics.StartPerfCounter(SERVICE_NAME_END);

            if (Interlocked.Exchange(ref _hasPostTickActions, false))
            {
                while (_postTickActions.TryDequeue(out IPostedAction action))
                    _postTickWork.Add(action);

                GameLoop.ExecuteWork(_postTickWork.Count, static i =>
                {
                    try
                    {
                        _postTickWork[i].Invoke();
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error($"Critical error encountered in {SERVICE_NAME_END}: {e}");
                    }
                });

                _postTickWork.Clear();
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME_END);
        }

        public static void PostBeforeTick<TState>(Action<TState> action, TState state)
        {
            _preTickActions.Enqueue(new PostedAction<TState>(action, state));
            Volatile.Write(ref _hasPreTickActions, true);
        }

        public static void PostAfterTick<TState>(Action<TState> action, TState state)
        {
            _postTickActions.Enqueue(new PostedAction<TState>(action, state));
            Volatile.Write(ref _hasPostTickActions, true);
        }

        private readonly struct PostedAction<T> : IPostedAction
        {
            public readonly Action<T> Action;
            public readonly T State;

            public PostedAction(Action<T> action, T state)
            {
                Action = action;
                State = state;
            }

            public void Invoke()
            {
                Action(State);
            }
        }

        private interface IPostedAction
        {
            void Invoke();
        }
    }
}
