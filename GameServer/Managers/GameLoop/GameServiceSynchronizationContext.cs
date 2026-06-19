using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DOL.GS
{
    public static class GameServiceContext
    {
        private static readonly ConcurrentDictionary<IGameService, GameServiceSynchronizationContext> _contexts = new();

        public static GameServiceSynchronizationContext GetContextFor(IGameService service)
        {
            return service == null ? null : _contexts.GetOrAdd(service, static s => new(s));
        }
    }

    public class GameServiceSynchronizationContext : SynchronizationContext
    {
        public IGameService TargetService { get; }

        public GameServiceSynchronizationContext(IGameService targetService)
        {
            TargetService = targetService;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            TargetService.Post(static state => state.d(state.state), (d, state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            // Deadlock prevention for game loop cross-talk.
            if (Current is GameServiceSynchronizationContext)
            {
                d(state);
                return;
            }

            using ManualResetEvent completed = new(false);

            TargetService.Post(static state =>
            {
                try
                {
                    state.d(state.state);
                }
                finally
                {
                    state.completed.Set();
                }
            }, (d, state, completed));

            completed.WaitOne();
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }

    public static class GameLoopAsyncHelper
    {
        public static T GetResult<T>(Task<T> task)
        {
            if (SynchronizationContext.Current is GameServiceSynchronizationContext && !task.IsCompleted)
                ProcessServicePostedActionsUntilCompletion(task);

            return task.GetAwaiter().GetResult();
        }

        public static void Wait(Task task)
        {
            if (SynchronizationContext.Current is GameServiceSynchronizationContext && !task.IsCompleted)
                ProcessServicePostedActionsUntilCompletion(task);

            task.GetAwaiter().GetResult();
            return;
        }

        private static void ProcessServicePostedActionsUntilCompletion(Task task)
        {
            if (SynchronizationContext.Current is not GameServiceSynchronizationContext ctx)
                throw new InvalidOperationException("Not running on a Game Service context.");

            IGameService targetService = ctx.TargetService;
            SpinWait spinWait = new();

            while (!task.IsCompleted)
            {
                targetService.ProcessPostedActions();
                spinWait.SpinOnce(-1);
            }
        }
    }
}
