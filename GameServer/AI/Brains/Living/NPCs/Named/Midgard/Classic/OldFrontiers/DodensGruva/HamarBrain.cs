using System;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.AI.Brains;

public class HamarBrain : StandardMobBrain
{
    private bool _startAttack = true;
    public HamarBrain() : base()
    {
        AggroLevel = 200;
        AggroRange = 550;
    }

    public override void Think()
    {
        base.Think();

        if (Body.InCombat && Body.IsAlive && HasAggro)
        {
            if (Body.TargetObject != null)
            {
                if (_startAttack)
                {
                    foreach (GameNpc vendos in Body.GetNPCsInRadius(1000))
                    {
                        if (vendos == null) 
                            return;
						
                        foreach (GamePlayer player in Body.GetPlayersInRadius(1000))
                        {
                            if (player == null)
                                return;

                            if (vendos.Name.ToLower().Contains("snow vendo") && vendos.IsVisibleTo(Body))
                            {
                                vendos.StartAttack(player);
                                _startAttack = false;
                            }
                        }
                    }
                }
            }
        }
        else if(!Body.InCombat && Body.IsAlive && !HasAggro)
        {
            _startAttack = true;
        }
    }

    /// <summary>
    /// Broadcast relevant messages to the raid.
    /// </summary>
    /// <param name="message">The message to be broadcast.</param>
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
        }
    }
		
    /// <summary>
    /// Called whenever the Hamar body sends something to its brain.
    /// </summary>
    /// <param name="e">The event that occured.</param>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event details.</param>
    public override void Notify(CoreEvent e, object sender, EventArgs args)
    {
        base.Notify(e, sender, args);
    }
}