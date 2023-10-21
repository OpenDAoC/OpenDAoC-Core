using Core.Database.Tables;

namespace Core.GS.AI.Brains;

public class AncientSyverBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public AncientSyverBrain() : base()
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
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
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
						if (npc.IsAlive && npc.PackageID == "AncientSyverBaf")
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
					GameLiving target = Body.TargetObject as GameLiving;
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDisease), 1000);
					}
				}
				if (Util.Chance(15))
				{
					if (LivingHasEffect(Body.TargetObject as GameLiving, Syver_Str_Debuff) == false && Body.TargetObject.IsVisibleTo(Body))
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastStrengthDebuff), 1000);
					}
				}
				if (Util.Chance(15))
				{
					if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.MeleeHasteBuff))
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastHasteBuff), 1000);
					}
				}
			}
		}
		base.Think();
	}
	public int CastStrengthDebuff(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(Syver_Str_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	public int CastDisease(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(SyverDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	public int CastHasteBuff(EcsGameTimer timer)
	{
		if (HasAggro && Body.IsAlive)
		{
			Body.CastSpell(Syver_Haste_Buff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
    #region Spells
    private Spell m_Syver_Str_Debuff;
	private Spell Syver_Str_Debuff
	{
		get
		{
			if (m_Syver_Str_Debuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 30;
				spell.Duration = 60;
				spell.ClientEffect = 4537;
				spell.Icon = 4537;
				spell.Name = "Ancient Syver's Strength Debuff";
				spell.TooltipId = 4537;
				spell.Range = 1500;
				spell.Value = 46;
				spell.SpellID = 11826;
				spell.Target = "Enemy";
				spell.Type = ESpellType.StrengthDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Syver_Str_Debuff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Syver_Str_Debuff);
			}
			return m_Syver_Str_Debuff;
		}
	}
	private Spell m_SyverDisease;
	private Spell SyverDisease
	{
		get
		{
			if (m_SyverDisease == null)
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
				spell.Duration = 210;
				spell.SpellID = 11825;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_SyverDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SyverDisease);
			}
			return m_SyverDisease;
		}
	}
	private Spell m_Syver_Haste_Buff;
	private Spell Syver_Haste_Buff
	{
		get
		{
			if (m_Syver_Haste_Buff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 35;
				spell.Duration = 20;
				spell.ClientEffect = 10535;
				spell.Icon = 10535;
				spell.Name = "Ancient Syver's Haste";
				spell.Message2 = "{0} begins attacking faster!";
				spell.Message4 = "{0}'s attacks return to normal.";
				spell.TooltipId = 10535;
				spell.Range = 0;
				spell.Value = 50;
				spell.SpellID = 11827;
				spell.Target = "Self";
				spell.Type = ESpellType.CombatSpeedBuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Syver_Haste_Buff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Syver_Haste_Buff);
			}
			return m_Syver_Haste_Buff;
		}
	}
    #endregion
}