using Core.AI.Brain;
using Core.GS.AI.Brains;

namespace Core.GS;

public class Anilius : GameNpc
{
	public Anilius() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12254);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		AniliusBrain sbrain = new AniliusBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public override void Die(GameObject killer)
	{
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc.IsAlive && npc != null && npc.Brain is AniliusAddBrain)
				npc.RemoveFromWorld();
		}
		base.Die(killer);
	}
}

#region Anilius adds
public class AniliusAdd : GameNpc
{
	public AniliusAdd() : base() { }
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12292);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		AniliusAddBrain sbrain = new AniliusAddBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
}
#endregion
