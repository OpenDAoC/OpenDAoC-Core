using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

public class FostraOrmBrain : StandardMobBrain
{
	public FostraOrmBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 500;
	}
	
	public override void Think()
	{
		if (HasAggro && Body.TargetObject != null)
		{
			GameLiving target = Body.TargetObject as GameLiving;
			if (Util.Chance(25) && target != null)
			{
				if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
				{
					Body.CastSpell(OrmDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			if (Util.Chance(25) && target != null)
			{
				if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
				{
					Body.CastSpell(OrmDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
		}
		base.Think();
	}
	private Spell m_OrmDot;
	private Spell OrmDot
	{
		get
		{
			if (m_OrmDot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 3411;
				spell.Icon = 3411;
				spell.TooltipId = 3411;
				spell.Name = "Orm Poison";
				spell.Description = "Inflicts 70 damage to the target every 3 sec for 30 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.Damage = 70;
				spell.Duration = 30;
				spell.Frequency = 30;
				spell.Range = 500;
				spell.SpellID = 11853;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.DamageType = (int)EDamageType.Body;
				spell.Uninterruptible = true;
				m_OrmDot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OrmDot);
			}
			return m_OrmDot;
		}
	}
	private Spell m_OrmDisease;
	private Spell OrmDisease
	{
		get
		{
			if (m_OrmDisease == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4375;
				spell.Icon = 4375;
				spell.Name = "Disease";
				spell.Message1 = "You are diseased!";
				spell.Message2 = "{0} is diseased!";
				spell.Message3 = "You look healthy.";
				spell.Message4 = "{0} looks healthy again.";
				spell.TooltipId = 4375;
				spell.Range = 350;
				spell.Duration = 120;
				spell.SpellID = 11854;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_OrmDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OrmDisease);
			}
			return m_OrmDisease;
		}
	}
}