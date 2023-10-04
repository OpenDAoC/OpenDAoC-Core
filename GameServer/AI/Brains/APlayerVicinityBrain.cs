namespace DOL.AI;

/// <summary>
/// <p>This class is the base brain of all npc's that only stay active when players are close</p>
/// <p>This class defines the base for a brain that activates itself when players get close to
/// it's body and becomes dormat again after a certain amount of time when no players are close
/// to it's body anymore.</p>
/// <p>Useful to save CPU for MANY mobs that have no players in range, they will stay dormant.</p>
/// </summary>
public abstract class APlayerVicinityBrain : ABrain
{
    public APlayerVicinityBrain() : base() { }

    public override bool Start()
    {
        if (!Body.IsVisibleToPlayers)
            return false;

        return base.Start();
    }
}