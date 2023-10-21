using System;
using Core.Events;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

public class JarlOrmarrBrain : StandardMobBrain
{
    protected String[] m_HitAnnounce;
    public JarlOrmarrBrain() : base()
    {
        m_HitAnnounce = new String[]
        {
            "Haha! You call that a hit? I\'ll show you a hit!",
            "I am a warrior, you can\'t kill me!"
        };
		
        AggroLevel = 50;
        AggroRange = 400;
    }
    public override void Think()
    {
        base.Think();
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
    /// Called whenever the Jarl Ormarr body sends something to its brain.
    /// </summary>
    /// <param name="e">The event that occured.</param>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event details.</param>
    public override void Notify(CoreEvent e, object sender, EventArgs args)
    {
        base.Notify(e, sender, args);
        if (e == GameObjectEvent.TakeDamage)
        {
            if (Util.Chance(3))
            {
                int messageNo = Util.Random(1, m_HitAnnounce.Length) - 1;
                BroadcastMessage(String.Format(m_HitAnnounce[messageNo]));
            }
        }
    }
}