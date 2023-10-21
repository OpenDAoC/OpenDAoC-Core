using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS;

#region Gneiss
public class Gneiss : GameEpicBoss
{
	public Gneiss() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Gneiss Initializing...");
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
		get { return 30000; }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161448);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		GneissBrain sbrain = new GneissBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}
#endregion Gneiss

#region Gneiss add
public class GneissAdd : GameNpc
{
	public GneissAdd() : base()
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
		Model = 925;
		Name = "Gneiss's Piece";
		Level = 58;
		Size = (byte)Util.Random(50, 55);
		RespawnInterval = -1;
		RoamingRange = 200;

		LoadedFromScript = true;
		GneissAddBrain sbrain = new GneissAddBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Gneiss add