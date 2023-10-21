using Core.AI.Brain;

namespace Core.GS.Scripts;

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