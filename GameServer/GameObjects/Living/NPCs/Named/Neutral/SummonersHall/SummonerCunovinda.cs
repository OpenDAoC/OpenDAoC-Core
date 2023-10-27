﻿using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

public class SummonerCunovinda : GameEpicBoss
{
	public SummonerCunovinda() : base() { }
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
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18805);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		SummonerCunovindaBrain.RandomTarget = null;
		SummonerCunovindaBrain.CanCast = false;
		Faction = FactionMgr.GetFactionByID(187);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));
		IsCloakHoodUp = true;

		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TorsoArmor, 305, 43, 0, 0); //Slot,model,color,effect,extension
		template.AddNPCEquipment(EInventorySlot.ArmsArmor, 307, 43);
		template.AddNPCEquipment(EInventorySlot.LegsArmor, 306, 43);
		template.AddNPCEquipment(EInventorySlot.HandsArmor, 308, 43, 0, 0);
		template.AddNPCEquipment(EInventorySlot.FeetArmor, 309, 43, 0, 0);
		template.AddNPCEquipment(EInventorySlot.Cloak, 57, 54, 0, 0);
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 327, 43, 90, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		SummonerCunovindaBrain sbrain = new SummonerCunovindaBrain();
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

		npcs = WorldMgr.GetNPCsByNameFromRegion("Summoner Cunovinda", 248, (ERealm)0);
		if (npcs.Length == 0)
		{
			log.Warn("Summoner Cunovinda not found, creating it...");

			log.Warn("Initializing Summoner Cunovinda...");
			SummonerCunovinda OF = new SummonerCunovinda();
			OF.Name = "Summoner Cunovinda";
			OF.Model = 162;
			OF.Realm = 0;
			OF.Level = 75;
			OF.Size = 65;
			OF.CurrentRegionID = 248;//OF summoners hall

			OF.Strength = 5;
			OF.Intelligence = 200;
			OF.Piety = 200;
			OF.Dexterity = 200;
			OF.Constitution = 100;
			OF.Quickness = 80;
			OF.Empathy = 300;
			OF.BodyType = (ushort)EBodyType.Humanoid;
			OF.MeleeDamageType = EDamageType.Crush;
			OF.Faction = FactionMgr.GetFactionByID(187);
			OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));

			OF.X = 26023;
			OF.Y = 36132;
			OF.Z = 15998;
			OF.MaxDistance = 2000;
			OF.TetherRange = 1300;
			OF.MaxSpeedBase = 250;
			OF.Heading = 19;
			OF.IsCloakHoodUp = true;

			SummonerCunovindaBrain ubrain = new SummonerCunovindaBrain();
			ubrain.AggroLevel = 100;
			ubrain.AggroRange = 600;
			OF.SetOwnBrain(ubrain);
			OF.AddToWorld();
			OF.Brain.Start();
			OF.SaveIntoDatabase();
		}
		else
			log.Warn("Summoner Cunovinda exist ingame, remove it and restart server if you want to add by script code.");
	}
}