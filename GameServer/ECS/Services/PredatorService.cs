using System.Linq;
using DOL.GS.PacketHandler;
using ECS.Debug;

namespace DOL.GS;

public class PredatorService
{
    private const string SERVICE_NAME = "PredatorService";

    private static long _updateInterval = 3000; // 3secs
    private static long _messageBroadcastInterval = 15000; // 15secs
    private static long _insertInterval = ServerProperties.Properties.QUEUED_PLAYER_INSERT_INTERVAL * 1000;

    private static long _lastUpdate;
    private static long _lastInsert;
    private static long _lastMessage;

    public static void Tick(long tick)
    {
        GameLoop.CurrentServiceTick = SERVICE_NAME;
        Diagnostics.StartPerfCounter(SERVICE_NAME);

        if (tick - _lastUpdate > _updateInterval)
        {
            _lastUpdate = tick;
            //Console.WriteLine($"Predator || Queued Players: {PredatorManager.QueuedPlayers.Count} | Active Players: {PredatorManager.ActivePredators.Count}");
            foreach (var activePreds in PredatorMgr.ActiveBounties.ToList())
            {
                GamePlayer activePlayer = activePreds.Predator;
                
                if (activePlayer == null) continue;

                AbstractArea area = activePlayer.CurrentZone?.GetAreasOfSpot(activePlayer.X, activePlayer.Y, activePlayer.Z)
                    .FirstOrDefault() as AbstractArea;
                
                //if user is not in an RvR zone, or is in DF
                if (ConquestService.ConquestManager.IsPlayerInSafeZone(activePlayer))
                {
                    if(!activePlayer.PredatorTimeoutTimer.IsAlive)
                        PredatorMgr.StartTimeoutCountdownFor(activePlayer);
                }
                else if(activePlayer.PredatorTimeoutTimer.IsAlive)
                {
                    PredatorMgr.StopTimeoutCountdownFor(activePlayer);
                }
            }
        }
        
        if (tick - _lastMessage > _messageBroadcastInterval)
        {
            _lastMessage = tick;
            foreach (var activePreds in PredatorMgr.ActiveBounties.ToList())
            {
                if (activePreds.Predator != null && activePreds.Prey != null &&
                    activePreds.Predator.GetDistance(activePreds.Prey) <= WorldMgr.VISIBILITY_DISTANCE)
                {
                    if (!activePreds.Predator.InCombat)
                        activePreds.Predator.Out.SendMessage($"Your prey is within sight.",
                            EChatType.CT_ScreenCenterSmaller_And_CT_System, EChatLoc.CL_SystemWindow);
                    if (!activePreds.Prey.InCombat)
                        activePreds.Prey.Out.SendMessage($"Your senses tingle. A hunter is near.",
                            EChatType.CT_ScreenCenterSmaller_And_CT_System, EChatLoc.CL_SystemWindow);
                }
                else
                {
                    //activePreds.Predator.Out.SendMessage($"Your prey is within sight.", eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
                    //TODO: figure out compass coordinate readouts here
                }
            }
        }
        
        if (tick - _lastInsert > _insertInterval)
        {
            _lastInsert = tick;
            PredatorMgr.InsertQueuedPlayers();
            PredatorMgr.InsertFreshKillers();
            PredatorMgr.TryFillEmptyPrey();
            //Console.WriteLine($"INSERTING Predator || Queued Players: {PredatorManager.QueuedPlayers.Count} | Active Players: {PredatorManager.ActivePredators.Count}");
        }

        Diagnostics.StopPerfCounter(SERVICE_NAME);
    }
}