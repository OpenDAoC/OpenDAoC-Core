using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Events;
using log4net;

namespace Core.GS.Scripts.Custom;

public class HardCoreLogin
{
    
    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    
    [GameServerStartedEvent]
    public static void OnServerStart(CoreEvent e, object sender, EventArgs arguments)
    {
        GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new CoreEventHandler(HCPlayerEntered));
    }

    /// <summary>
    /// Event handler fired when server is stopped
    /// </summary>
    [GameServerStoppedEvent]
    public static void OnServerStop(CoreEvent e, object sender, EventArgs arguments)
    {
        GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new CoreEventHandler(HCPlayerEntered));
    }
    
    /// <summary>
    /// Event handler fired when players enters the game
    /// </summary>
    /// <param name="e"></param>
    /// <param name="sender"></param>
    /// <param name="arguments"></param>
    private static void HCPlayerEntered(CoreEvent e, object sender, EventArgs arguments)
    {
        GamePlayer player = sender as GamePlayer;
        if (player == null) return;
        if (!player.HCFlag) return;
        
        if (player.DeathCount > 0 && player.HCFlag)
        {
            DbCoreCharacter cha = CoreDb<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(player.Name));
            if (cha != null)
            {
                Log.Warn("[HARDCORE] player " + player.Name + " has " + player.DeathCount + " deaths and has been removed from the database.");
                GameServer.Database.DeleteObject(cha);
                player.Client.Out.SendPlayerQuit(true);
            }
        }
    }
}