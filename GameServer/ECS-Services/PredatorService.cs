using System;
using ECS.Debug;

namespace DOL.GS;

public class PredatorService
{
    private const string ServiceName = "Predator Service";

     private static long _updateInterval = 3000; // 3secs
    //private static long _updateInterval = ServerProperties.Properties.BOUNTY_CHECK_INTERVAL * 1000;

    private static long _lastUpdate;

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

        Diagnostics.StopPerfCounter(ServiceName);
    }
}