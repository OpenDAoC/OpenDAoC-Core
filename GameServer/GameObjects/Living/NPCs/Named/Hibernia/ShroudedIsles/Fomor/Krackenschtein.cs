using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Events;
using Core.GS.PacketHandler;
using Core.Database;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS;

public class Krackenschtein : GameEpicBoss
{
	public Krackenschtein() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Krackenschtein Initializing...");
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
			if (!source.IsWithinRadius(this, 300)) //take no damage
			{
				GamePlayer truc;
				if (source is GamePlayer)
					truc = (source as GamePlayer);
				else
					truc = ((source as GameSummonedPet).Owner as GamePlayer);
				if (truc != null)
					truc.Out.SendMessage(Name + " is immune to your damage!", EChatType.CT_System,
						EChatLoc.CL_ChatWindow);

				base.TakeDamage(source, damageType, 0, 0);
				return;
			}
			else //take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
		}
	}
	public override void StartAttack(GameObject target)
    {
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162981);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(82);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));
		MaxSpeedBase = 0;

		KrackenschteinBrain sbrain = new KrackenschteinBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}