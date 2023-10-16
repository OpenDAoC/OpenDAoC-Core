namespace DOL.GS;

public class StealtherMob : GameNpc
{
    public StealtherMob() : base() 
    {            
        Flags = ENpcFlags.STEALTH;
    }
}