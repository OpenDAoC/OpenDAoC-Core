using System;
using Core.AI.Brain;
using Core.GS;
using Core.GS.AI.Brains;

namespace Core.GS.Scripts;

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