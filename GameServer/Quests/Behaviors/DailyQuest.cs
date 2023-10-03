using DOL.Database;

namespace DOL.GS.Quests
{
    public abstract class DailyQuest : BaseQuest
    {
        public abstract string QuestPropertyKey { get; set; }

        public DailyQuest() : base() { }

        public DailyQuest(GamePlayer questingPlayer) : base(questingPlayer) { }

        public DailyQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }

        public DailyQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest) { }

        public override bool CheckQuestQualification(GamePlayer player)
        {
            return !player.HasFinishedQuest(this);
        }

        public abstract void LoadQuestParameters();
        public abstract void SaveQuestParameters();
    }
}
