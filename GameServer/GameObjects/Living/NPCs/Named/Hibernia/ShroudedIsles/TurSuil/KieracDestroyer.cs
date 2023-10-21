using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Kierac the Destroyer
public class KieracDestroyer : GameEpicBoss
{
	public KieracDestroyer() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Kierac the Destroyer Initializing...");
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
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (IsOutOfTetherRange)
			{
				if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
					|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
					|| damageType == EDamageType.Slash)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GameSummonedPet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
			}
			else//take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
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
	public override bool AddToWorld()
	{
		Model = 826;
		Level = 69;
		Name = "Kierac the Destroyer";
		Size = 180;
		ParryChance = 50;

		Strength = 550;
		Dexterity = 200;
		Constitution = 100;
		Quickness = 80;
		Piety = 200;
		Intelligence = 200;
		Empathy = 400;

		MaxSpeedBase = 250;
		MaxDistance = 2500;
		TetherRange = 1800;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 841, 0, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		VisibleActiveWeaponSlots = 34;
		MeleeDamageType = EDamageType.Slash;
		Faction = FactionMgr.GetFactionByID(93);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
		SpawnMoP = false;

		KieracDestroyerBrain sbrain = new KieracDestroyerBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		bool success = base.AddToWorld();
		if (success)
        {
			foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.Brain is MasterOfPainBrain)
					{
						npc.RemoveFromWorld();
					}
				}
			}
		}
		return success;
	}
	private static bool SpawnMoP=false;
	public override void Die(GameObject killer)
	{
		if (SpawnMoP == false)
		{
			SpawnMasterOfPain();
			SpawnMoP = true;
		}
		base.Die(killer);
	}
	public void SpawnMasterOfPain()
	{
			MasterOfPain Add1 = new MasterOfPain();
			Add1.X = 33971;
			Add1.Y = 20939;
			Add1.Z = 11611;
			Add1.CurrentRegion = CurrentRegion;
			Add1.Heading = 39;
			Add1.RespawnInterval = -1;
			Add1.AddToWorld();
	}
}
#endregion Kierac the Destroyer

#region Master of Pain
public class MasterOfPain : GameEpicBoss
{
	public MasterOfPain() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Master of Pain Initializing...");
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
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (IsOutOfTetherRange)
			{
				if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
					|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
					|| damageType == EDamageType.Slash)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GameSummonedPet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
			}
			else//take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
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
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163806);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(93);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
		RespawnInterval = -1;
		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 442, 0, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		VisibleActiveWeaponSlots = 34;
		MeleeDamageType = EDamageType.Crush;

		MasterOfPainBrain sbrain = new MasterOfPainBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion Master of Pain