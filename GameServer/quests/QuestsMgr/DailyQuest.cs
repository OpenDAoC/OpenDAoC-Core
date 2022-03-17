using DOL.Database;

namespace DOL.GS.Quests;

public abstract class DailyQuest : BaseQuest
{
    public abstract string QuestPropertyKey { get; set; }
    public override bool CheckQuestQualification(GamePlayer player)
    {
        if (player.QuestListFinished.Contains(this))
            return false;
        
        return true;
    }
    
    public DailyQuest() : base()
    {
    }
    
    public DailyQuest(GamePlayer questingPlayer) : base(questingPlayer)
    {
    }

    public DailyQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
    {
    }

    public DailyQuest(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
    {
    }

    public abstract void LoadQuestParameters();
    public abstract void SaveQuestParameters();

}