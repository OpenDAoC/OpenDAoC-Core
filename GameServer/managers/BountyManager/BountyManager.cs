using System;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS;

public class BountyManager
{
    [ScriptLoadedEvent]
    public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
    {
        GameEventMgr.AddHandler(GamePlayerEvent.Dying, new DOLEventHandler(GreyPlayerKilled));
    }
    
    public BountyManager()
    {

    }

    public const string KILLEDBY = "KilledBy";
    
    private static void GreyPlayerKilled(DOLEvent e, object sender, EventArgs args)
    {
        GamePlayer player = sender as GamePlayer;

        if (player == null) return;
        
        if (e != GameLivingEvent.Dying) return;
        
        DyingEventArgs eArgs = args as DyingEventArgs;

        if (eArgs.Killer is not GamePlayer) return;
        
        GamePlayer killer = eArgs.Killer as GamePlayer;
        
        Console.WriteLine($"player killed (con: {killer.GetConLevel(player)})");

        if (killer.GetConLevel(player) <= -3)
        {
            player.TempProperties.setProperty(KILLEDBY, killer);
            player.Out.SendMessage($"Use /bounty add <amount> if you want to raise a bounty on {killer.Name}!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            Console.WriteLine($"gray killed (con: {killer.GetConLevel(player)})");
        }
    }
}