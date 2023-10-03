using DOL.Database;

namespace DOL.GS.Quests
{
    public abstract class AtlasQuest : BaseQuest
    {
        public abstract string QuestPropertyKey { get; set; }

        public AtlasQuest() : base() { }

        public AtlasQuest(GamePlayer questingPlayer) : base(questingPlayer) { }

        public AtlasQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }

        public AtlasQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest) { }

        public override bool CheckQuestQualification(GamePlayer player)
        {
            return !player.HasFinishedQuest(this);
        }

        public abstract void LoadQuestParameters();
        public abstract void SaveQuestParameters();
    }
}
