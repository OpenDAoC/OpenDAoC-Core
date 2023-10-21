using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS;

public class SummonerRoesia : GameEpicBoss
{
	public SummonerRoesia() : base() { }
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40; // dmg reduction for melee dmg
			case EDamageType.Crush: return 40; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
			default: return 70; // dmg reduction for rest resists
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
		get { return 100000; }
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
						truc.Out.SendMessage(Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
			}
			else//take dmg
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18804);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		SummonerRoesiaBrain.RandomTarget = null;
		SummonerRoesiaBrain.CanCast = false;
		Faction = FactionMgr.GetFactionByID(187);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));
		IsCloakHoodUp = true;

		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TorsoArmor, 139, 43, 0, 0); //Slot,model,color,effect,extension
		template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 43);
		template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 43);
		template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 43, 0, 0);
		template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 43, 0, 0);
		template.AddNPCEquipment(EInventorySlot.Cloak, 57, 66, 0, 0);
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 19, 43, 94, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		SummonerRoesiaBrain sbrain = new SummonerRoesiaBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		GameNpc[] npcs;

		npcs = WorldMgr.GetNPCsByNameFromRegion("Summoner Roesia", 248, (ERealm)0);
		if (npcs.Length == 0)
		{
			log.Warn("Summoner Roesia not found, creating it...");

			log.Warn("Initializing Summoner Roesia...");
			SummonerRoesia OF = new SummonerRoesia();
			OF.Name = "Summoner Roesia";
			OF.Model = 6;
			OF.Realm = 0;
			OF.Level = 75;
			OF.Size = 65;
			OF.CurrentRegionID = 248;//OF summoners hall

			OF.Strength = 5;
			OF.Intelligence = 200;
			OF.Piety = 200;
			OF.Dexterity = 200;
			OF.Constitution = 100;
			OF.Quickness = 125;
			OF.Empathy = 300;
			OF.BodyType = (ushort)EBodyType.Humanoid;
			OF.MeleeDamageType = EDamageType.Crush;
			OF.Faction = FactionMgr.GetFactionByID(187);
			OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));

			OF.X = 34577;
			OF.Y = 31371;
			OF.Z = 15998;
			OF.MaxDistance = 2000;
			OF.TetherRange = 1300;
			OF.MaxSpeedBase = 250;
			OF.Heading = 19;
			OF.IsCloakHoodUp = true;

			SummonerRoesiaBrain ubrain = new SummonerRoesiaBrain();
			ubrain.AggroLevel = 100;
			ubrain.AggroRange = 600;
			OF.SetOwnBrain(ubrain);
			OF.AddToWorld();
			OF.Brain.Start();
			OF.SaveIntoDatabase();
		}
		else
			log.Warn("Summoner Roesia exist ingame, remove it and restart server if you want to add by script code.");
	}
}