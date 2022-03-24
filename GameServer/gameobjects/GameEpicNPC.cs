using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.ServerProperties;

namespace DOL.GS {
    public class GameEpicNPC : GameNPC {
        public GameEpicNPC() : base()
        {
            ScalingFactor = 60;
            OrbsReward = Properties.EPICNPC_ORBS;
        }
    }
}
