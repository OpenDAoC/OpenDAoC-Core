using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS;

public class Marfoirwort : GameEpicBoss
{
	public Marfoirwort() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Marfoirwort Initializing...");
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
		get { return 40000; }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163696);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		MarfoirwortBrain sbrain = new MarfoirwortBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public override void DealDamage(AttackData ad)
	{
		if (ad != null && ad.DamageType == EDamageType.Body)
			Health += ad.Damage;
		base.DealDamage(ad);
	}
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(25))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(MarfoirwortDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
	private Spell m_MarfoirwortDD;
	public Spell MarfoirwortDD
	{
		get
		{
			if (m_MarfoirwortDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.Power = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 9191;
				spell.Icon = 9191;
				spell.Damage = 450;
				spell.Radius = 350;
				spell.DamageType = (int)EDamageType.Body;
				spell.Name = "Marfoirwort Life-leech";
				spell.Range = 500;
				spell.SpellID = 11905;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_MarfoirwortDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MarfoirwortDD);
			}
			return m_MarfoirwortDD;
		}
	}
}