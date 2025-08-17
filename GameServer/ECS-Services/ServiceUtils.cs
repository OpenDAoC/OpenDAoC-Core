using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace DOL.GS
{
    public static class ServiceUtils
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static long HalfTickDuration => GameLoop.TickDuration / 2;

        public static bool ShouldTick(long tickTime)
        {
            // This method checks if the current game loop time is within the range of the tick time.
            // It allows for a half-tick rate tolerance to ensure that ticks are processed the closest to the intended time.
            // If this is a recurring tick, the tick time will need to be updated by the service that uses it. There are two ways to do this:
            // 1. Increment the tick time by the tick interval (prevents drifting).
            // 2. Set the tick time to the current game loop time then add the tick interval (prevents issues if tick time isn't initialized properly).
            // For most services, drifting is inconsequential, so the second option is preferred.
            return tickTime - GameLoop.GameLoopTime - HalfTickDuration <= 0;
        }

        public static void HandleServiceException<T>(Exception exception, string serviceName, T entity, GameObject entityOwner) where T : class, IServiceObject
        {
            if (entity != null)
                ServiceObjectStore.Remove(entity);

            List<string> logMessages = [$"Critical error encountered in {serviceName}: {exception}"];

            Action action;
            string actionMessage;

            switch (entityOwner)
            {
                case GamePlayer player:
                {
                    action = () => KickPlayerToCharScreen(player);
                    actionMessage = $"Calling {nameof(KickPlayerToCharScreen)} with ({nameof(entityOwner)}: {player})";
                    break;
                }
                case not null:
                {
                    action = () => entityOwner.RemoveFromWorld();
                    actionMessage = $"Calling {nameof(entityOwner.RemoveFromWorld)} with ({nameof(entityOwner)}: {entityOwner})";
                    break;
                }
                default:
                {
                    action = static () => { }; // No-op
                    actionMessage = $"No other action performed ({nameof(entityOwner)}: null)";
                    break;
                }
            }

            logMessages.Add(actionMessage);

            if (log.IsErrorEnabled)
                log.Error(string.Join(Environment.NewLine, logMessages));

            try
            {
                action();
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

        public static void ScheduleActionAfterTask<T>(Task task, ContinuationAction<T> continuation, T argument, GameObject owner)
        {
            task.ContinueWith(ContinueWithHandler, new ContinuationActionState<T>(owner, continuation, argument));

            static void ContinueWithHandler(Task task, object stateObj)
            {
                if (task.IsFaulted)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Async task failed", task.Exception);

                    return;
                }

                GameLoopService.PostBeforeTick(ActionHandler, stateObj);

                static void ActionHandler(object stateObj)
                {
                    ContinuationActionState<T> state = (ContinuationActionState<T>) stateObj;
                    state.ContinuationAction(state.Argument);
                }
            }
        }

        public delegate bool ContinuationAction<T>(T argument);

        private class ContinuationActionState<T>
        {
            public GameObject Owner { get; }
            public ContinuationAction<T> ContinuationAction { get; }
            public T Argument { get; }

            public ContinuationActionState(GameObject owner, ContinuationAction<T> continuationAction, T argument)
            {
                Owner = owner;
                ContinuationAction = continuationAction;
                Argument = argument;
            }
        }
    }
}
