namespace DOL.GS
{
    public class SchedulableServiceObjectId : ServiceObjectId
    {
        public const int SLEEPING_ID = -2;

        public bool IsSleeping => Value == SLEEPING_ID;
        public long SleepToken { get; private set; } = 0;

        public SchedulableServiceObjectId(ServiceObjectType type) : base(type) { }

        public override void MoveTo(int index)
        {
            SleepToken++;
            base.MoveTo(index);
        }
    }
}
