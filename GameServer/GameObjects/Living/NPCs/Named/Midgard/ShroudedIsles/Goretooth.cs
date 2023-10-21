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

public class Goretooth : GameEpicBoss
{
	public Goretooth() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Goretooth Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 70;// dmg reduction for rest resists
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
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(35) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(GoretoothPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
		{
			if (Util.Chance(35) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && ad.Target.IsAlive)
				CastSpell(Goretooth_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
	public override int MaxHealth
	{
		get { return 30000; }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161492);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		GoretoothBrain sbrain = new GoretoothBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    #region Spells
    private Spell m_GoretoothPoison;
	private Spell GoretoothPoison
	{
		get
		{
			if (m_GoretoothPoison == null)
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
				spell.SpellID = 11879;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.DamageType = (int)EDamageType.Body;
				spell.Uninterruptible = true;
				m_GoretoothPoison = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GoretoothPoison);
			}
			return m_GoretoothPoison;
		}
	}
	private Spell m_Goretooth_stun;
	private Spell Goretooth_stun
	{
		get
		{
			if (m_Goretooth_stun == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 2165;
				spell.Icon = 2132;
				spell.TooltipId = 2132;
				spell.Duration = 6;
				spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
				spell.Name = "Stun";
				spell.Range = 400;
				spell.SpellID = 11880;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Stun.ToString();
				m_Goretooth_stun = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Goretooth_stun);
			}
			return m_Goretooth_stun;
		}
	}
    #endregion
}