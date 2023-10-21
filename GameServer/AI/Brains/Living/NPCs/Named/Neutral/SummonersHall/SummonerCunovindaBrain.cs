using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.AI.Brains;

public class SummonerCunovindaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SummonerCunovindaBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
    public override void OnAttackedByEnemy(AttackData ad)
    {
		if(ad.Damage > 0 && ad != null)
        {
			if(Util.Chance(15))//here edit to change teleport chance to happen
				PickRandomTarget();//start teleport here
        }
        base.OnAttackedByEnemy(ad);
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
				Enemys_To_DD.Clear();//clear list if it reset
		}
		if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
		{
			Body.Health = Body.MaxHealth;
			CanCast = false;
			RandomTarget = null;
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
		foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
		{
			if (player != null)
			{
				if (player.IsAlive && player.Client.Account.PrivLevel == 1)
				{
					if (!Enemys_To_DD.Contains(player))
						Enemys_To_DD.Add(player);//add player to list
				}
			}
		}
		if (Enemys_To_DD.Count > 0)
		{
			if (CanCast == false)
			{
				GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
				RandomTarget = Target;//set random target to static RandomTarget
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastBolt), 1000);
				CanCast = true;
			}
		}
	}
	public int CastBolt(EcsGameTimer timer)
	{
		GameLiving oldTarget = (GameLiving)Body.TargetObject;//old target
		if (RandomTarget != null && RandomTarget.IsAlive)
		{
			Body.TurnTo(RandomTarget);//turn to randomtarget
			Body.StopFollowing();//stop follow
			Body.CastSpell(CunovindaBolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);//cast bolt

			RandomTarget.MoveTo(Body.CurrentRegionID, 24874, 36116, 17060, 3065);//port player to loc

			if(Body.TargetObject != null && Body.TargetObject != RandomTarget)
				Body.TargetObject = RandomTarget;//set target as randomtarget
		}
		if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetBolt), Util.Random(8000, 12000));//teleport every 8-12s if melee hit got chance to proc teleport
		return 0;
	}
	public int ResetBolt(EcsGameTimer timer)//reset here so boss can start dot again
	{
		RandomTarget = null;
		CanCast = false;
		return 0;
	}
	#region Cunovinda Spells
	private Spell m_CunovindaBolt;
	public Spell CunovindaBolt
	{
		get
		{
			if (m_CunovindaBolt == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 2970;
				spell.Icon = 2970;
				spell.TooltipId = 2970;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Summoner Bolt";
				spell.Range = 1800;
				spell.SpellID = 11761;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.Type = ESpellType.Bolt.ToString();
				m_CunovindaBolt = new Spell(spell, 50);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CunovindaBolt);
			}
			return m_CunovindaBolt;
		}
	}
	#endregion
}