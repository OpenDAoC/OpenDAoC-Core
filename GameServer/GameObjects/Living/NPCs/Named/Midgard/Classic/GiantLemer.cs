using DOL.AI.Brain;

namespace DOL.GS;

public class GiantLemer : GameNpc
{
	public GiantLemer() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50014);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		GiantLemerBrain sbrain = new GiantLemerBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is GiantLemerAddBrain)
				npc.RemoveFromWorld();
		}
		base.Die(killer);
    }
}

#region Giant lemer adds
public class GiantLemerAdd : GameNpc
{
	public GiantLemerAdd() : base() { }
	public override int MaxHealth
	{
		get { return 300; }
	}
	public override bool AddToWorld()
	{
		Name = "small rat";
		Level = (byte)Util.Random(13, 16);
		Model = 567;
		Size = 20;
		GiantLemerAddBrain sbrain = new GiantLemerAddBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
}
#endregion