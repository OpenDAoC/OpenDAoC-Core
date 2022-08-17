using System;
using System.Linq;
using DOL.GS.PacketHandler;
using ECS.Debug;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DOL.GS;

public class PredatorService
{
    private const string ServiceName = "Predator Service";

     private static long _updateInterval = 3000; // 3secs
     private static long _messageBroadcastInterval = 15000; // 15secs
    private static long _insertInterval = ServerProperties.Properties.QUEUED_PLAYER_INSERT_INTERVAL * 1000;

    private static long _lastUpdate;
    private static long _lastInsert;
    private static long _lastMessage;

    static PredatorService()
    {
        EntityManager.AddService(typeof(PredatorService));
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);

        if (tick - _lastUpdate > _updateInterval)
        {
            _lastUpdate = tick;
            //Console.WriteLine($"Predator || Queued Players: {PredatorManager.QueuedPlayers.Count} | Active Players: {PredatorManager.ActivePredators.Count}");
            foreach (var activePreds in PredatorManager.ActiveBounties.ToList())
            {
                GamePlayer activePlayer = activePreds.Predator;
                
                if (activePlayer == null) continue;

                AbstractArea area = activePlayer.CurrentZone?.GetAreasOfSpot(activePlayer.X, activePlayer.Y, activePlayer.Z)
                    .FirstOrDefault() as AbstractArea;
                
                //if user is not in an RvR zone, or is in DF
                if (activePlayer.CurrentZone is {IsRvR: false})
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
        
        if (tick - _lastMessage > _messageBroadcastInterval)
        {
            _lastMessage = tick;
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
        
        if (tick - _lastInsert > _insertInterval)
        {
            _lastInsert = tick;
            PredatorManager.InsertQueuedPlayers();
            PredatorManager.InsertFreshKillers();
            PredatorManager.TryFillEmptyPrey();
            //Console.WriteLine($"INSERTING Predator || Queued Players: {PredatorManager.QueuedPlayers.Count} | Active Players: {PredatorManager.ActivePredators.Count}");
        }

        Diagnostics.StopPerfCounter(ServiceName);
    }
}