using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.GS.ServerProperties;

namespace DOL.GS {
    public class GameEpicBoss : GameEpicNPC {
        public GameEpicBoss() : base()
        {
            ScalingFactor = 80;
            OrbsReward = Properties.EPICBOSS_ORBS;
        }
        
    }
}
