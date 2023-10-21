using System;
using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS.Scripts;

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