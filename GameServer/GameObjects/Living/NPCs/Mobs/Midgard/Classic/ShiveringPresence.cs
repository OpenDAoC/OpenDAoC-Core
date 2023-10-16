using DOL.AI.Brain;

namespace DOL.GS.Scripts;

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