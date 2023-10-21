using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

public class MahattavaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MahattavaBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsPulled = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled = false;
		}
		if (Body.InCombat && HasAggro && Body.TargetObject != null)
		{
			if (IsPulled == false)
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.PackageID == "MahattavaBaf")
						{
							AddAggroListTo(npc.Brain as StandardMobBrain);
						}
					}
				}
				IsPulled = true;
			}
			if(Body.TargetObject != null)
            {
				GameLiving target = Body.TargetObject as GameLiving;
				if (target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
					Body.CastSpell(Mahattava_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				else
					Body.CastSpell(Mahattava_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
		}
		base.Think();
	}
	private Spell m_Mahattava_Dot;
	private Spell Mahattava_Dot
	{
		get
		{
			if (m_Mahattava_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 3411;
				spell.Icon = 3411;
				spell.Name = "Mahattava's Infection";
				spell.Description = "Inflicts 80 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 3411;
				spell.Range = 1500;
				spell.Damage = 80;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11804;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Mahattava_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mahattava_Dot);
			}
			return m_Mahattava_Dot;
		}
	}
	private Spell m_Mahattava_DD;
	private Spell Mahattava_DD
	{
		get
		{
			if (m_Mahattava_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = Util.Random(10,15);
				spell.ClientEffect = 3494;
				spell.Icon = 3494;
				spell.Name = "Mahattava's Strike";
				spell.TooltipId = 3494;
				spell.Range = 1500;
				spell.Damage = 300;
				spell.SpellID = 11804;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter; 
				m_Mahattava_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mahattava_DD);
			}
			return m_Mahattava_DD;
		}
	}
}