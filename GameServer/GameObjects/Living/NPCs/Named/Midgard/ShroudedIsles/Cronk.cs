using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;

namespace DOL.GS;

public class Cronk : GameEpicBoss
{
	public Cronk() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Cronk Initializing...");
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
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159504);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		CronkBrain sbrain = new CronkBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(70))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(CronkDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		if (Util.Chance(35) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DexQuiDebuff))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(DebuffDQ, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
    #region Spells
    private Spell m_CronkDD;
	private Spell CronkDD
	{
		get
		{
			if (m_CronkDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 20;
				spell.ClientEffect = 360;
				spell.Icon = 360;
				spell.Damage = 400;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Fire Blast";
				spell.Range = 500;
				spell.SpellID = 11881;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_CronkDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CronkDD);
			}
			return m_CronkDD;
		}
	}
	private Spell m_DebuffDQ;
	private Spell DebuffDQ
	{
		get
		{
			if (m_DebuffDQ == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(25, 45);
				spell.Duration = 60;
				spell.Value = 78;
				spell.ClientEffect = 2627;
				spell.Icon = 2627;
				spell.TooltipId = 2627;
				spell.Name = "Vitality Dispersal";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.SpellID = 11882;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DexterityQuicknessDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_DebuffDQ = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DebuffDQ);
			}
			return m_DebuffDQ;
		}
	}
    #endregion
}