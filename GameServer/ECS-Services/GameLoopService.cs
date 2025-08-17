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

        // Queue for actions that are posted to be executed before and after the game loop tick.
        private static readonly ConcurrentQueue<IPostedAction> _actions = new();

        // Used to avoid having to call `TryPeek` or `IsEmpty` on the concurrent queue, which can be particularly slow under contention.
        private static bool _hasActions;

        // List to drain the concurrent queue into, to avoid contention during execution.
        private static readonly List<IPostedAction> _work = new();

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            if (Interlocked.Exchange(ref _hasActions, false))
            {
                while (_actions.TryDequeue(out IPostedAction action))
                    _work.Add(action);

                GameLoop.ExecuteWork(_work.Count, static i =>
                {
                    try
                    {
                        _work[i].Invoke();
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error($"Critical error encountered in {SERVICE_NAME}: {e}");
                    }
                });

                _work.Clear();
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        public static void Post<TState>(Action<TState> action, TState state)
        {
            _actions.Enqueue(new PostedAction<TState>(action, state));
            Volatile.Write(ref _hasActions, true);
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
