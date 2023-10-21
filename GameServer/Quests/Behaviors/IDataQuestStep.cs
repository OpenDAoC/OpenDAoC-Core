namespace Core.GS.Quests
{
	public interface IDataQuestStep
	{
		bool Execute(DataQuest dataQuest, GamePlayer player, int step, EStepCheckType stepCheckType);
	}
}