namespace DOL.GS
{
    public class SingleStatBuffComponent
    {
        public GameLiving owner;
        public bool isApplied;
        public EStat statToModify;
        public int buffValue;
        public int timeSinceApplication;
        public int maxDuration;

        public SingleStatBuffComponent(GameLiving owner, EStat stat, int buffValue, int maxDuration)
        {
            this.owner = owner;
            this.statToModify = stat;
            this.buffValue = buffValue;
            this.isApplied = false;
            this.timeSinceApplication = 0;
            this.maxDuration = maxDuration;
        }

        public void UpdateTimeLeft()
        {
            //figure out how best to track buff durations
        }
    }
}