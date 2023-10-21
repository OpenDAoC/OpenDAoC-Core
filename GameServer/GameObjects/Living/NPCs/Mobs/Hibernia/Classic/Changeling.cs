using Core.AI.Brain;

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