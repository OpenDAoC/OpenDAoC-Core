using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.AI.Brains;

public class MortufoghusBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MortufoghusBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}

	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsTargetPicked = false;
			RandomTarget = null;
			if (Port_Enemys.Count > 0)
				Port_Enemys.Clear();
		}
		if(HasAggro && Body.TargetObject != null)
        {
			if(!Body.IsCasting && Util.Chance(50))
				Body.CastSpell(MortufoghusDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
			if (IsTargetPicked == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ThrowPlayer), Util.Random(25000, 35000));//timer to port and pick player
				IsTargetPicked = true;
			}
		}
		base.Think();
	}
	#region Throw Player
	List<GamePlayer> Port_Enemys = new List<GamePlayer>();
	public static bool IsTargetPicked = false;
	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	public int ThrowPlayer(EcsGameTimer timer)
	{
		if (Body.IsAlive && HasAggro)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Port_Enemys.Contains(player) && player != Body.TargetObject)
							Port_Enemys.Add(player);
					}
				}
			}
			if (Port_Enemys.Count > 0)
			{
				GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
				RandomTarget = Target;
				if (RandomTarget.IsAlive && RandomTarget != null)
				{
					RandomTarget.MoveTo(Body.CurrentRegionID, Body.X+Util.Random(-1500,1500), Body.Y + Util.Random(-1500, 1500), Body.Z, Body.Heading);
					RandomTarget.TakeDamage(RandomTarget, EDamageType.Falling, RandomTarget.MaxHealth / 5, 0);
					RandomTarget.Out.SendMessage("You take falling damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
					Port_Enemys.Remove(RandomTarget);
				}
			}
			RandomTarget = null;//reset random target to null
			IsTargetPicked = false;
		}
		return 0;
	}
	#endregion
	#region Spells
	private Spell m_MortufoghusDD;
	public Spell MortufoghusDD
	{
		get
		{
			if (m_MortufoghusDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 2;
				spell.RecastDelay = Util.Random(15,25);
				spell.ClientEffect = 14315;
				spell.Icon = 14315;
				spell.Damage = 250;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Dark Packt";
				spell.Range = 500;
				spell.Radius = 1000;
				spell.SpellID = 11891;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_MortufoghusDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MortufoghusDD);
			}
			return m_MortufoghusDD;
		}
	}
	#endregion
}