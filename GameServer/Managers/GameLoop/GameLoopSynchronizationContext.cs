using System.Threading;

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
            IGameService targetService = GameServiceContext.Current.Value ?? GameLoopService.Instance;
            targetService.Post(static state => state.d(state.state), (d, state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            // Calling Send from a GameLoop thread will cause a deadlock.
            // This method is only safe to call from external threads (e.g., network I/O).

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
}
