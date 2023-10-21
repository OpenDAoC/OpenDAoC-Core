using DOL.AI.Brain;

namespace DOL.GS.Scripts;

public class Strangler : GameNpc
{
    public Strangler() : base()
    {
    }

    public override bool AddToWorld()
    {
        var brain = new StranglerBrain();
        SetOwnBrain(brain);
        return base.AddToWorld();
    }

}