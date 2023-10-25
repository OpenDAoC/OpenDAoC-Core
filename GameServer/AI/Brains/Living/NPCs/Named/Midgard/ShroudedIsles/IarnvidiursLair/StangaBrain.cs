using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

public class StangaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public StangaBrain() : base()
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
						if (npc.IsAlive && npc.PackageID == "StangaBaf")
						{
							AddAggroListTo(npc.Brain as StandardMobBrain);
						}
					}
				}
				IsPulled = true;
			}
			if (Body.TargetObject != null)
			{
				if (Util.Chance(15))
				{
					if (LivingHasEffect(Body.TargetObject as GameLiving, Stanga_SC_Debuff) == false && Body.TargetObject.IsVisibleTo(Body))
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastSCDebuff), 1000);
					}
				}
				if (Util.Chance(15))
				{
					if (LivingHasEffect(Body.TargetObject as GameLiving, StangaDisease) == false && Body.TargetObject.IsVisibleTo(Body))
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDisease), 1000);
					}
				}
			}
		}
		base.Think();
	}
	public int CastSCDebuff(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(Stanga_SC_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	public int CastDisease(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(StangaDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	private Spell m_StangaDisease;
	private Spell StangaDisease
	{
		get
		{
			if (m_StangaDisease == null)
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
				spell.Range = 0;
				spell.Duration = 186;
				spell.SpellID = 11819;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_StangaDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_StangaDisease);
			}
			return m_StangaDisease;
		}
	}
	private Spell m_Stanga_SC_Debuff;
	private Spell Stanga_SC_Debuff
	{
		get
		{
			if (m_Stanga_SC_Debuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 30;
				spell.Duration = 60;
				spell.ClientEffect = 2767;
				spell.Icon = 2767;
				spell.Name = "Stanga's Debuff S/C";
				spell.TooltipId = 2767;
				spell.Range = 1500;
				spell.Value = 80;
				spell.Radius = 400;
				spell.SpellID = 11818;
				spell.Target = "Enemy";
				spell.Type = ESpellType.StrengthConstitutionDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Stanga_SC_Debuff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Stanga_SC_Debuff);
			}
			return m_Stanga_SC_Debuff;
		}
	}
}