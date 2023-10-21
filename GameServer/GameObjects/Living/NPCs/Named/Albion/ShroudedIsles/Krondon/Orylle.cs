using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS;

public class Orylle : GameEpicBoss
{
	public Orylle() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Orylle Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 30;// dmg reduction for melee dmg
			case EDamageType.Crush: return 30;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 30;// dmg reduction for melee dmg
			default: return 40;// dmg reduction for rest resists
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
		get { return 40000; }
	}
	public override bool AddToWorld()
	{
		Model = 861;
		Level = 83;
		Name = "Orylle";
		Size = 175;
		ParryChance = 70;

		Strength = 300;
		Dexterity = 150;
		Constitution = 100;
		Quickness = 80;
		Piety = 200;
		Intelligence = 200;
		Charisma = 200;
		Empathy = 400;

		MaxSpeedBase = 250;
		MaxDistance = 3500;
		TetherRange = 3800;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		OrylleBrain sbrain = new OrylleBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}