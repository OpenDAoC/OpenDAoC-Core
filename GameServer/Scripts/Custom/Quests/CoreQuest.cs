using Core.Database;

namespace Core.GS.Quests
{
    public abstract class CoreQuest : BaseQuest
    {
        public abstract string QuestPropertyKey { get; set; }

        public CoreQuest() : base() { }

        public CoreQuest(GamePlayer questingPlayer) : base(questingPlayer) { }

        public CoreQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }

        public CoreQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest) { }

        public override bool CheckQuestQualification(GamePlayer player)
        {
            return !player.HasFinishedQuest(this);
        }

        public abstract void LoadQuestParameters();
        public abstract void SaveQuestParameters();
    }
}
