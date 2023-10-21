using Core.AI.Brain;
using Core.GS.AI.Brains;

namespace Core.GS.Scripts;

public class ShiveringPresence : GameNpc
{
    public ShiveringPresence() : base()
    {
    }

    public override bool AddToWorld()
    {
        var brain = new ShiveringPresenceBrain();
        SetOwnBrain(brain);
        Model = 966;
        return base.AddToWorld();
    }

}