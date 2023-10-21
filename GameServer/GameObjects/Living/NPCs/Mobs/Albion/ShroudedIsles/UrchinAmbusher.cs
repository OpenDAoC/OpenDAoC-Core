using Core.AI.Brain;
using Core.GS.AI.Brains;

namespace Core.GS
{
    public class UrchinAmbusher : StealtherMob
    {
        public UrchinAmbusher() : base()
        {
            SetOwnBrain(new UrchinAmbusherBrain());
        }
        public void LeaveStealth()
        {
            Flags &= ENpcFlags.STEALTH;
        }
    }
}
