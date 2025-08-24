using System;
using System.Threading;
using System.Threading.Tasks;

namespace DOL.GS
{
    public static class GameServiceContext
    {
        public static readonly AsyncLocal<IGameService> Current = new();
    }

    public class GameLoopSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            IGameService targetService = GameServiceContext.Current.Value;

            // Typically, a continuation from a Task on which ConfigureAwait(false) was called.
            if (targetService == null)
            {
                d(state);
                return;
            }

            targetService.Post(static state => state.d(state.state), (d, state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            // Calling Send from a GameLoop thread would cause a deadlock without this check.
            if (Current == this)
            {
                d(state);
                return;
            }

            IGameService targetService = GameServiceContext.Current.Value ?? GameLoopService.Instance;
            using ManualResetEvent completed = new(false);

            targetService.Post(static state =>
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
    }

    public static class GameLoopAsyncHelper
    {
        public static T GetResult<T>(Task<T> task)
        {
            if (SynchronizationContext.Current is GameLoopSynchronizationContext && !task.IsCompleted)
                ProcessServicePostedActionsUntilCompletion(task);

            return task.GetAwaiter().GetResult();
        }

        public static void Wait(Task task)
        {
            if (SynchronizationContext.Current is GameLoopSynchronizationContext && !task.IsCompleted)
                ProcessServicePostedActionsUntilCompletion(task);

            task.GetAwaiter().GetResult();
            return;
        }

        private static void ProcessServicePostedActionsUntilCompletion(Task task)
        {
            IGameService targetService = GameServiceContext.Current.Value ?? throw new InvalidOperationException();
            SpinWait spinWait = new();

            while (!task.IsCompleted)
            {
                targetService.ProcessPostedActions();
                spinWait.SpinOnce(-1);
            }
        }
    }
}
