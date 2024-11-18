using System;
using System.Reflection;
using log4net;

namespace DOL.GS
{
    public static class ServiceUtils
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static long HalfTickRate => GameLoop.TickRate / 2;

        public static bool ShouldTick(long tickTime)
        {
            return GetDelta(tickTime) - HalfTickRate <= 0;
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
            if (delta < -GameLoop.TickRate)
            {
                tickTime = GameLoop.GameLoopTime;
                return true;
            }

            return delta - HalfTickRate <= 0;
        }

        public static bool ShouldTickAdjustNoEarly(ref long tickTime)
        {
            long delta = GetDelta(tickTime);

            // Consider ticks to be late if the game loop can't keep up with its tick rate.
            // This is used to adjust services' tick time, allowing them to catch up.
            if (delta < -GameLoop.TickRate)
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
                KickPlayerToCharScreen(player);
            else
                entityOwner?.RemoveFromWorld();
        }

        public static void KickPlayerToCharScreen(GamePlayer player)
        {
            if (player.Client.ClientState != GameClient.eClientState.Playing)
                return;

            player.Client.ClientState = GameClient.eClientState.CharScreen;

            if ((eCharacterClass) player.CharacterClass.ID is eCharacterClass.Necromancer && player.HasShadeModel)
                player.Shade(false);

            player.Out.SendPlayerQuit(false);
            player.Quit(true);
            CraftingProgressMgr.FlushAndSaveInstance(player);
            player.SaveIntoDatabase();
        }
    }
}
