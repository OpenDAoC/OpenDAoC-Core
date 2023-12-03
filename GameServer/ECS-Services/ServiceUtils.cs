using System;
using System.Reflection;
using log4net;

namespace DOL.GS
{
    public static class ServiceUtils
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const long HALF_TICK_RATE = GameLoop.TICK_RATE / 2;

        public static bool ShouldTick(long tickTime)
        {
            return GetDelta(tickTime) - HALF_TICK_RATE <= 0;
        }

        public static bool ShouldTickNoEarly(long tickTime)
        {
            return GetDelta(tickTime) <= 0;
        }

        public static bool ShouldTickAdjust(ref long tickTime)
        {
            long delta = GetDelta(tickTime);

            // Consider ticks to be late if the game loop can't keep up with its tick rate.
            // This is used to adjust services' tick time, allowing them to catch up.
            if (delta < -GameLoop.TICK_RATE)
            {
                tickTime = GameLoop.GameLoopTime;
                return true;
            }

            return delta - HALF_TICK_RATE <= 0;
        }

        public static bool ShouldTickAdjustNoEarly(ref long tickTime)
        {
            long delta = GetDelta(tickTime);

            // Consider ticks to be late if the game loop can't keep up with its tick rate.
            // This is used to adjust services' tick time, allowing them to catch up.
            if (delta < -GameLoop.TICK_RATE)
            {
                tickTime = GameLoop.GameLoopTime;
                return true;
            }

            return delta <= 0;
        }

        private static long GetDelta(long tickTime)
        {
            // Positive if we're checking early, negative otherwise.
            return tickTime - GameLoop.GameLoopTime;
        }

        public static void HandleServiceException<T>(Exception exception, string serviceName, T entity, GameObject entityOwner) where T : class, IManagedEntity
        {
            log.Error($"Critical error encountered in {serviceName}: {exception}");
            EntityManager.Remove(entity);

            if (entityOwner is GamePlayer player)
            {
                if (player.CharacterClass.ID == (int) eCharacterClass.Necromancer && player.IsShade)
                    player.Shade(false);

                player.Out.SendPlayerQuit(false);
                player.Quit(true);
                CraftingProgressMgr.FlushAndSaveInstance(player);
                player.SaveIntoDatabase();
            }
            else
                entityOwner?.RemoveFromWorld();
        }
    }
}
