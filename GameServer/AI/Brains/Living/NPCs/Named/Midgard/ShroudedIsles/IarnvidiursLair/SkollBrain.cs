using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

public class SkollBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SkollBrain() : base()
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
						if (npc.IsAlive && npc.PackageID == "SkollBaf")
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
				if (Util.Chance(15))
				{
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDot), 1000);
					}
				}
				if (Util.Chance(15))
				{
					if (LivingHasEffect(Body.TargetObject as GameLiving, Skoll_Haste_Debuff) == false && Body.TargetObject.IsVisibleTo(Body))
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastHasteDebuff), 1000);
					}
				}
			}
		}
		base.Think();
	}
	public int CastHasteDebuff(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(Skoll_Haste_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	public int CastDot(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(Skoll_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	private Spell m_Skoll_Haste_Debuff;
	private Spell Skoll_Haste_Debuff
	{
		get
		{
			if (m_Skoll_Haste_Debuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 30;
				spell.Duration = 45;
				spell.ClientEffect = 5427;
				spell.Icon = 5427;
				spell.Name = "Skoll's Debuff Haste";
				spell.TooltipId = 5427;
				spell.Range = 1500;
				spell.Value = 38;
				spell.SpellID = 11811;
				spell.Target = "Enemy";
				spell.Type = ESpellType.CombatSpeedDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Skoll_Haste_Debuff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Skoll_Haste_Debuff);
			}
			return m_Skoll_Haste_Debuff;
		}
	}
	private Spell m_Skoll_Dot;
	private Spell Skoll_Dot
	{
		get
		{
			if (m_Skoll_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 20;
				spell.ClientEffect = 92;
				spell.Icon = 92;
				spell.Name = "Skoll's Poison";
				spell.Description = "Inflicts 80 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "You are covered in lava!";
				spell.Message2 = "{0} is covered in lava!";
				spell.Message3 = "The lava hardens and falls away.";
				spell.Message4 = "The lava falls from {0}'s skin.";
				spell.TooltipId = 92;
				spell.Range = 1500;
				spell.Damage = 80;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11812;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_Skoll_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Skoll_Dot);
			}
			return m_Skoll_Dot;
		}
	}
}