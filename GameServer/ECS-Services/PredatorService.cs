using System;
using ECS.Debug;

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
            Console.WriteLine($"Predator || Queued Players: {PredatorManager.QueuedPlayers.Count} | Active Players: {PredatorManager.ActivePredators.Count}");
        }
        
        if (tick - _lastInsert > _insertInterval)
        {
            _lastInsert = tick;
            PredatorManager.InsertQueuedPlayers();
            PredatorManager.TryFillEmptyPrey();
            Console.WriteLine($"INSERTING Predator || Queued Players: {PredatorManager.QueuedPlayers.Count} | Active Players: {PredatorManager.ActivePredators.Count}");
        }

        Diagnostics.StopPerfCounter(ServiceName);
    }
}