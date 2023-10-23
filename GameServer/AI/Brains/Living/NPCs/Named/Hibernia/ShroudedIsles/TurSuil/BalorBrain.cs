using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

#region Balor
public class BalorBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public BalorBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public static bool spawn_eye = false;
    public void SpawnEyeOfBalor()
    {
        if (BalorEye.EyeCount == 0)
        {
            BalorEye Add1 = new BalorEye();
            Add1.X = Body.X;
            Add1.Y = Body.Y + 80;
            Add1.Z = Body.Z + 347;//at mob head heigh with boss size 105
            Add1.CurrentRegion = Body.CurrentRegion;
            Add1.Heading = Body.Heading;
            Add1.RespawnInterval = -1;
            Add1.AddToWorld();
        }
    }
    private int m_stage = 10;
    /// <summary>
    /// This keeps track of the stage the encounter is in.
    /// </summary>
    private int Stage
    {
        get { return m_stage; }
        set
        {
            if (value >= 0 && value <= 10) m_stage = value;
        }
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (Body.HealthPercent == 100 && Stage < 10 && !HasAggro)
            Stage = 10;

        if (Body.InCombat && Body.IsAlive && HasAggro)
        {
            int health = Body.HealthPercent / 10;
            if(health < Stage)
            {
                switch (health)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:	
                    {
                        SpawnEyeOfBalor();
                    }
                        break;
                }
                Stage = health;
            }

        }
        base.Think();
    }
}
#endregion Balor

#region Balor's Eye
public class BalorEyeBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public BalorEyeBrain()
		: base()
	{
		AggroLevel = 0;
		AggroRange = 1500;
		ThinkInterval = 500;
	}
	public static bool PickTarget = false;
	public static bool Cancast = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
	}
	public override void Think()
	{
		if (Body.IsAlive)
		{
			if (PickTarget == false)
			{
				PickRandomTarget();
				PickTarget = true;
			}
			if (HasAggro && Cancast && Body.TargetObject != null) //&& RandomTarget != null && RandomTarget.IsAlive && !Body.IsCasting)
			{
				if(Body.TargetObject != RandomTarget)
					Body.TargetObject = RandomTarget;
				Body.CastSpell(EyeDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
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
	List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
	public void PickRandomTarget()
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
		{
			if (player != null)
			{
				if (player.IsAlive && player.Client.Account.PrivLevel == 1)
				{
					if (!Enemys_To_DD.Contains(player) && !AggroTable.ContainsKey(player))
					{
						Enemys_To_DD.Add(player);
						AddToAggroList(player, 10);//make sure it will cast spell
					}
				}
			}
		}
		if (Enemys_To_DD.Count > 0)
		{
			GamePlayer Target = Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
			RandomTarget = Target;//set random target to static RandomTarget
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(StartCast), 3000);
		}
	}
	public int StartCast(EcsGameTimer timer)
    {
		Cancast = true;
		return 0;
    }
	private Spell m_EyeDD;
	private Spell EyeDD
	{
		get
		{
			if (m_EyeDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 4;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4111;
				spell.Icon = 4111;
				spell.TooltipId = 4111;
				spell.Damage = 1000;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Balor's Eye Light";
				spell.Range = 1800;
				spell.SpellID = 11791;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_EyeDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_EyeDD);
			}
			return m_EyeDD;
		}
	}
}
#endregion Balor's Eye