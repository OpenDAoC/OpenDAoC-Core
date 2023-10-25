using Core.GS.AI;
using Core.GS.Enums;

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
