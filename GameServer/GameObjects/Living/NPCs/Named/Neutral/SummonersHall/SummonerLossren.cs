using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS;

#region Summoner Lossren
public class SummonerLossren : GameEpicBoss
{
	public SummonerLossren() : base() { }
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
			if (IsOutOfTetherRange)//dont take any dmg if is too far away from spawn point
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
				if (source is GameSummonedPet)
				{
					base.TakeDamage(source, damageType, 5, 5);
				}
				else
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18806);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		Faction = FactionMgr.GetFactionByID(206);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
		IsCloakHoodUp = true;
		SummonerLossrenBrain.IsCreatingSouls = false;
		TorturedSouls.TorturedSoulKilled = 0;

		SummonerLossrenBrain sbrain = new SummonerLossrenBrain();
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

		npcs = WorldMgr.GetNPCsByNameFromRegion("Summoner Lossren", 248, (ERealm)0);
		if (npcs.Length == 0)
		{
			log.Warn("Summoner Lossren not found, creating it...");

			log.Warn("Initializing Summoner Lossren...");
			SummonerLossren OF = new SummonerLossren();
			OF.Name = "Summoner Lossren";
			OF.Model = 343;
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
			OF.Faction = FactionMgr.GetFactionByID(206);
			OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

			OF.X = 39273;
			OF.Y = 41166;
			OF.Z = 15998;
			OF.MaxDistance = 2000;
			OF.TetherRange = 1300;
			OF.MaxSpeedBase = 250;
			OF.Heading = 967;
			OF.IsCloakHoodUp = true;

			SummonerLossrenBrain ubrain = new SummonerLossrenBrain();
			ubrain.AggroLevel = 100;
			ubrain.AggroRange = 600;
			OF.SetOwnBrain(ubrain);
			OF.AddToWorld();
			OF.Brain.Start();
			OF.SaveIntoDatabase();
		}
		else
			log.Warn("Summoner Lossren exist ingame, remove it and restart server if you want to add by script code.");
	}
}
#endregion Summoner Lossren

#region Lossren adds
public class TorturedSouls : GameNpc
{
	public override int MaxHealth
	{
		get { return 600; }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 25;// dmg reduction for melee dmg
			case EDamageType.Crush: return 25;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 25;// dmg reduction for melee dmg
			default: return 25;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 500;
	}
	public static int TorturedSoulCount = 0;
	public static int TorturedSoulKilled = 0;
	public override void Die(GameObject killer)
    {
		--TorturedSoulCount;
		++TorturedSoulKilled;
        base.Die(killer);
    }
    public override void DropLoot(GameObject killer)//dont drop loot
    {
    }
    List<string> soul_names = new List<string>()
	{
		"Aphryx's Tortured Soul","Arus's Tortured Soul","Briandina's Tortured Soul","Dwuanne's Tortured Soul",
		"Feraa's Tortured Soul","Klose's Tortured Soul","Lonar's Tortured Soul","Threepwood's Tortured Soul"
	};
	public override bool AddToWorld()
	{
		switch(Util.Random(1,2))
        {
			case 1:
                {
					Model = 123;
					Name = (string)soul_names[Util.Random(0, soul_names.Count - 1)];
				}
				break;
			case 2:
                {
					Model = 659;
					Name = (string)soul_names[Util.Random(0, soul_names.Count - 1)];
				}
				break;
		}
		RespawnInterval = -1;
		MaxSpeedBase = 200;
		RoamingRange = 150;
		Size = (byte)Util.Random(45,55);
		Level = (byte)Util.Random(48, 53);
		Faction = FactionMgr.GetFactionByID(187);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
		TorturedSoulsBrain souls = new TorturedSoulsBrain();
		SetOwnBrain(souls);			
		base.AddToWorld();
		return true;
	}
}

public class ExplodeUndead : GameNpc
{
	public override int MaxHealth
	{
		get { return 4000; }
	}
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
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
	public override int AttackRange
	{
		get { return 200; }
		set { }
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 25;// dmg reduction for melee dmg
			case EDamageType.Crush: return 25;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 25;// dmg reduction for melee dmg
			default: return 25;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 150;
	}
	public static int ExplodeZombieCount = 0;
	public override void Die(GameObject killer)
	{
		--ExplodeZombieCount;
		RandomTarget = null;
		base.Die(killer);
	}
    public override void DropLoot(GameObject killer)//dont drop loot
    {
    }
    public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	List<GamePlayer> Zombie_Targets = new List<GamePlayer>();
	public override bool AddToWorld()
	{
		Model = 923;
		RandomTarget = null;
		ExplodeUndeadBrain.IsKilled = false;
		ExplodeUndeadBrain.SetAggroAmount = false;
		ExplodeZombieCount = 0;
		Zombie_Targets.Clear();
		Name = "infected ghoul";
		RespawnInterval = -1;
		MaxSpeedBase = 110;//slow so players can kite it
		Size = 70;
		Level = (byte)Util.Random(62, 65);
		Faction = FactionMgr.GetFactionByID(187);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
		ExplodeUndeadBrain souls = new ExplodeUndeadBrain();
		SetOwnBrain(souls);
		bool success = base.AddToWorld();
		if (success)
		{
			foreach(GamePlayer player in GetPlayersInRadius(2000))
            {
				if(player != null)
                {
					if(player.IsAlive && player.PlayerClass.ID != 12 && player.Client.Account.PrivLevel==1)
                    {
						if(!Zombie_Targets.Contains(player))
                        {
							Zombie_Targets.Add(player);
                        }
					}
                }
            }
			if(Zombie_Targets.Count>0)
            {
				GamePlayer Target = (GamePlayer)Zombie_Targets[Util.Random(0, Zombie_Targets.Count - 1)];
				RandomTarget = Target;						
				BroadcastMessage(String.Format(this.Name+" crawls toward "+RandomTarget.Name+"!"));
			}
		}
		return success;
	}
}
#endregion Lossren adds