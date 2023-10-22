using Core.GS.AI.Brains;
using Core.GS.GameUtils;

namespace Core.GS;

public class Vagdush : GameNpc
{
	public Vagdush() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12742);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		VagdushBrain sbrain = new VagdushBrain();
		
		if (NPCTemplate != null)
		{
			sbrain.AggroLevel = NPCTemplate.AggroLevel;
			sbrain.AggroRange = NPCTemplate.AggroRange;
		}
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
	public override void Die(GameObject killer)
	{
		switch (Util.Random(1, 2))
		{
			case 1:
				SpawnPoint.X = 421759;
				SpawnPoint.Y = 650509;
				SpawnPoint.Z = 3933;
				Heading = 3842;
				break;
			case 2:
				SpawnPoint.X = 421716;
				SpawnPoint.Y = 658478;
				SpawnPoint.Z = 4196;
				Heading = 2164;
				break;
		}
		base.Die(killer);
	}
}