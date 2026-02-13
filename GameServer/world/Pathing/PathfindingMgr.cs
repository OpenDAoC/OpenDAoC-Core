using System.Reflection;

namespace DOL.GS
{
    /// <summary>
    /// Wrapper for the currently active pathfinding mgr
    /// </summary>
    public static class PathfindingMgr
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly NullPathfindingMgr NullPathfindingMgr = new();
        public static readonly LocalPathfindingMgr LocalPathfindingMgr = new();
        public static IPathfindingMgr Instance { get; private set; } = NullPathfindingMgr;

        public static bool Init()
        {
            if (log.IsInfoEnabled)
                log.Info($"Starting {nameof(PathfindingMgr)}");

            if (LocalPathfindingMgr.Init())
                SetPathfindingMgr(LocalPathfindingMgr);
            else
                SetPathfindingMgr(NullPathfindingMgr);

            return true;
        }

        public static void SetPathfindingMgr(IPathfindingMgr mgr)
        {
            if (log.IsInfoEnabled)
                log.Info($"Setting {nameof(PathfindingMgr)} to {mgr}");

            Instance = mgr ?? NullPathfindingMgr;
        }

        public static void Stop()
        {
            if (log.IsInfoEnabled)
                log.Info($"Stopping {nameof(PathfindingMgr)}");

            LocalPathfindingMgr.Stop();
        }
    }
}
