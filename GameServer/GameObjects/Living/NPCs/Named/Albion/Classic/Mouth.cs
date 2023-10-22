using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS;

public class Mouth : GameEpicBoss
{
	public Mouth() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Mouth Initializing...");
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
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(13025);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(18);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(18));

		MouthBrain sbrain = new MouthBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "MouthAdd")
				npc.Die(this);
		}
		base.Die(killer);
    }
    public override void OnAttackEnemy(AttackData ad)
	{
		if (Util.Chance(30))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
			{
				CastSpell(MouthLifeDrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
		}
		base.OnAttackEnemy(ad);
	}
    public override void DealDamage(AttackData ad)
    {
		if (ad != null && ad.AttackType == EAttackType.Spell && ad.Damage > 0)
		{
			Health += ad.Damage;
		}
        base.DealDamage(ad);
    }
    private Spell m_MouthLifeDrain;
	private Spell MouthLifeDrain
	{
		get
		{
			if (m_MouthLifeDrain == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 14352;
				spell.Icon = 14352;
				spell.TooltipId = 14352;
				spell.Damage = 500;
				spell.Name = "Lifedrain";
				spell.Range = 350;
				spell.SpellID = 11897;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Body;
				m_MouthLifeDrain = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MouthLifeDrain);
			}
			return m_MouthLifeDrain;
		}
	}
}