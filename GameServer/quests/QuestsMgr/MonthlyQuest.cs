using DOL.Database;

namespace DOL.GS.Quests
{
    public abstract class MonthlyQuest : BaseQuest
    {
        public abstract string QuestPropertyKey { get; set; }

        public MonthlyQuest() : base() { }

        public MonthlyQuest(GamePlayer questingPlayer) : base(questingPlayer) { }

        public MonthlyQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }

        public MonthlyQuest(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest) { }


        public override bool CheckQuestQualification(GamePlayer player)
        {
            lock (player.QuestLock)
            {
                return !player.QuestListFinished.Contains(this);
            }
        }

        public abstract void LoadQuestParameters();
        public abstract void SaveQuestParameters();
    }
}
