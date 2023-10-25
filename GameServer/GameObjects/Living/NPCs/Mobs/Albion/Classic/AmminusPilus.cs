using Core.GS.AI;
using Core.GS.ECS;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS;

public class AmminusPilus : GameNpc
{
	public AmminusPilus() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12888);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		SpawnPilusFury();

		AmminusPilusBrain sbrain = new AmminusPilusBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;
		SaveIntoDatabase();
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
		}
		return success;
	}

	protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellEffectAnimation(this, this, 5920, 0, false, 0x01);

			return 2000;
		}

		return 0;
	}

	public override void Die(GameObject killer)
    {
		foreach(GameNpc npc in GetNPCsInRadius(5000))
        {
			if (npc.IsAlive && npc != null && npc.Brain is PilusFuryBrain)
				npc.RemoveFromWorld();
        }
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc.IsAlive && npc != null && npc.Brain is PilusAddBrain)
				npc.RemoveFromWorld();
		}
		base.Die(killer);
    }
	private void SpawnPilusFury()
    {
		foreach (GameNpc fury in GetNPCsInRadius(5000))
		{
			if (fury.Brain is PilusFuryBrain)
				return;
		}
		PilusFury npc = new PilusFury();
		npc.X = 33072;
		npc.Y = 43263;
		npc.Z = 15360;
		npc.Heading = 3003;
		npc.CurrentRegion = CurrentRegion;
		npc.AddToWorld();
	}
}

#region Pilus adds
public class PilusAdd : GameNpc
{
	public PilusAdd() : base() { }
    #region Stats
    public override short Constitution { get => base.Constitution; set => base.Constitution = 100; }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 180; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 100; }
	#endregion
	public override bool AddToWorld()
	{
		Name = "centurio princeps preatorii";
		Level = (byte)Util.Random(37, 39);
		Size = (byte)Util.Random(50, 60);
		Model = 108;
		Race = 2009;
		BodyType = 11;

		PilusAddBrain sbrain = new PilusAddBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
}
#endregion