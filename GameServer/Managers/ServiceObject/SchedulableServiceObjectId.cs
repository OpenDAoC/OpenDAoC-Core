namespace DOL.GS
{
    public class SchedulableServiceObjectId : ServiceObjectId
    {
        public const int SLEEPING_ID = -2;

        public bool IsSleeping => Value == SLEEPING_ID;
        public long ExpectedWakeTick { get; set; } = -1;

        public SchedulableServiceObjectId(ServiceObjectType type) : base(type) { }

        public override void Unset()
        {
            base.Unset();
            ExpectedWakeTick = -1;
        }
    }
}
