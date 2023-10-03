using DOL.Database;

namespace DOL.GS.Quests
{
    public abstract class WeeklyQuest : BaseQuest
    {
        public abstract string QuestPropertyKey { get; set; }

        public WeeklyQuest() : base() { }

        public WeeklyQuest(GamePlayer questingPlayer) : base(questingPlayer) { }

        public WeeklyQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }

        public WeeklyQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest) { }

        public override bool CheckQuestQualification(GamePlayer player)
        {
            return !player.HasFinishedQuest(this);
        }

        public abstract void LoadQuestParameters();
        public abstract void SaveQuestParameters();
    }
}
