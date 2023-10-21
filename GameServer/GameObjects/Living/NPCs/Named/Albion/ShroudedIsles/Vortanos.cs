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

namespace Core.GS;

#region Vortanos
public class Vortanos : GameEpicBoss
{
	public Vortanos() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Vortanos Initializing...");
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167731);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		Faction = FactionMgr.GetFactionByID(64);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		VortanosBrain sbrain = new VortanosBrain();
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
			if (npc != null && npc.IsAlive && npc.Brain is VortanosAddBrain)
				npc.RemoveFromWorld();
		}
		base.Die(killer);
    }
	public override void EnemyKilled(GameLiving enemy)
    {
		
        base.EnemyKilled(enemy);
    }
    public override void OnAttackEnemy(AttackData ad)
	{
		if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
		{
			if(Util.Chance(50) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.StrConDebuff))
				CastSpell(VortanosSCDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (Util.Chance(50) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DexQuiDebuff))
				CastSpell(VortanosDebuffDQ, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
	#region Spells
	private Spell m_VortanosSCDebuff;
	private Spell VortanosSCDebuff
	{
		get
		{
			if (m_VortanosSCDebuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 60;
				spell.ClientEffect = 5408;
				spell.Icon = 5408;
				spell.Name = "S/C Debuff";
				spell.TooltipId = 5408;
				spell.Range = 1200;
				spell.Value = 65;
				spell.Duration = 60;
				spell.SpellID = 11917;
				spell.Target = "Enemy";
				spell.Type = "StrengthConstitutionDebuff";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_VortanosSCDebuff = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VortanosSCDebuff);
			}
			return m_VortanosSCDebuff;
		}
	}
	private Spell m_VortanosDebuffDQ;
	private Spell VortanosDebuffDQ
	{
		get
		{
			if (m_VortanosDebuffDQ == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 60;
				spell.Duration = 60;
				spell.Value = 65;
				spell.ClientEffect = 2627;
				spell.Icon = 2627;
				spell.TooltipId = 2627;
				spell.Name = "D/Q Debuff";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.SpellID = 11918;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DexterityQuicknessDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_VortanosDebuffDQ = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VortanosDebuffDQ);
			}
			return m_VortanosDebuffDQ;
		}
	}
	#endregion
}
#endregion Vortanos

#region Vortanos adds
public class VortanosAdd : GameNpc
{
	public VortanosAdd() : base() { }

	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 10;// dmg reduction for melee dmg
			case EDamageType.Crush: return 10;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 10;// dmg reduction for melee dmg
			default: return 10;// dmg reduction for rest resists
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
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.10;
	}
	public override int MaxHealth
	{
		get { return 2000; }
	}
    #region Stats
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
	public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
	public override short Piety { get => base.Piety; set => base.Piety = 200; }
	public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 120; }
    #endregion
	public override bool AddToWorld()
	{
		Size = (byte)Util.Random(50, 55);
		MaxSpeedBase = 225;
		RespawnInterval = -1;

		Faction = FactionMgr.GetFactionByID(64);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

		VortanosAddBrain sbrain = new VortanosAddBrain();
		SetOwnBrain(sbrain);
		RoamingRange = 300;
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
	public override long ExperienceValue => 0;
	public override void DropLoot(GameObject killer)
	{
	}
}
#endregion Vortanos adds