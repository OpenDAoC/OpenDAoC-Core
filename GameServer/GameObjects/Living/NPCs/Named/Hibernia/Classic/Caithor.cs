using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;

namespace Core.GS;

#region Caithor
public class Caithor : GameEpicNPC
{
	public Caithor() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Caithor Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 20;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 350;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}
	public override int MaxHealth
	{
		get { return 10000; }
	}
	public static bool RealCaithorUp = false;
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50023);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RealCaithorUp = true;

		SpawnDorochas();
		CaithorDorocha.DorochaKilled = 0;
		CaithorBrain sbrain = new CaithorBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
	private void SpawnDorochas()
	{
		for (int i = 0; i < 4; i++)
		{
			GameNpc npc = new GameNpc();
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160718);
			npc.LoadTemplate(npcTemplate);
			npc.X = X + Util.Random(-100, 100);
			npc.Y = Y + Util.Random(-100, 100);
			npc.Z = Z;
			npc.Heading = Heading;
			npc.CurrentRegion = CurrentRegion;
			npc.PackageID = "RealCaithorDorocha";
			npc.RespawnInterval = -1;
			npc.AddToWorld();
		}
	}
	public override void Die(GameObject killer)
    {
		RealCaithorUp = false;
		foreach(GameNpc npc in GetNPCsInRadius(8000))
        {
			if (npc.IsAlive && npc != null && npc.PackageID == "RealCaithorDorocha")
				npc.Die(this);
        }
		base.Die(killer);
    }
}
#endregion Caithor

#region Ghost of Caithor
public class GhostOfCaithor : GameEpicNPC
{
	public GhostOfCaithor() : base() { }
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 20;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 350;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}
	public override int MaxHealth
	{
		get { return 7000; }
	}
	#region Stats
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 400; }
	#endregion
	public static bool GhostCaithorUP = false;
	public override bool AddToWorld()
	{
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc.Brain is GhostOfCaithorBrain)
				return false;
		}
		Name = "Giant Caithor";
		Level = (byte)Util.Random(62, 65);
		Model = 339;
		Size = 160;
		MaxDistance = 3500;
		TetherRange = 4000;
		Flags = 0;
		LoadEquipmentTemplateFromDatabase("65b95161-a813-41cb-be0c-a57d132f8173");
		GhostCaithorUP = true;
		GhostOfCaithorBrain.CanDespawn = false;
		GhostOfCaithorBrain.despawnGiantCaithor = false;

		GhostOfCaithorBrain sbrain = new GhostOfCaithorBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		var despawnGiantCaithorTimer2 = TempProperties.GetProperty<EcsGameTimer>("giantcaithor_despawn2");
		if (despawnGiantCaithorTimer2 != null)
		{
			despawnGiantCaithorTimer2.Stop();
			TempProperties.RemoveProperty("giantcaithor_despawn2");
		}
		var despawnGiantCaithorTimer = TempProperties.GetProperty<EcsGameTimer>("giantcaithor_despawn");
		if (despawnGiantCaithorTimer != null)
		{
			despawnGiantCaithorTimer.Stop();
			TempProperties.RemoveProperty("giantcaithor_despawn");
		}
		GhostCaithorUP = false;
		SpawnCaithor();
        base.Die(killer);
    }
	private void SpawnCaithor()
	{
		Caithor npc = new Caithor();
		npc.X = 470547;
		npc.Y = 531497;
		npc.Z = 4984;
		npc.Heading = 3319;
		npc.CurrentRegion = CurrentRegion;
		npc.AddToWorld();
	}
}
#endregion Ghost of Caithor

#region Caithor far dorochas
public class CaithorDorocha : GameNpc
{
	public CaithorDorocha() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160718);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		EquipmentTemplateID = "65b95161-a813-41cb-be0c-a57d132f8173";

		CaithorDorochaBrain sbrain = new CaithorDorochaBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public static int DorochaKilled = 0;
    public override void Die(GameObject killer)
    {
		if(!Caithor.RealCaithorUp)
		++DorochaKilled;
        base.Die(killer);
    }
}
#endregion Caithor far dorochas