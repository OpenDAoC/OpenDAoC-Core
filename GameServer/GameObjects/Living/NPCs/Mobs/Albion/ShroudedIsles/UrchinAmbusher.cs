using Core.AI.Brain;

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
