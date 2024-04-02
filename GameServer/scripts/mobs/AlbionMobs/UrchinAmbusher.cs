using DOL.AI.Brain;
using System;

namespace DOL.GS
{
    public class UrchinAmbusher : StealtherMob
    {
        public UrchinAmbusher() : base()
        {
            SetOwnBrain(new UrchinAmbusherBrain());
        }
        public void LeaveStealth()
        {
            Flags &= eFlags.STEALTH;
        }
    }
}
