using System;
using System.Collections.Generic;
using DOL.GS.Quests;
using static DOL.GS.RolloverSchedulerService;

namespace DOL.GS
{
    public sealed class PeriodicQuestService : GameServiceBase
    {
        public static PeriodicQuestService Instance { get; }

        private static readonly List<QuestRegistration> _registrations = new();

        static PeriodicQuestService()
        {
            Instance = new();
        }

        private PeriodicQuestService()
        {
            RegisterQuest<Quests.DailyQuest>(IntervalKey.Daily);
            RegisterQuest<Quests.WeeklyQuest>(IntervalKey.Weekly);
            RegisterQuest<Quests.MonthlyQuest>(IntervalKey.Monthly);
        }

        public static void Initialize() { }

        public override void Tick()
        {
            // Core service duties are still processed, but delegated to the rollover scheduler.
            ProcessPostedActionsParallel();
        }

        public static void OnPlayerJoin(GamePlayer player)
        {
            DateTime login = player.PreviousLoginDate;
            uint triggeredMask = 0;

            // Evaluate which intervals need resetting.
            for (int i = 0; i < _registrations.Count; i++)
            {
                QuestRegistration reg = _registrations[i];

                if (login < RolloverSchedulerService.Instance.GetLastRollover(reg.Interval))
                    triggeredMask |= reg.BitMask;
            }

            if (triggeredMask == 0)
                return;

            player.RemoveFinishedQuests(static (quest, mask) =>
            {
                for (int i = 0; i < _registrations.Count; i++)
                {
                    QuestRegistration reg = _registrations[i];

                    if ((mask & reg.BitMask) != 0 && reg.IsMatch(quest))
                        return true;
                }

                return false;
            }, triggeredMask);
        }

        private static void RegisterQuest<T>(IntervalKey intervalKey)
        {
            // Supports up to 32 different intervals.
            RolloverSchedulerService.Instance.Subscribe(intervalKey, ResetQuests<T>);
            _registrations.Add(new(intervalKey, 1u << _registrations.Count, static quest => quest is T));
        }

        private static void ResetQuests<T>()
        {
            List<GamePlayer> players = ClientService.Instance.GetPlayers();
            GameLoop.ExecuteForEach(players, players.Count - 1, Action);

            static void Action(GamePlayer player)
            {
                player.RemoveFinishedQuests(static quest => quest is T);
            }
        }

        private record struct QuestRegistration(IntervalKey Interval, uint BitMask, Func<AbstractQuest, bool> IsMatch) { }
    }
}
