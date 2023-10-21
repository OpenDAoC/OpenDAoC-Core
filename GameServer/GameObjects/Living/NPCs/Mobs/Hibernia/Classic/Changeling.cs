using DOL.AI.Brain;

namespace DOL.GS.Scripts;

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