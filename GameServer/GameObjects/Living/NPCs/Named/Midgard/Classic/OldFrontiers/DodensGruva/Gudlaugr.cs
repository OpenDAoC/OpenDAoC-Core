using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;

namespace Core.GS.Scripts;

public class Gudlaugr : GameEpicBoss
{
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9919);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		Faction = FactionMgr.GetFactionByID(779);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		BodyType = 1;
		ScalingFactor = 40;
		GudlaugrBrain sbrain = new GudlaugrBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		SaveIntoDatabase();
		return true;
	}

	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
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
	public override void OnAttackEnemy(AttackData ad)
	{
		GudlaugrBrain brain = new GudlaugrBrain();
		if (TargetObject != null)
		{
			if (ad.Target.IsWithinRadius(this, AttackRange))
			{
				if (!ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.Bleed))
				{
					CastSpell(brain.Bleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				if (!ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff))
				{
					CastSpell(brain.Snare, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
		}
		base.OnAttackEnemy(ad);
	}		
	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Gudlaugr NPC Initializing...");
	}
}