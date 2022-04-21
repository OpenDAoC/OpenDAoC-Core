using System;
using System.Linq;
using ECS.Debug;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DOL.GS;

public class PredatorService
{
    private const string ServiceName = "Predator Service";

     private static long _updateInterval = 3000; // 3secs
    private static long _insertInterval = ServerProperties.Properties.QUEUED_PLAYER_INSERT_INTERVAL * 1000;

    private static long _lastUpdate;
    private static long _lastInsert;

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
            foreach (var activePreds in PredatorManager.ActivePredators.ToList())
            {
                GamePlayer activePlayer = activePreds.Predator;
                
                AbstractArea area = activePlayer.CurrentZone.GetAreasOfSpot(activePlayer.X, activePlayer.Y, activePlayer.Z)
                    .FirstOrDefault() as AbstractArea;
                
                //if user is not in an RvR zone, or is in DF
                if ((!activePlayer.CurrentZone.IsRvR 
                     && (area == null || (area != null && !area.Description.Equals("Druim Ligen")))) 
                     || activePlayer.CurrentZone.ID == 249)
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