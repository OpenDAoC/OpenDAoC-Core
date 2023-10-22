using System;
using Core.GS.AI.Brains;
using Core.GS.Events;

namespace Core.GS;

public class DaySpawnMob : GameNpc
{
    public override bool AddToWorld()
    {
        DaySpawnBrain sBrain = new DaySpawnBrain();
        if (NPCTemplate != null)
        {
            sBrain.AggroLevel = NPCTemplate.AggroLevel;
            sBrain.AggroRange = NPCTemplate.AggroRange;
        }
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Day mobs initialising...");
    }
}