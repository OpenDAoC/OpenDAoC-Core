using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

#region Green Maw
public class GreenMaw : GameEpicNPC
{
	public GreenMaw() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Green Maw Initializing...");
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
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
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
	public override bool AddToWorld()
	{
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc.Brain is GreenMawBrain)
				return false;
		}
		foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
		{
			if (npc != null && npc.IsAlive && npc.Brain is GreenMawAddBrain)
				npc.RemoveFromWorld();
		}
		foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
		{
			if (npc != null && npc.IsAlive && npc.Brain is GreenMawAdd2Brain)
				npc.RemoveFromWorld();
		}
		foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
		{
			if (npc != null && npc.IsAlive && npc.Brain is GreenMawAdd3Brain)
				npc.RemoveFromWorld();
		}
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50022);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		GreenMawAdd.GreenMawRedCount = 0;
		GreenMawAdd2.GreenMawOrangeCount = 0;

		RespawnInterval = ServerProperty.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		GreenMawBrain sbrain = new GreenMawBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public override void Die(GameObject killer)
	{
		SpawnCopies();
		base.Die(killer);
	}
	private void SpawnCopies()
	{
		for (int i = 0; i < 3; i++)
		{
			GreenMawAdd npc = new GreenMawAdd();
			npc.X = X + Util.Random(-50, 50);
			npc.Y = Y + Util.Random(-50, 50);
			npc.Z = Z;
			npc.Heading = Heading;
			npc.CurrentRegion = CurrentRegion;
			npc.AddToWorld();
		}
	}
}
#endregion Green Maw

#region Green maw Copies Red
public class GreenMawAdd : GameNpc
{
	public GreenMawAdd() : base() { }
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
		return 250;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.10;
	}
	public override int MaxHealth
	{
		get { return 5000; }
	}
	#region Stats
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 200; }
	#endregion
	public override bool AddToWorld()
	{
		Name = "Part of Green Maw";
		Level = (byte)Util.Random(58,60);
		Model = 136;
		Size = 120;
		GreenMawAddBrain sbrain = new GreenMawAddBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
	public static int GreenMawRedCount = 0;
    public override void Die(GameObject killer)
    {
		++GreenMawRedCount;
		if (GreenMawRedCount >= 3)
			SpawnCopies();
		base.Die(killer);
    }
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	private void SpawnCopies()
	{
		for (int i = 0; i < 4; i++)
		{
			GreenMawAdd2 npc = new GreenMawAdd2();
			npc.X = X + Util.Random(-50, 50);
			npc.Y = Y + Util.Random(-50, 50);
			npc.Z = Z;
			npc.Heading = Heading;
			npc.CurrentRegion = CurrentRegion;
			npc.AddToWorld();
		}
	}
}
#endregion Green maw Copies Red

#region Green maw Copies Orange
public class GreenMawAdd2 : GameNpc
{
	public GreenMawAdd2() : base() { }
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
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.10;
	}
	public override int MaxHealth
	{
		get { return 3000; }
	}
	#region Stats
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 150; }
	#endregion
	public override bool AddToWorld()
	{
		Name = "Part of Green Maw";
		Level = (byte)Util.Random(53, 55);
		Model = 136;
		Size = 95;
		GreenMawAdd2Brain sbrain = new GreenMawAdd2Brain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
	public static int GreenMawOrangeCount = 0;
	public override void Die(GameObject killer)
	{
		++GreenMawOrangeCount;
		if (GreenMawOrangeCount >= 4)
			SpawnCopies();
		base.Die(killer);
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	private void SpawnCopies()
	{
		for (int i = 0; i < 2; i++)
		{
			GreenMawAdd3 npc = new GreenMawAdd3();
			npc.X = X + Util.Random(-50, 50);
			npc.Y = Y + Util.Random(-50, 50);
			npc.Z = Z;
			npc.Heading = Heading;
			npc.CurrentRegion = CurrentRegion;
			npc.AddToWorld();
		}
	}
}
#endregion Green maw Copies Orange

#region Green maw Copies Yellow
public class GreenMawAdd3 : GameNpc
{
	public GreenMawAdd3() : base() { }
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
		return 150;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.10;
	}
	public override int MaxHealth
	{
		get { return 2500; }
	}
	#region Stats
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 150; }
	#endregion
	public override bool AddToWorld()
	{
		Name = "Part of Green Maw";
		Level = 50;
		Model = 136;
		Size = 70;
		GreenMawAdd3Brain sbrain = new GreenMawAdd3Brain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
}
#endregion Green maw Copies Yellow