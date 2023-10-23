using System;
using Core.GS.Enums;
using Core.GS.World;

namespace Core.GS.AI;

public class UaimhLairmasterBrain : StandardMobBrain
{
    protected byte MAX_Size = 100;
    protected byte MIN_Size = 60;

    protected String m_AggroAnnounce;
    public static bool IsAggroEnemies = true;

    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public UaimhLairmasterBrain() : base()
    {
        m_AggroAnnounce = "{0} feels threatened and appears more menacing!";
    }

    public override void Think()
    {
        if (Body.InCombat && Body.IsAlive && HasAggro)
        {
            if (Body.TargetObject != null)
            {
                if (IsAggroEnemies)
                {
                    //Starts Growing
                    GrowSize();
                }
            }
        }
        else
        {
            //Starts Shrinking
            ShrinkSize();
        }

        base.Think();
    }

    #region Broadcast Message

    /// <summary>
    /// Broadcast relevant messages to the raid.
    /// </summary>
    /// <param name="message">The message to be broadcast.</param>
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }

    #endregion

    public void GrowSize()
    {
        BroadcastMessage(String.Format(m_AggroAnnounce, Body.Name));
        Body.Size = MAX_Size;
        IsAggroEnemies = false;
    }

    public void ShrinkSize()
    {
        Body.Size = MIN_Size;
        IsAggroEnemies = true;
    }
}