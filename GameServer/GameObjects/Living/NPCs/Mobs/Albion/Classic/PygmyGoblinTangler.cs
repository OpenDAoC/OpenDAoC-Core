using Core.AI.Brain;
using Core.GS.AI.Brains;

namespace Core.GS;

public class PygmyGoblinTangler : GameNpc
{
	public PygmyGoblinTangler() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12979);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		//RespawnInterval = Util.Random(3600000, 7200000);

		PygmyGoblinTanglerBrain sbrain = new PygmyGoblinTanglerBrain();
		if (NPCTemplate != null)
		{
			sbrain.AggroLevel = NPCTemplate.AggroLevel;
			sbrain.AggroRange = NPCTemplate.AggroRange;
		}
		BodyType = 0;
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}