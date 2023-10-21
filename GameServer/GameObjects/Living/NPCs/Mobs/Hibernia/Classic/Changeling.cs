using Core.AI.Brain;
using Core.GS.AI.Brains;

namespace Core.GS.Scripts;

public class Changeling : GameNpc
{
    public Changeling() : base()
    {
    }
    
    public override bool AddToWorld()
    {
        var brain = new ChangelingBrain();
        SetOwnBrain(brain);
        return base.AddToWorld();
    }
    
}