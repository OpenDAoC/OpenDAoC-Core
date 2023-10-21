using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;

namespace Core.GS.Scripts;

public class Hamar : GameEpicBoss
{
	public Hamar() : base()
	{ }
	
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9910);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
	
		// 1h Axe, left + main
		GameNpcInventoryTemplate hamarTemp = new GameNpcInventoryTemplate();
		hamarTemp.AddNPCEquipment(EInventorySlot.RightHandWeapon, 319);
		hamarTemp.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 319);
		Inventory = hamarTemp.CloseTemplate();
		
		// undead
		BodyType = 11;
		MeleeDamageType = EDamageType.Slash;
		Faction = FactionMgr.GetFactionByID(779);
		
		Flags |= ENpcFlags.GHOST;
		// double-wielded
		VisibleActiveWeaponSlots = 16;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		ScalingFactor = 40;
		base.SetOwnBrain(new HamarBrain());
		LoadedFromScript = false; //load from database
		SaveIntoDatabase();
		base.AddToWorld();
		
		return true;
	}
	
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
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
	public override int AttackRange
	{
		get
		{
			return 350;
		}
		set
		{ }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	
	/// <summary>
	/// Return to spawn point, Hamar can't be attacked while it's
	/// on it's way.
	/// </summary>
	public override void ReturnToSpawnPoint(short speed)
	{
		base.ReturnToSpawnPoint(MaxSpeed);
	}

	public override void OnAttackedByEnemy(AttackData ad)
	{
		if (IsReturningToSpawnPoint)
			return;

		base.OnAttackedByEnemy(ad);
	}

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Hamar NPC Initializing...");
	}
}