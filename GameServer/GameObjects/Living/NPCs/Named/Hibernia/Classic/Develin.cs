using DOL.AI.Brain;

namespace DOL.GS;

#region Develin
public class Develin : GameEpicNPC
{
	public Develin() : base() { }

	public static int KillsRequireToSpawn = 20;
	public override bool AddToWorld()
	{
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc.Brain is DevelinBrain)
				return false;
		}
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159930);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		KillsRequireToSpawn = Util.Random(20, 40);
		//log.Warn("KillsRequireToSpawn = " + KillsRequireToSpawn);

		DevelinAdd.DevelinAddCount = 0;
		DevelinBrain sbrain = new DevelinBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}
#endregion Develin

#region Develin add
public class DevelinAdd : GameNpc
{
	public DevelinAdd() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164991);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		DevelinAddBrain sbrain = new DevelinAddBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public static int DevelinAddCount = 0;
	public override void Die(GameObject killer)
	{
		if (CurrentRegion.IsNightTime)
			++DevelinAddCount;
		base.Die(killer);
	}
}
#endregion Develin add
