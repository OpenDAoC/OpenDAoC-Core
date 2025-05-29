using System;
using System.Collections.Generic;
using System.Reflection;

namespace DOL.GS
{
    public static class ServiceUtils
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
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

        public static void HandleServiceException<T>(Exception exception, string serviceName, T entity, GameObject entityOwner) where T : class, IServiceObject
        {
            if (entity != null)
                ServiceObjectStore.Remove(entity);

            List<string> logMessages = [$"Critical error encountered in {serviceName}: {exception}"];

            // Define the actions and log messages.
            Action action = entityOwner switch
            {
                GamePlayer player => () =>
                {
                    logMessages.Add($"Calling {nameof(KickPlayerToCharScreen)} with ({nameof(entityOwner)}: {player})");
                    KickPlayerToCharScreen(player);
                },
                not null => () =>
                {
                    logMessages.Add($"Calling {nameof(entityOwner.RemoveFromWorld)} with ({nameof(entityOwner)}: {entityOwner})");
                    entityOwner.RemoveFromWorld();
                },
                _ => () => logMessages.Add($"No other action performed ({nameof(entityOwner)}: null)")
            };

            // Log error messages before executing the action (if any).
            if (log.IsErrorEnabled)
                log.Error(string.Join(Environment.NewLine, logMessages));

            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Couldn't invoke {nameof(action)} in {nameof(HandleServiceException)}", e);
            }
        }

        public static void KickPlayerToCharScreen(GamePlayer player)
        {
            if (player.Client.ClientState is not GameClient.eClientState.Playing)
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
