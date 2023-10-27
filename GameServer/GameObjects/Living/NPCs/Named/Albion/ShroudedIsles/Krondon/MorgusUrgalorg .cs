﻿using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

public class MorgusUrgalorg : GameEpicBoss
{
	public MorgusUrgalorg() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Morgus Urgalorg Initializing...");
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
	public override void StartAttack(GameObject target)
    {
    }
    public override bool AddToWorld()
	{
		Model = 919;
		Level = (byte)(Util.Random(72, 75));
		Name = "Morgus Urgalorg";
		Size = 120;

		Strength = 280;
		Dexterity = 200;
		Constitution = 100;
		Quickness = 80;
		Piety = 200;
		Intelligence = 200;
		Charisma = 200;
		Empathy = 400;

		MaxSpeedBase = 250;
		MaxDistance = 3500;
		TetherRange = 3800;
		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		MorgusUrgalorgBrain sbrain = new MorgusUrgalorgBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}