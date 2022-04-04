using ECS.Debug;

namespace DOL.GS;

public class BountyService
{
    private const string ServiceName = "Bounty Service";

    private static BountyManager BountyManager;

    // private static long _updateInterval = 10000; // 10secs
    private static long _updateInterval = ServerProperties.Properties.BOUNTY_CHECK_INTERVAL * 1000;

    private static long _lastUpdate;

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

        Diagnostics.StopPerfCounter(ServiceName);
    }
}