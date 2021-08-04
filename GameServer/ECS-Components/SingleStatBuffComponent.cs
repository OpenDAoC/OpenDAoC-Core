using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class SingleStatBuffComponent
    {
        public GameLiving owner;
        public bool isApplied;
        public eStat statToModify;
        public int buffValue;
        public int timeSinceApplication;
        public int maxDuration;

        public SingleStatBuffComponent(GameLiving owner, eStat stat, int buffValue, int maxDuration)
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