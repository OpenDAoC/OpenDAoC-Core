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
}
