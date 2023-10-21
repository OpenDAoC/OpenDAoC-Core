using Core.AI.Brain;

namespace Core.GS;

public class Cronker : GameNpc
{
	public Cronker() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12329);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		//RespawnInterval = Util.Random(3600000, 7200000);

		CronkerBrain sbrain = new CronkerBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false; //load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}