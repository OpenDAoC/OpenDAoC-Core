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

    private static long _nextUpdate;
    private static long _nextInsert;
    private static long _nextMessage;

    public static void Tick()
    {
        GameLoop.CurrentServiceTick = SERVICE_NAME;
        Diagnostics.StartPerfCounter(SERVICE_NAME);

        if (ServiceUtils.ShouldTickAdjust(ref _nextUpdate))
        {
            _nextUpdate += _updateInterval;
            //Console.WriteLine($"Predator || Queued Players: {PredatorManager.QueuedPlayers.Count} | Active Players: {PredatorManager.ActivePredators.Count}");
            foreach (var activePreds in PredatorManager.ActiveBounties.ToList())
            {
                GamePlayer activePlayer = activePreds.Predator;
                
                if (activePlayer == null) continue;

                AbstractArea area = activePlayer.CurrentZone?.GetAreasOfSpot(activePlayer.X, activePlayer.Y, activePlayer.Z)
                    .FirstOrDefault() as AbstractArea;
                
                //if user is not in an RvR zone, or is in DF
                if (ConquestService.ConquestManager.IsPlayerInSafeZone(activePlayer))
                {
                    if(!activePlayer.PredatorTimeoutTimer.IsAlive)
                        PredatorManager.StartTimeoutCountdownFor(activePlayer);
                }
                else if(activePlayer.PredatorTimeoutTimer.IsAlive)
                {
                    PredatorManager.StopTimeoutCountdownFor(activePlayer);
                }
            }
        }
        
        if (ServiceUtils.ShouldTickAdjust(ref _nextMessage))
        {
            _nextMessage += _messageBroadcastInterval;
            foreach (var activePreds in PredatorManager.ActiveBounties.ToList())
            {
                if (activePreds.Predator != null && activePreds.Prey != null &&
                    activePreds.Predator.GetDistance(activePreds.Prey) <= WorldMgr.VISIBILITY_DISTANCE)
                {
                    if (!activePreds.Predator.InCombat)
                        activePreds.Predator.Out.SendMessage($"Your prey is within sight.",
                            eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
                    if (!activePreds.Prey.InCombat)
                        activePreds.Prey.Out.SendMessage($"Your senses tingle. A hunter is near.",
                            eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    //activePreds.Predator.Out.SendMessage($"Your prey is within sight.", eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
                    //TODO: figure out compass coordinate readouts here
                }
            }
        }
        
        if (ServiceUtils.ShouldTickAdjust(ref _nextInsert))
        {
            _nextInsert += _insertInterval;
            PredatorManager.InsertQueuedPlayers();
            PredatorManager.InsertFreshKillers();
            PredatorManager.TryFillEmptyPrey();
            //Console.WriteLine($"INSERTING Predator || Queued Players: {PredatorManager.QueuedPlayers.Count} | Active Players: {PredatorManager.ActivePredators.Count}");
        }

        Diagnostics.StopPerfCounter(SERVICE_NAME);
    }
}