using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS;

public class Rift : GameEpicBoss
{
	public Rift() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Rift Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20; // dmg reduction for melee dmg
			case EDamageType.Crush: return 20; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
			default: return 30; // dmg reduction for rest resists
		}
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
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
		{
			if (npc != null)
			{
				if (npc.IsAlive && npc.Brain is MorkenhetBrain)
				{
					npc.RemoveFromWorld();
				}
			}
		}
		base.Die(killer);
    }
    public override bool AddToWorld()
	{
		Name = "Trollkarl Bonchar";
		Model = 874;
		Level = 65;
		Size = 81;
		MaxSpeedBase = 250;
		Strength = 320;
		Dexterity = 150;
		Constitution = 100;
		Quickness = 100;
		Piety = 150;
		Intelligence = 150;
		Empathy = 300;
		MaxDistance = 3500;
		TetherRange = 3500;
		MeleeDamageType = EDamageType.Crush;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(150);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
		RiftBrain.IsValkyn = false;

		RiftBrain sbrain = new RiftBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}

#region Rift adds
public class Morkenhet : GameNpc
{
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 35; // dmg reduction for melee dmg
			case EDamageType.Crush: return 35; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
			default: return 35; // dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
    public override short Strength { get => base.Strength; set => base.Strength = 350; }
    public override int MaxHealth
	{
		get { return 5000; }
	}
	public override bool AddToWorld()
	{
		Model = 929;
		Name = "morkenhet";
		Strength = 550;
		Dexterity = 200;
		Quickness = 100;
		Constitution = 100;
		RespawnInterval = -1;
		MaxSpeedBase = 225;

		Size = (byte)Util.Random(50, 60);
		Level = (byte)Util.Random(58, 62);
		Faction = FactionMgr.GetFactionByID(150);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
		MorkenhetBrain add = new MorkenhetBrain();
		SetOwnBrain(add);
		base.AddToWorld();
		return true;
	}
}
#endregion Rift adds