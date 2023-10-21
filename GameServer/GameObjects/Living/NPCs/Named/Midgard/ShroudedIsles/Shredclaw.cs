using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;

namespace Core.GS;

public class Shredclaw : GameEpicBoss
{
	public Shredclaw() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Shredclaw Initializing...");
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
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 180; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 400; }
    #endregion
    public override bool AddToWorld()
	{
		Name = "Shredclaw";
		Model = 891;
		Size = 100;
		Level = (byte)Util.Random(66,68);

		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		ShredclawBrain sbrain = new ShredclawBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(35) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(ShredclawPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
		{
			if (Util.Chance(35) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.Bleed) && ad.Target.IsAlive)
				CastSpell(ShredclawBleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
    #region Spells
    private Spell m_ShredclawPoison;
	private Spell ShredclawPoison
	{
		get
		{
			if (m_ShredclawPoison == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 3475;
				spell.Icon = 3475;
				spell.TooltipId = 3475;
				spell.Name = "Poison";
				spell.Description = "Inflicts 100 damage to the target every 3 sec for 30 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.Damage = 100;
				spell.Duration = 30;
				spell.Frequency = 30;
				spell.Range = 500;
				spell.SpellID = 11880;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.DamageType = (int)EDamageType.Body;
				spell.Uninterruptible = true;
				m_ShredclawPoison = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ShredclawPoison);
			}
			return m_ShredclawPoison;
		}
	}
	private Spell m_ShredclawBleed;
	private Spell ShredclawBleed
	{
		get
		{
			if (m_ShredclawBleed == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 2130;
				spell.Icon = 3411;
				spell.TooltipId = 3411;
				spell.Damage = 45;
				spell.Name = "Bleed";
				spell.Description = "Does 45 bleeding damage to a target every 3 seconds for 30 seconds.";
				spell.Message1 = "You are bleeding! ";
				spell.Message2 = "{0} is bleeding! ";
				spell.Duration = 30;
				spell.Frequency = 30;
				spell.Range = 350;
				spell.SpellID = 11781;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.StyleBleeding.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Body;
				m_ShredclawBleed = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ShredclawBleed);
			}
			return m_ShredclawBleed;
		}
	}
    #endregion
}