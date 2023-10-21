﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;

namespace DOL.GS;

#region Zytka
public class Zytka : GameEpicBoss
{
	public Zytka() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Zytka Initializing...");
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
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
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
	#region Stats
	public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
	public override short Piety { get => base.Piety; set => base.Piety = 200; }
	public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
	public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 50; }
	public override short Strength { get => base.Strength; set => base.Strength = 250; }
	#endregion
	public override bool AddToWorld()
	{
		Name = "Zytka";
		Model = 904;
		Size = 120;
		Level = (byte)Util.Random(65,70);
		MaxDistance = 2500;
		TetherRange = 2600;
		Faction = FactionMgr.GetFactionByID(96);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
		MaxSpeedBase = 280;

		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		ZytkaBrain sbrain = new ZytkaBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public override void Die(GameObject killer)
	{
		foreach (GameNpc npc in GetNPCsInRadius(2500))
		{
			if (npc != null && npc.IsAlive && npc.Brain is ZytkaAddBrain)
				npc.RemoveFromWorld();
		}
		base.Die(killer);
	}
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(ZytkaDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		base.OnAttackEnemy(ad);
	}
	private Spell m_ZytkaDD;
	public Spell ZytkaDD
	{
		get
		{
			if (m_ZytkaDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.Power = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 4111;
				spell.Icon = 4111;
				spell.Damage = 250;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Fire Blast";
				spell.Range = 500;
				spell.SpellID = 11908;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_ZytkaDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ZytkaDD);
			}
			return m_ZytkaDD;
		}
	}
}
#endregion Zytka

#region Zytka adds
public class ZytkaAdd : GameNpc
{
	public ZytkaAdd() : base()
	{
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 15;// dmg reduction for melee dmg
			case EDamageType.Crush: return 15;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 15;// dmg reduction for melee dmg
			default: return 15;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int MaxHealth
	{
		get { return 2000; }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.10;
	}
	#region Stats
	public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
	public override short Piety { get => base.Piety; set => base.Piety = 200; }
	public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
	public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 150; }
	#endregion

	public override bool AddToWorld()
	{
		Model = 904;
		Name = "young octonoid";
		Level = 50;
		Size = (byte)Util.Random(35, 45);
		RespawnInterval = -1;
		RoamingRange = 200;
		MaxDistance = 2500;
		TetherRange = 2600;
		Faction = FactionMgr.GetFactionByID(96);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

		LoadedFromScript = true;
		ZytkaAddBrain sbrain = new ZytkaAddBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Zytka adds