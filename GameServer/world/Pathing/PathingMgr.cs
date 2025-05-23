﻿using System.Reflection;

namespace DOL.GS
{
    /// <summary>
    /// Wrapper for the currently active pathing mgr
    /// </summary>
    public static class PathingMgr
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// PathingMgr that does nothing
        /// </summary>
        public static readonly IPathingMgr NullPathingMgr = new NullPathingMgr();

        /// <summary>
        /// Calculates data locally and performs calls here
        /// </summary>
        public static readonly LocalPathingMgr LocalPathingMgr = new LocalPathingMgr();

        public static bool Init()
        {
            if (log.IsInfoEnabled)
                log.Info("Starting PathingMgr");

            if (LocalPathingMgr.Init())
                SetPathingMgr(LocalPathingMgr);
            else
                SetPathingMgr(NullPathingMgr);

            return true;
        }

        /// <summary>
        /// Changes the active pathing mgr
        /// </summary>
        /// <param name="mgr"></param>
        public static void SetPathingMgr(IPathingMgr mgr)
        {
            if (log.IsInfoEnabled)
                log.Info($"Setting PathingMgr to {mgr}");

            Instance = mgr ?? NullPathingMgr;
        }

        public static void Stop()
        {
            if (log.IsInfoEnabled)
                log.Info("Stopping PathingMgr");

            LocalPathingMgr.Stop();
        }

        /// <summary>
        /// Currently used instance
        /// </summary>
        public static IPathingMgr Instance { get; private set; } = NullPathingMgr;
    }
}
