using System;
using Core.GS.AI.Brains;
using Core.GS.Events;

namespace Core.GS;

public class Alina : GameNpc
{
    public override bool AddToWorld()
    {
        AlinaModelBrain sBrain = new AlinaModelBrain();
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Alina initialising...");
    }
}