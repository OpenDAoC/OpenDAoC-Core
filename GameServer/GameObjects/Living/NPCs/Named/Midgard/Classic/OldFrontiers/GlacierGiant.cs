using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

public class GlacierGiant : GameEpicBoss
{
	public GlacierGiant() : base() { }
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161360);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		GlacierGiantBrain.Clear_List = false;
		GlacierGiantBrain.RandomTarget = null;

		GlacierGiantBrain sbrain = new GlacierGiantBrain();
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

		npcs = WorldMgr.GetNPCsByNameFromRegion("Glacier Giant", 100, (ERealm)0);
		if (npcs.Length == 0)
		{
			log.Warn("Glacier Giant not found, creating it...");

			log.Warn("Initializing Glacier Giant...");
			GlacierGiant OF = new GlacierGiant();
			OF.Name = "Glacier Giant";
			OF.Model = 1384;
			OF.Realm = 0;
			OF.Level = 80;
			OF.Size = 255;
			OF.CurrentRegionID = 100;//OF odins gate

			OF.Strength = 5;
			OF.Intelligence = 150;
			OF.Piety = 150;
			OF.Dexterity = 200;
			OF.Constitution = 100;
			OF.Quickness = 125;
			OF.Empathy = 300;
			OF.BodyType = (ushort)EBodyType.Magical;
			OF.MeleeDamageType = EDamageType.Slash;

			OF.X = 651517;
			OF.Y = 625897;
			OF.Z = 5320;
			OF.MaxDistance = 5500;
			OF.TetherRange = 5600;
			OF.MaxSpeedBase = 280;
			OF.Heading = 4003;

			GlacierGiantBrain ubrain = new GlacierGiantBrain();
			ubrain.AggroLevel = 0;
			ubrain.AggroRange = 600;
			OF.SetOwnBrain(ubrain);
			OF.AddToWorld();
			OF.Brain.Start();
			OF.SaveIntoDatabase();
		}
		else
			log.Warn("Glacier Giant exist ingame, remove it and restart server if you want to add by script code.");
	}
}