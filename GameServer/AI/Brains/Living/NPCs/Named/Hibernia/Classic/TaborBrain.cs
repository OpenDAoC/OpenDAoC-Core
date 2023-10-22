using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI.Brains;

#region Tabor
public class TaborBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public TaborBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 1000;
	}
    public override void Think()
	{
		if (HasAggro && Body.TargetObject != null)
        {
			GameLiving target = Body.TargetObject as GameLiving;
			if(!LivingHasEffect(target, Tabor_Dot) && Util.Chance(15) && !Body.IsCasting)
				Body.CastSpell(Tabor_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (!LivingHasEffect(target, Tabor_Dot2) && Util.Chance(15) && !Body.IsCasting)
				Body.CastSpell(Tabor_Dot2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (Util.Chance(15) && !Body.IsCasting)
				Body.CastSpell(Tabor_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (Util.Chance(15) && !Body.IsCasting)
				Body.CastSpell(Tabor_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.Think();
	}
	#region Spells
	private Spell m_Tabor_DD;
	private Spell Tabor_DD
	{
		get
		{
			if (m_Tabor_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3.5;
				spell.RecastDelay = Util.Random(10,15);
				spell.ClientEffect = 5087;
				spell.Icon = 5087;
				spell.TooltipId = 5087;
				spell.Damage = 100;
				spell.Name = "Earth Blast";
				spell.Range = 1500;
				spell.SpellID = 11931;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Tabor_DD = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_DD);
			}
			return m_Tabor_DD;
		}
	}
	private Spell m_Tabor_DD2;
	private Spell Tabor_DD2
	{
		get
		{
			if (m_Tabor_DD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3.5;
				spell.RecastDelay = Util.Random(15, 20);
				spell.ClientEffect = 5087;
				spell.Icon = 5087;
				spell.TooltipId = 5087;
				spell.Damage = 80;
				spell.Name = "Earth Blast";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.SpellID = 11932;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Tabor_DD2 = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_DD2);
			}
			return m_Tabor_DD2;
		}
	}
	private Spell m_Tabor_Dot;
	private Spell Tabor_Dot
	{
		get
		{
			if (m_Tabor_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3411;
				spell.Icon = 3411;
				spell.Name = "Poison";
				spell.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 3411;
				spell.Range = 1500;
				spell.Damage = 25;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11933;
				spell.Target = "Enemy";
				spell.SpellGroup = 1802;
				spell.EffectGroup = 1502;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Tabor_Dot = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_Dot);
			}
			return m_Tabor_Dot;
		}
	}
	private Spell m_Tabor_Dot2;
	private Spell Tabor_Dot2
	{
		get
		{
			if (m_Tabor_Dot2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3475;
				spell.Icon = 4431;
				spell.Name = "Acid";
				spell.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 4431;
				spell.Range = 1500;
				spell.Damage = 25;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11934;
				spell.Target = "Enemy";
				spell.SpellGroup = 1803;
				spell.EffectGroup = 1503;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Body;
				m_Tabor_Dot2 = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_Dot2);
			}
			return m_Tabor_Dot2;
		}
	}
	#endregion
}
#endregion Tabor

#region Ghost of Tabor
public class TaborGhostBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public TaborGhostBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if (HasAggro && Body.TargetObject != null)
		{
			GameLiving target = Body.TargetObject as GameLiving;
			if (!LivingHasEffect(target, Tabor_Dot) && Util.Chance(15) && !Body.IsCasting)
				Body.CastSpell(Tabor_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (!LivingHasEffect(target, Tabor_Dot2) && Util.Chance(15) && !Body.IsCasting)
				Body.CastSpell(Tabor_Dot2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (Util.Chance(15) && !Body.IsCasting)
				Body.CastSpell(Tabor_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (Util.Chance(15) && !Body.IsCasting)
				Body.CastSpell(Tabor_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

		}
		base.Think();
	}
	#region Spells
	private Spell m_Tabor_DD;
	private Spell Tabor_DD
	{
		get
		{
			if (m_Tabor_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3.5;
				spell.RecastDelay = Util.Random(10, 15);
				spell.ClientEffect = 5087;
				spell.Icon = 5087;
				spell.TooltipId = 5087;
				spell.Damage = 100;
				spell.Name = "Earth Blast";
				spell.Range = 1500;
				spell.SpellID = 11938;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Tabor_DD = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_DD);
			}
			return m_Tabor_DD;
		}
	}
	private Spell m_Tabor_DD2;
	private Spell Tabor_DD2
	{
		get
		{
			if (m_Tabor_DD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3.5;
				spell.RecastDelay = Util.Random(15, 20);
				spell.ClientEffect = 5087;
				spell.Icon = 5087;
				spell.TooltipId = 5087;
				spell.Damage = 80;
				spell.Name = "Earth Blast";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.SpellID = 11937;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Tabor_DD2 = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_DD2);
			}
			return m_Tabor_DD2;
		}
	}
	private Spell m_Tabor_Dot;
	private Spell Tabor_Dot
	{
		get
		{
			if (m_Tabor_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3411;
				spell.Icon = 3411;
				spell.Name = "Poison";
				spell.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 3411;
				spell.Range = 1500;
				spell.Damage = 25;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11936;
				spell.Target = "Enemy";
				spell.SpellGroup = 1802;
				spell.EffectGroup = 1502;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Tabor_Dot = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_Dot);
			}
			return m_Tabor_Dot;
		}
	}
	private Spell m_Tabor_Dot2;
	private Spell Tabor_Dot2
	{
		get
		{
			if (m_Tabor_Dot2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3475;
				spell.Icon = 4431;
				spell.Name = "Acid";
				spell.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 4431;
				spell.Range = 1500;
				spell.Damage = 25;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11935;
				spell.Target = "Enemy";
				spell.SpellGroup = 1803;
				spell.EffectGroup = 1503;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Body;
				m_Tabor_Dot2 = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_Dot2);
			}
			return m_Tabor_Dot2;
		}
	}
	#endregion
}
#endregion Ghost of Tabor

#region Swirl of Dirt
public class SwirlDirtBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SwirlDirtBrain() : base()
	{
		AggroLevel = 0;
		AggroRange = 0;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Swirl of Dirt