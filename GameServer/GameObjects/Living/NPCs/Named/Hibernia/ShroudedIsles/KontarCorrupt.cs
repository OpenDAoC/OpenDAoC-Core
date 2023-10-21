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
using Core.GS.Skills;

namespace Core.GS;

#region Kontar
public class KontarCorrupt : GameEpicBoss
{
	public KontarCorrupt() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Kontar the Corrupt Initializing...");
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
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
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
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 350; }
	#endregion

	public override bool AddToWorld()
	{
		Name = "Kontar the Corrupt";
		Model = 767;
		Size = 120;
		Level = 65;
		MaxDistance = 2500;
		TetherRange = 2600;
		Faction = FactionMgr.GetFactionByID(96);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
		MaxSpeedBase = 280;

		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		KontarCorruptBrain sbrain = new KontarCorruptBrain();
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
			if (npc != null && npc.IsAlive && npc.Brain is CorruptorBodyguardBrain)
				npc.RemoveFromWorld();
		}
		base.Die(killer);
    }
}
#endregion Kontar

#region Kontar adds
public class CorruptorBodyguard : GameNpc
{
	public CorruptorBodyguard() : base()
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
		Model = 767;
		Name = "corruptor bodyguard";
		Level = (byte)Util.Random(50, 55);
		Size = (byte)Util.Random(35, 45);
		RespawnInterval = -1;
		RoamingRange = 200;
		MaxDistance = 2500;
		TetherRange = 2600;
		Faction = FactionMgr.GetFactionByID(96);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

		LoadedFromScript = true;
		CorruptorBodyguardBrain sbrain = new CorruptorBodyguardBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(25))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(CorruptorBodyguardDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
	private Spell m_CorruptorBodyguardDD;
	public Spell CorruptorBodyguardDD
	{
		get
		{
			if (m_CorruptorBodyguardDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 5;
				spell.ClientEffect = 4359;
				spell.Icon = 4359;
				spell.Damage = 150;
				spell.DamageType = (int)EDamageType.Energy;
				spell.Name = "Energy Blast";
				spell.Range = 500;
				spell.Radius = 300;
				spell.SpellID = 11907;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_CorruptorBodyguardDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CorruptorBodyguardDD);
			}
			return m_CorruptorBodyguardDD;
		}
	}
}
#endregion Kontar adds