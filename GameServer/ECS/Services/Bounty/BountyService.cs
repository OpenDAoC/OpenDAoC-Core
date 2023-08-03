using ECS.Debug;

namespace DOL.GS
{
    public class BountyService
    {
        private const string SERVICE_NAME = "BountyService";

        private static BountyMgr BountyManager = new();

        // private static long _updateInterval = 10000; // 10secs
        private static long _updateInterval = ServerProperties.ServerProperties.BOUNTY_CHECK_INTERVAL * 1000;

        private static long _lastUpdate;

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            if (tick - _lastUpdate > _updateInterval)
            {
                _lastUpdate = tick;
                BountyMgr.CheckExpiringBounty(tick);
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}