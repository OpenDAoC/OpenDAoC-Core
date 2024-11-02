using DOL.AI.Brain;

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
            Flags &= ~eFlags.STEALTH;
        }
    }
}
