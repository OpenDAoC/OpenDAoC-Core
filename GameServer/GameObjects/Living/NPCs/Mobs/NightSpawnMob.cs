using System;
using Core.GS.AI.Brains;
using Core.GS.Events;

namespace Core.GS;

public class NightSpawnMob : GameNpc
{
    public override bool AddToWorld()
    {
        NightSpawnBrain sBrain = new NightSpawnBrain();
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
            log.Info("Night mobs initialising...");
    }
}