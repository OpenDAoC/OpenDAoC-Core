using Core.GS.AI;

namespace Core.GS;

public class Eques : GameNpc
{
	public Eques() : base() { }
	public override void ReturnToSpawnPoint(short speed)
	{
		return;
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12907);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		MaxDistance = 0;
		TetherRange = 0;

		EquesBrain.point1check = false;
		EquesBrain.point2check = false;
		EquesBrain.point3check = false;
		EquesBrain.point4check = false;
		EquesBrain.point5check = false;
		EquesBrain.point6check = false;
		EquesBrain.point7check = false;
		EquesBrain.point8check = false;

		EquesBrain sbrain = new EquesBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}