using Core.GS.AI;

namespace Core.GS;

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