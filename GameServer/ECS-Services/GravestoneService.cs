using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.GS.ServerProperties;
using DOL.Logging;
using static DOL.GS.RolloverSchedulerService;

namespace DOL.GS
{
    public static class GravestoneService
    {
        public static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static Dictionary<string, HashSet<GameGravestone>> _gravestonesByOwner = new();
        private static Lock _gravestonesLock = new();

        public static void Initialize()
        {
            // We check every day, but CheckGravestones only processes gravestones whose individual timer has expired.
            RolloverSchedulerService.Instance.Subscribe(IntervalKey.Daily, CheckGravestones);
        }

        public static void AddGravestone(GameGravestone gravestone)
        {
            lock (_gravestonesLock)
            {
                if (_gravestonesByOwner.TryGetValue(gravestone.OwnerID, out var gravestones))
                    gravestones.Add(gravestone);
                else
                    _gravestonesByOwner[gravestone.OwnerID] = [gravestone];
            }
        }

        public static void RemoveGravestone(GameGravestone gravestone)
        {
            lock (_gravestonesLock)
            {
                if (_gravestonesByOwner.TryGetValue(gravestone.OwnerID, out var gravestones))
                {
                    gravestones.Remove(gravestone);

                    if (gravestones.Count == 0)
                        _gravestonesByOwner.Remove(gravestone.OwnerID);
                }
            }
        }

        public static GameGravestone PruneExcessGravestonesAndGetReusable(GamePlayer player)
        {
            const int MAX_GRAVESTONES_PER_PLAYER = 1;

            do
            {
                GameGravestone oldestGrave = null;
                bool isExcess = false;

                lock (_gravestonesLock)
                {
                    if (_gravestonesByOwner.TryGetValue(player.ObjectId, out var gravestones) && gravestones.Count >= MAX_GRAVESTONES_PER_PLAYER)
                    {
                        DateTime oldestTime = DateTime.MaxValue;

                        foreach (GameGravestone gravestone in gravestones)
                        {
                            if (gravestone.CreationTime < oldestTime)
                            {
                                oldestTime = gravestone.CreationTime;
                                oldestGrave = gravestone;
                            }
                        }

                        isExcess = gravestones.Count > MAX_GRAVESTONES_PER_PLAYER;
                    }
                }

                if (oldestGrave == null)
                    return null;

                if (isExcess)
                {
                    oldestGrave.Delete();
                    oldestGrave.DeleteFromDatabase();
                }
                else
                    return oldestGrave;
            } while (true);
        }

        private static void CheckGravestones()
        {
            if (Properties.GRAVESTONE_DECAY_TIME <= 0)
                return;

            var gravestonesToRemove = GameLoop.GetListForTick<GameStaticItem>();

            lock (_gravestonesLock)
            {
                foreach (var gravestoneSet in _gravestonesByOwner.Values)
                {
                    foreach (GameGravestone gravestone in gravestoneSet)
                    {
                        TimeSpan timeSinceCreation = DateTime.Now - gravestone.CreationTime;

                        if (timeSinceCreation.TotalDays < Properties.GRAVESTONE_DECAY_TIME)
                            continue;

                        if (log.IsInfoEnabled)
                            log.Info($"'{gravestone.Name}' has decayed and is being removed. (Time since creation: {timeSinceCreation})");

                        gravestonesToRemove.Add(gravestone);
                    }
                }
            }

            foreach (GameStaticItem gravestone in gravestonesToRemove)
            {
                gravestone.Delete();
                gravestone.DeleteFromDatabase();
            }
        }
    }
}
