using ECS.Debug;

namespace DOL.GS
{
    public class BountyService
    {
        private const string SERVICE_NAME = "BountyService";

        private static BountyManager BountyManager = new();
        private static long _updateInterval = ServerProperties.Properties.BOUNTY_CHECK_INTERVAL * 1000;
        private static long _nextUpdate;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            if (ServiceUtils.ShouldTickAdjust(ref _nextUpdate))
            {
                _nextUpdate += _updateInterval;
                BountyManager.CheckExpiringBounty();
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
