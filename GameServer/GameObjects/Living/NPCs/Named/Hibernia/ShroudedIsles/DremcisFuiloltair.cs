using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

#region Dremcis Fuiloltair
public class DremcisFuiloltair : GameEpicBoss
{
	public DremcisFuiloltair() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Dremcis Fuiloltair Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 30;// dmg reduction for rest resists
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
		get { return 30000; }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160146);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		DremcisFuiloltairBrain.CanSpawnStag = false;

		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		DremcisFuiloltairBrain sbrain = new DremcisFuiloltairBrain();
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
			if (npc != null && npc.IsAlive && npc.Brain is FuilslathachBrain)
				npc.RemoveFromWorld();
		}
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is BeomarbhanBrain)
				npc.Die(this);
		}
		base.Die(killer);
    }
}
#endregion Dremcis Fuiloltair

#region Stag pet
public class Beomarbhan : GameEpicBoss
{
	public Beomarbhan() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Beomarbhan Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 70;// dmg reduction for rest resists
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
		get { return 30000; }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158366);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = -1;
		BeomarbhanBrain sbrain = new BeomarbhanBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion Stag pet

#region Dremcis adds
public class Fuilslathach : GameNpc
{
	public Fuilslathach() : base()
	{
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 15;// dmg reduction for melee dmg
			case EDamageType.Crush: return 15;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 15;// dmg reduction for melee dmg
			default: return 15;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int MaxHealth
	{
		get { return 2500; }
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
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 150; }
	public override bool AddToWorld()
	{
		Model = 932;
		Name = "fuilslathach";
		Level = (byte)Util.Random(51, 59);
		Size = (byte)Util.Random(15, 20);
		RespawnInterval = -1;
		RoamingRange = 200;

		LoadedFromScript = true;
		FuilslathachBrain sbrain = new FuilslathachBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Dremcis adds