using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;

namespace Core.GS;

#region Queen Qunilaria
public class QueenQunilaria : GameEpicBoss
{
	public QueenQunilaria() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Queen Qunilaria Initializing...");
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165085);
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
		foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
		{
			if (npc != null)
			{
				if (npc.IsAlive && npc.Brain is QunilariaAddBrain)
				{
					npc.Die(npc);
				}
			}
		}
		QueenQunilariaBrain sbrain = new QueenQunilariaBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		SpawnAdds();
        base.Die(killer);
    }
	public void SpawnAdds()
	{
		for (int i = 0; i < Util.Random(15,22); i++)
		{
			QunilariaAdd2 Add1 = new QunilariaAdd2();
			Add1.X = X + Util.Random(-100, 100);
			Add1.Y = Y + Util.Random(-100, 100);
			Add1.Z = Z;
			Add1.CurrentRegion = CurrentRegion;
			Add1.Heading = Heading;
			Add1.RespawnInterval = -1;
			Add1.AddToWorld();
		}
	}
}
#endregion Queen Qunilaria

#region Queen adds
public class QunilariaAdd : GameNpc
{
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 25; // dmg reduction for melee dmg
			case EDamageType.Crush: return 25; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 25; // dmg reduction for melee dmg
			default: return 25; // dmg reduction for rest resists
		}
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}
	public override int MaxHealth
	{
		get { return 3000; }
	}

	public static int MinionCount = 0;
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override long ExperienceValue => 0;
	public override void Die(GameObject killer)
	{
		--MinionCount;
		base.Die(killer);
	}
	public override bool AddToWorld()
	{
		Model = 764;
		Name = "Qunilaria's minion";
		Strength = 150;
		Dexterity = 200;
		Quickness = 100;
		Constitution = 100;
		RespawnInterval = -1;
		MaxSpeedBase = 225;

		++MinionCount;
		Size = (byte)Util.Random(80, 100);
		Level = (byte)Util.Random(58, 64);
		Faction = FactionMgr.GetFactionByID(93);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));//minions of balor
		QunilariaAddBrain add = new QunilariaAddBrain();
		SetOwnBrain(add);
		base.AddToWorld();
		return true;
	}
}
#endregion Queen adds

#region Queen post-death adds
public class QunilariaAdd2 : GameNpc
{
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 25; // dmg reduction for melee dmg
			case EDamageType.Crush: return 25; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 25; // dmg reduction for melee dmg
			default: return 25; // dmg reduction for rest resists
		}
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
	public override int MaxHealth
	{
		get { return 1200; }
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override long ExperienceValue => 0;
	public override bool AddToWorld()
	{
		Model = 764;
		Name = "Qunilaria's minion";
		Strength = 50;
		Dexterity = 150;
		Quickness = 100;
		Constitution = 100;
		RespawnInterval = -1;
		MaxSpeedBase = 200;

		Size = 60;
		Level = (byte)Util.Random(52, 55);
		Faction = FactionMgr.GetFactionByID(93);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));//minions of balor
		QunilariaAddBrain add = new QunilariaAddBrain();
		SetOwnBrain(add);
		base.AddToWorld();
		return true;
	}
}
#endregion Queen post-death adds