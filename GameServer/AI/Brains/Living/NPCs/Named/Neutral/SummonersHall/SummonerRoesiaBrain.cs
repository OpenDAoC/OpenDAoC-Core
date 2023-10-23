using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI;

public class SummonerRoesiaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SummonerRoesiaBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 2000;
	}
	public override void Think()
	{			
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			RandomTarget = null;
			CanCast = false;
			if (Enemys_To_DD.Count > 0)
				Enemys_To_DD.Clear();
		}
		if(Body.IsAlive)
        {
			if (!Body.Spells.Contains(RoesiaDot))
				Body.Spells.Add(RoesiaDot);
			if (!Body.Spells.Contains(RoesiaDS))
				Body.Spells.Add(RoesiaDS);
			if (!Body.Spells.Contains(RoesiaHOT))
				Body.Spells.Add(RoesiaHOT);
		}
		if (HasAggro)
		{
			if (Body.TargetObject != null)
			{
				if (Util.Chance(25))
				{
					if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.DamageReturn) && !Body.IsCasting)
						Body.CastSpell(RoesiaDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//Cast DS
				}
				if(Util.Chance(35))
                {
					if (Body.HealthPercent < 25)
						Body.CastSpell(RoesiaHOT, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast HOT
				}
				if (Util.Chance(35))
				{ 
					foreach (Spell spells in Body.Spells)
					{
						if (spells != null)
						{
							if (Body.attackComponent.AttackState && Body.IsCasting)
								Body.attackComponent.StopAttack();
							if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range) && Body.IsCasting)
								Body.StopFollowing();

							if(Body.GetSkillDisabledDuration(RoesiaDot) == 0)
								PickRandomTarget();

							GameLiving oldTarget = Body.TargetObject as GameLiving;
							if (RandomTarget != null && RandomTarget.IsAlive && CanCast)
							{								
								Body.TargetObject = RandomTarget;
								if (Body.GetSkillDisabledDuration(RoesiaDot) == 0 && !Body.IsCasting)
									Body.TurnTo(RandomTarget);
								Body.CastSpell(RoesiaDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);								
							}
							if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
						}
					}
				}
			}
		}
		base.Think();
	}
	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	public static bool CanCast = false;
	List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
	public void PickRandomTarget()
    {
		foreach(GamePlayer player in Body.GetPlayersInRadius(2000))
        {
			if(player != null)
            {
				if(player.IsAlive && player.Client.Account.PrivLevel==1)
                {
					if(!Enemys_To_DD.Contains(player))
						Enemys_To_DD.Add(player);
                }
            }
        }
		if(Enemys_To_DD.Count>0)
        {
			if (CanCast==false)
			{
				GamePlayer Target = Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
				RandomTarget = Target;//set random target to static RandomTarget
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetDot), 3000);
				CanCast = true;
			}				
		}
    }
	public int ResetDot(EcsGameTimer timer)//reset here so boss can start dot again
    {
		RandomTarget = null;
		CanCast = false;
		return 0;
    }
    #region Roesia Spells
    private Spell m_RoesiaDot;
	private Spell RoesiaDot
	{
		get
		{
			if (m_RoesiaDot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 585;
				spell.Icon = 585;
				spell.TooltipId = 585;
				spell.Damage = 150;
				spell.Frequency = 30;
				spell.Duration = 36;
				spell.DamageType = (int)EDamageType.Spirit;
				spell.Name = "Summoner Pain";
				spell.Description = "Inflicts 150 damage to the target every 3 sec for 36 seconds.";
				spell.Message1 = "Your body is covered with painful sores!";
				spell.Message2 = "{0}'s skin erupts in open wounds!";
				spell.Message3 = "The destructive energy wounding you fades.";
				spell.Message4 = "The destructive energy around {0} fades.";
				spell.Range = 1800;
				spell.Radius = 1000;
				spell.SpellID = 11756;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.Type = ESpellType.DamageOverTime.ToString();
				m_RoesiaDot = new Spell(spell, 50);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RoesiaDot);
			}
			return m_RoesiaDot;
		}
	}
	private Spell m_RoesiaHOT;
	private Spell RoesiaHOT
	{
		get
		{
			if (m_RoesiaHOT == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 45;
				spell.ClientEffect = 4414;
				spell.Icon = 4414;
				spell.TooltipId = 4414;
				spell.Value = 1250;
				spell.Frequency = 20;
				spell.Duration = 10;
				spell.Name = "Summoner Heal";
				spell.Description = "Causes the target to regain 2% health during the spell's duration.";
				spell.Message1 = "You start healing faster.";
				spell.Message2 = "{0} starts healing faster.";
				spell.Range = 1800;
				spell.SpellID = 11757;
				spell.Target = "Self";
				spell.Uninterruptible = true;
				spell.Type = ESpellType.HealOverTime.ToString();
				m_RoesiaHOT = new Spell(spell, 50);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RoesiaHOT);
			}
			return m_RoesiaHOT;
		}
	}
	private Spell m_RoesiaDS;
	private Spell RoesiaDS
	{
		get
		{
			if (m_RoesiaDS == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 300;
				spell.ClientEffect = 57;
				spell.Icon = 57;
				spell.Damage = 80;
				spell.Duration = 300;
				spell.Name = "Roesia Damage Shield";
				spell.TooltipId = 57;
				spell.SpellID = 11758;
				spell.Target = "Self";
				spell.Type = "DamageShield";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_RoesiaDS = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RoesiaDS);
			}
			return m_RoesiaDS;
		}
	}
    #endregion
}