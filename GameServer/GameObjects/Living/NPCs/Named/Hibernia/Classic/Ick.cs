using Core.AI.Brain;
using Core.GS.AI.Brains;

namespace Core.GS;

public class Ick : GameNpc
{
	public Ick() : base() { }

	public override bool AddToWorld()
	{
		foreach(GameNpc npc in GetNPCsInRadius(5000))
        {
			if (npc != null && npc.IsAlive && npc.Brain is IckAddBrain)
				npc.RemoveFromWorld();
        }
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162371);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		IckBrain sbrain = new IckBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		SpawnWorms();
        base.Die(killer);
    }
	private void SpawnWorms()
    {
		for (int i = 0; i < 10; i++)
		{
			IckAdd npc = new IckAdd();
			npc.X = X + Util.Random(-100, 100);
			npc.Y = Y + Util.Random(-100, 100);
			npc.Z = Z;
			npc.Heading = Heading;
			npc.CurrentRegion = CurrentRegion;
			npc.AddToWorld();
		}
	}
	public override void DealDamage(AttackData ad)
	{
		if (ad != null && ad.AttackType == EAttackType.Spell && ad.Damage > 0)
			Health += ad.Damage;
		base.DealDamage(ad);
	}
}

public class IckAdd : GameNpc
{
	public IckAdd() : base() { }

	public override bool AddToWorld()
	{
		Name = "Ick worm";
		Level = (byte)Util.Random(17, 19);
		Model = 458;
		Size = 17;
		IckAddBrain sbrain = new IckAddBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
}