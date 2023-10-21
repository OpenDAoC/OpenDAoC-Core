using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS;

#region Rotoddjur
public class Rotoddjur : GameEpicNPC
{
	public Rotoddjur() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Rotoddjur Initializing...");
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
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int MaxHealth
	{
		get { return 10000; }
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
		{
			if (npc != null)
			{
				if (npc.IsAlive && npc.Brain is RotoddjurAddBrain)
				{
					npc.RemoveFromWorld();
				}
			}
		}
		base.Die(killer);
    }
    public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165428);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(150);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
		RotoddjurBrain sbrain = new RotoddjurBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}
#endregion Rotoddjur

#region Rotoddjur adds
public class RotoddjurAdd : GameNpc
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
		return 0.35;
	}
	public override short Strength { get => base.Strength; set => base.Strength = 350; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override int MaxHealth
	{
		get { return 3000; }
	}
	public override bool AddToWorld()
	{
		Model = 820;
		Name = "Rotoddjur's Servant";
		RespawnInterval = -1;
		MaxSpeedBase = 225;

		Size = (byte)Util.Random(40, 60);
		Level = (byte)Util.Random(62, 66);
		Faction = FactionMgr.GetFactionByID(150);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
		RotoddjurAddBrain add = new RotoddjurAddBrain();
		SetOwnBrain(add);
		base.AddToWorld();
		return true;
	}
}
#endregion Rotoddjur adds