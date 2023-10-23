using Core.GS.AI;
using Core.GS.Enums;

namespace Core.GS;

public class AntipassGuard : GameNpc
{
    public override bool AddToWorld()
    {
        SetOwnBrain(new AntipassBrain());
        Brain.Start();
        base.AddToWorld();
        Name = "No Pass";
        Flags |= ENpcFlags.PEACE;
        //Flags |= (uint)GameNPC.eFlags.CANTTARGET;
        Flags |= ENpcFlags.FLYING;      
        Model = 10;
        Size = 50;
        Level = 90;
        MaxSpeedBase = 0;
        return true;
    }
}