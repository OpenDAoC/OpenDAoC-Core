using System.Reflection;
using DOL.Logging;

namespace DOL.GS
{
    public static class PathfindingProvider
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly NullPathfindingMgr NullPathfindingMgr = new();
        public static readonly LocalPathfindingMgr LocalPathfindingMgr = new();
        public static IPathfindingMgr Instance { get; private set; } = NullPathfindingMgr;

        public static bool Init()
        {
            if (log.IsInfoEnabled)
                log.Info($"Starting {nameof(PathfindingProvider)}");

            if (LocalPathfindingMgr.Init())
                SetPathfindingMgr(LocalPathfindingMgr);
            else
                SetPathfindingMgr(NullPathfindingMgr);

            return true;
        }

        public static void SetPathfindingMgr(IPathfindingMgr mgr)
        {
            if (log.IsInfoEnabled)
                log.Info($"Setting {nameof(PathfindingProvider)} to {mgr}");

            Instance = mgr ?? NullPathfindingMgr;
        }

        public static void Stop()
        {
            if (log.IsInfoEnabled)
                log.Info($"Stopping {nameof(PathfindingProvider)}");

            LocalPathfindingMgr.Stop();
        }
    }
}
