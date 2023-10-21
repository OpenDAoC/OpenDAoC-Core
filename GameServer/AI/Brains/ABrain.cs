using System;
using System.Text;
using Core.AI;
using Core.Events;
using Core.GS.AI.States;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

/// <summary>
/// This class is the base of all artificial intelligence in game objects
/// </summary>
public abstract class ABrain : IManagedEntity
{
    public FiniteStateMachine FiniteStateMachine { get; set; }
    public EntityManagerId EntityManagerId { get; set; } = new(EEntityType.Brain, false);
    public virtual GameNpc Body { get; set; }
    public virtual bool IsActive => Body != null && Body.IsAlive && Body.ObjectState == GameObject.eObjectState.Active && Body.IsVisibleToPlayers;
    public virtual int ThinkInterval { get; set; } = 2500;
    public virtual long LastThinkTick { get; set; }

    /// <summary>
    /// Returns the string representation of the ABrain
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return new StringBuilder()
            .Append("body name='").Append(Body==null?"(null)":Body.Name)
            .Append("' (id=").Append(Body==null?"(null)":Body.ObjectID.ToString())
            .Append("), active=").Append(IsActive)
            .Append(", ThinkInterval=").Append(ThinkInterval)
            .ToString();
    }

    /// <summary>
    /// Starts the brain thinking
    /// </summary>
    /// <returns>true if started</returns>
    public virtual bool Start()
    {
        return EntityMgr.Add(this);
    }

    /// <summary>
    /// Stops the brain thinking
    /// </summary>
    /// <returns>true if stopped</returns>
    public virtual bool Stop()
    {
        return EntityMgr.Remove(this);
    }

    /// <summary>
    /// Receives all messages of the body
    /// </summary>
    /// <param name="e">The event received</param>
    /// <param name="sender">The event sender</param>
    /// <param name="args">The event arguments</param>
    public virtual void Notify(CoreEvent e, object sender, EventArgs args) { }

    /// <summary>
    /// This method is called whenever the brain does some thinking
    /// </summary>
    public abstract void Think();

    public abstract void KillFSM();
}