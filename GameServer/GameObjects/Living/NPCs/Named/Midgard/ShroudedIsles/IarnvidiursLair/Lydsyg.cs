using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;

namespace Core.GS;

public class Lydsyg : GameEpicNPC
{
	public Lydsyg() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Lydsyg Initializing...");
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
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(45))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
			{
				CastSpell(LydsygDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
		}
		base.OnAttackEnemy(ad);
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
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.35;
	}
	public override int MaxHealth
	{
		get { return 10000; }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(83009);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(159);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(159));

		LydsygBrain sbrain = new LydsygBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	private Spell m_LydsygDisease;
	private Spell LydsygDisease
	{
		get
		{
			if (m_LydsygDisease == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4375;
				spell.Icon = 4375;
				spell.Name = "Black Plague";
				spell.Message1 = "You are diseased!";
				spell.Message2 = "{0} is diseased!";
				spell.Message3 = "You look healthy.";
				spell.Message4 = "{0} looks healthy again.";
				spell.TooltipId = 4375;
				spell.Radius = 450;
				spell.Range = 450;
				spell.Duration = 3000;//50min
				spell.SpellID = 11822;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_LydsygDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LydsygDisease);
			}
			return m_LydsygDisease;
		}
	}
}