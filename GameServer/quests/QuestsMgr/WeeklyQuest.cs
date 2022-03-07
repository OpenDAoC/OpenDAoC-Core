using DOL.Database;

namespace DOL.GS.Quests;

public class WeeklyQuest : BaseQuest
{
    public override bool CheckQuestQualification(GamePlayer player)
    {
        if (player.QuestListFinished.Contains(this))
            return false;
        
        return true;
    }
    
    public WeeklyQuest() : base()
    {
    }
    
    public WeeklyQuest(GamePlayer questingPlayer) : base(questingPlayer)
    {
    }

    public WeeklyQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
    {
    }

    public WeeklyQuest(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
    {
    }
}