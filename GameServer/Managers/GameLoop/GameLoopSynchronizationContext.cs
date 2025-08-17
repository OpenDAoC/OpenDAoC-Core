using System.Threading;

namespace DOL.GS
{
    public class GameLoopSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            GameLoopService.Post(static state => state.d(state.state), (d, state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            ManualResetEvent completed = new(false);

            GameLoopService.Post(static state =>
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
