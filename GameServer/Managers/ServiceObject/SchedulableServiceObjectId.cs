using System.Threading;

namespace DOL.GS
{
    public class SchedulableServiceObjectId : ServiceObjectId
    {
        public const int SLEEPING_ID = -2;

        private long _scheduleToken;
        private long _sleepToken;

        public bool IsSleeping => Value == SLEEPING_ID;
        public long SleepToken => Volatile.Read(ref _sleepToken);
        public long ScheduleToken => Volatile.Read(ref _scheduleToken);

        public SchedulableServiceObjectId(ServiceObjectType type) : base(type) { }

        public long NextScheduleToken()
        {
            return Interlocked.Increment(ref _scheduleToken);
        }

        public void InvalidateScheduleRequests()
        {
            Interlocked.Increment(ref _scheduleToken);
        }

        public override void MoveTo(int index)
        {
            Interlocked.Increment(ref _sleepToken);
            base.MoveTo(index);
        }
    }
}
