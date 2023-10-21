using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

#region Anurigunda
public class Anurigunda : GameEpicBoss
{
	public Anurigunda() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Anurigunda Initializing...");
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
		get { return 350; }
		set { }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
		{
			if (npc != null)
			{
				if (npc.IsAlive && npc.Brain is AnurigundaAddsBrain)
				{
					npc.RemoveFromWorld();
				}
			}
		}
		base.Die(killer);
    }
    public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157942);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		Level = Convert.ToByte(npcTemplate.Level);
		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(82);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

		AnurigundaBrain sbrain = new AnurigundaBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}
#endregion Anurigunda

#region Anurigunda adds
public class AnurigundaAdd : GameNpc
{
	public override int MaxHealth
	{
		get { return 5000; }
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
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 35;// dmg reduction for melee dmg
			case EDamageType.Crush: return 35;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 35;// dmg reduction for melee dmg
			default: return 25;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 70;
	}
	List<int> Id_npctemplates = new List<int>()
	{
		60160948,60160946,60160979,60161009
	};

    public override bool AddToWorld()
	{
		int idtemplate = Id_npctemplates[Util.Random(0, Id_npctemplates.Count - 1)];
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(idtemplate);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = -1;

		Faction = FactionMgr.GetFactionByID(82);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));
		AnurigundaAddsBrain adds = new AnurigundaAddsBrain();
		SetOwnBrain(adds);
		base.AddToWorld();
		return true;
	}
}
#endregion Anurigunda adds