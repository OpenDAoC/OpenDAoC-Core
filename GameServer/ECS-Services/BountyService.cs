using System;
using ECS.Debug;

namespace DOL.GS;

public class BountyService
{
    private const string ServiceName = "Bounty Service";

    public static BountyManager BountyManager;
    
    private static long _updateInterval = 10000; // 10secs
    // private static long _updateInterval = ServerProperties.Properties.BOUNTY_CHECK_INTERVAL * 1000; // 10secs

    private static long _lastUpdate = 0;
    private static long _lastDebug = 0;

    static BountyService()
    {
        EntityManager.AddService(typeof(ConquestService));
        BountyManager = new BountyManager();
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);

        if (tick - _lastUpdate > _updateInterval)
        {
            _lastUpdate = tick;
            BountyManager.CheckExpiringBounty(tick);
        }

        // if (tick - _lastDebug > 1000)
        // {
        //     Console.WriteLine(
        //         $"bounty heartbeat {GameLoop.GameLoopTime} - next check at {_lastUpdate + _updateInterval} in {(_lastUpdate + _updateInterval - tick) / 1000} seconds");
        //     _lastDebug = tick;
        // }

        Diagnostics.StopPerfCounter(ServiceName);
    }
}