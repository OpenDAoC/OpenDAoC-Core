using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain;

#region Myrddraxis
public class MyrddraxisBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MyrddraxisBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsPulled = false;
	public static bool CanCast = false;
	public static bool CanCast2 = false;
	public static bool CanCastStun1 = false;
	public static bool CanCastStun2 = false;
	public static bool CanCastStun3 = false;
	public static bool CanCastStun4 = false;
	public static bool CanCastPBAOE1 = false;
	public static bool CanCastPBAOE2 = false;
	public static bool CanCastPBAOE3 = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
	}
	#region Hydra DOT
	public static GamePlayer randomtarget2 = null;
	public static GamePlayer RandomTarget2
	{
		get { return randomtarget2; }
		set { randomtarget2 = value; }
	}
	List<GamePlayer> Enemys_To_DOT = new List<GamePlayer>();
	public int PickRandomTarget2(EcsGameTimer timer)
	{
		if (HasAggro)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_DOT.Contains(player))
							Enemys_To_DOT.Add(player);
					}
				}
			}
			if (Enemys_To_DOT.Count > 0)
			{
				if (CanCast2 == false)
				{
					GamePlayer Target = (GamePlayer)Enemys_To_DOT[Util.Random(0, Enemys_To_DOT.Count - 1)];//pick random target from list
					RandomTarget2 = Target;//set random target to static RandomTarget
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDOT), 2000);
					CanCast2 = true;
				}
			}
		}
		return 0;
	}
	public int CastDOT(EcsGameTimer timer)
	{
		if (HasAggro && RandomTarget2 != null)
		{
			GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
			if (RandomTarget2 != null && RandomTarget2.IsAlive)
			{
				Body.TargetObject = RandomTarget2;
				Body.TurnTo(RandomTarget2);
				Body.CastSpell(Hydra_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetDOT), 5000);
		}
		return 0;
	}
	public int ResetDOT(EcsGameTimer timer)
	{
		RandomTarget2 = null;
		CanCast2 = false;
		StartCastDOT = false;
		return 0;
	}
	#endregion
	#region Hydra DD
	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
	public int PickRandomTarget(EcsGameTimer timer)
	{
		if (HasAggro)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_DD.Contains(player))
							Enemys_To_DD.Add(player);
					}
				}
			}
			if (Enemys_To_DD.Count > 0)
			{
				if (CanCast == false)
				{
					GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDD), 5000);
					BroadcastMessage(String.Format(Body.Name + " taking a big flame breath at " + RandomTarget.Name + "."));
					CanCast = true;
				}
			}
		}
		return 0;
	}
	public int CastDD(EcsGameTimer timer)
	{
		if (HasAggro && RandomTarget != null)
		{
			GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
			if (RandomTarget != null && RandomTarget.IsAlive)
			{
				Body.TargetObject = RandomTarget;
				Body.TurnTo(RandomTarget);
				Body.CastSpell(Hydra_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetDD), 5000);
		}
		return 0;
	}
	public int ResetDD(EcsGameTimer timer)
	{
		RandomTarget = null;
		CanCast = false;
		StartCastDD = false;
		return 0;
	}
    #endregion
    #region Hydra Stun
	public int HydraStun(EcsGameTimer timer)
    {
		if(HasAggro && Body.IsAlive)
			Body.CastSpell(Hydra_Stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		return 0;
    }
	#endregion
	#region Hydra PBAOE
	public int HydraPBAOE(EcsGameTimer timer)
	{
		if (HasAggro && Body.IsAlive)
			Body.CastSpell(Hydra_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		return 0;
	}
	#endregion
	public static bool StartCastDD = false;
	public static bool StartCastDOT = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled = false;
			StartCastDD = false;
			StartCastDOT = false;
			CanCast = false;
			CanCast2 = false;
			RandomTarget = null;
			RandomTarget2 = null;
			CanCastStun1 = false;
			CanCastStun2 = false;
			CanCastStun3 = false;
			CanCastStun4 = false;
			CanCastPBAOE1 = false;
			CanCastPBAOE2 = false;
			CanCastPBAOE3 = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null)
					{
						if (MyrddraxisSecondHead.SecondHeadCount == 0)
						{
							MyrddraxisSecondHead Add1 = new MyrddraxisSecondHead();
							Add1.X = 32384;
							Add1.Y = 31942;
							Add1.Z = 15931;
							Add1.CurrentRegion = Body.CurrentRegion;
							Add1.Heading = 455;
							Add1.Flags = ENpcFlags.FLYING;
							Add1.RespawnInterval = -1;
							Add1.AddToWorld();
						}
						if (MyrddraxisThirdHead.ThirdHeadCount == 0)
						{
							MyrddraxisThirdHead Add2 = new MyrddraxisThirdHead();
							Add2.X = 32187;
							Add2.Y = 32205;
							Add2.Z = 15961;
							Add2.CurrentRegion = Body.CurrentRegion;
							Add2.Heading = 4095;
							Add2.Flags = ENpcFlags.FLYING;
							Add2.RespawnInterval = -1;
							Add2.AddToWorld();
						}
						if (MyrddraxisFourthHead.FourthHeadCount == 0)
						{
							MyrddraxisFourthHead Add3 = new MyrddraxisFourthHead();
							Add3.X = 32371;
							Add3.Y = 32351;
							Add3.Z = 15936;
							Add3.CurrentRegion = Body.CurrentRegion;
							Add3.Heading = 971;
							Add3.Flags = ENpcFlags.FLYING;
							Add3.RespawnInterval = -1;
							Add3.AddToWorld();
						}
						if (MyrddraxisFifthHead.FifthHeadCount == 0)
						{
							MyrddraxisFifthHead Add4 = new MyrddraxisFifthHead();
							Add4.X = 32576;
							Add4.Y = 32133;
							Add4.Z = 15936;
							Add4.CurrentRegion = Body.CurrentRegion;
							Add4.Heading = 4028;
							Add4.Flags = ENpcFlags.FLYING;
							Add4.RespawnInterval = -1;
							Add4.AddToWorld();
						}
					}
				}
				RemoveAdds = true;
			}
		}
		if (Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			if(StartCastDD==false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickRandomTarget), Util.Random(35000, 45000));
				StartCastDD = true;
			}
			if (StartCastDOT == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickRandomTarget2), Util.Random(35000, 45000));
				StartCastDOT = true;
			}
			if (IsPulled == false)
			{
				GameLiving ptarget = Body.TargetObject as GameLiving;
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisSecondHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisThirdHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisFourthHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisFifthHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				IsPulled = true;
			}
			#region Hydra Stun
			if (Body.HealthPercent <= 80 && CanCastStun1==false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(HydraStun), 5000);
				BroadcastMessage(String.Format(Body.Name + " prepares stunning breath."));
				CanCastStun1 = true;
            }
			else if (Body.HealthPercent <= 60 && CanCastStun2 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(HydraStun), 5000);
				BroadcastMessage(String.Format(Body.Name + " prepares stunning breath."));
				CanCastStun2 = true;
			}
			else if (Body.HealthPercent <= 40 && CanCastStun3 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(HydraStun), 5000);
				BroadcastMessage(String.Format(Body.Name + " prepares stunning breath."));
				CanCastStun3 = true;
			}
			else if (Body.HealthPercent <= 20 && CanCastStun4 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(HydraStun), 5000);
				BroadcastMessage(String.Format(Body.Name + " prepares stunning breath."));
				CanCastStun4 = true;
			}
			#endregion
			#region Hydra PBAOE
			if (Body.HealthPercent <= 75 && CanCastPBAOE1 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(HydraPBAOE), 6000);
				BroadcastMessage(String.Format(Body.Name + " taking a massive breath of flames to annihilate enemys."));
				CanCastPBAOE1 = true;
			}
			else if (Body.HealthPercent <= 50 && CanCastPBAOE2 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(HydraPBAOE), 6000);
				BroadcastMessage(String.Format(Body.Name + " taking a massive breath of flames to annihilate enemys."));
				CanCastPBAOE2 = true;
			}
			else if (Body.HealthPercent <= 25 && CanCastPBAOE3 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(HydraPBAOE), 6000);
				BroadcastMessage(String.Format(Body.Name + " taking a massive breath of flames to annihilate enemys."));
				CanCastPBAOE3 = true;
			}
			#endregion
			GameLiving target = Body.TargetObject as GameLiving;
			if(target != null && !target.IsWithinRadius(Body,Body.AttackRange))
            {
				Body.SetGroundTarget(target.X, target.Y, target.Z);
				Body.CastSpell(Hydra_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast dmg if main target is not in attack range
			}
		}
		base.Think();
	}
	#region Spells
	private Spell m_Hydra_Dot;
	private Spell Hydra_Dot
	{
		get
		{
			if (m_Hydra_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4445;
				spell.Icon = 4445;
				spell.TooltipId = 4445;
				spell.Damage = 90;
				spell.Duration = 40;
				spell.Frequency = 40;
				spell.Name = "Myrddraxis Poison";
				spell.Range = 2000;
				spell.Radius = 800;
				spell.SpellID = 11849;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Body;
				m_Hydra_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_Dot);
			}
			return m_Hydra_Dot;
		}
	}
	private Spell m_Hydra_DD;
	private Spell Hydra_DD
	{
		get
		{
			if (m_Hydra_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5700;
				spell.Icon = 5700;
				spell.TooltipId = 5700;
				spell.Damage = 1100;
				spell.Name = "Myrddraxis Breath of Flame";
				spell.Range = 2000;
				spell.Radius = 450;
				spell.SpellID = 11840;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_Hydra_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_DD);
			}
			return m_Hydra_DD;
		}
	}
	private Spell m_Hydra_DD2;
	private Spell Hydra_DD2
	{
		get
		{
			if (m_Hydra_DD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 3;
				spell.ClientEffect = 5700;
				spell.Icon = 5700;
				spell.TooltipId = 5700;
				spell.Damage = 450;
				spell.Name = "Myrddraxis Breath of Flame";
				spell.Range = 2000;
				spell.Radius = 200;
				spell.SpellID = 11850;
				spell.Target = "Area";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_Hydra_DD2 = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_DD2);
			}
			return m_Hydra_DD2;
		}
	}
	private Spell m_Hydra_PBAOE;
	private Spell Hydra_PBAOE
	{
		get
		{
			if (m_Hydra_PBAOE == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5700;
				spell.Icon = 5700;
				spell.TooltipId = 5700;
				spell.Damage = 2000;
				spell.Name = "Myrddraxis Breath of Annihilation";
				spell.Range = 0;
				spell.Radius = 1800;
				spell.SpellID = 11841;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_Hydra_PBAOE = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_PBAOE);
			}
			return m_Hydra_PBAOE;
		}
	}
	private Spell m_Hydra_Stun;
	private Spell Hydra_Stun
	{
		get
		{
			if (m_Hydra_Stun == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5703;
				spell.Icon = 5703;
				spell.TooltipId = 5703;
				spell.Duration = 30;
				spell.Name = "Myrddraxis Stun";
				spell.Range = 0;
				spell.Radius = 1800;
				spell.SpellID = 11842;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Stun.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Body;
				m_Hydra_Stun = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_Stun);
			}
			return m_Hydra_Stun;
		}
	}
	#endregion
}
#endregion Myrddraxis

#region 2nd Head of Myrddraxis
public class MyrddraxisSecondHeadBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MyrddraxisSecondHeadBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsPulled1 = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled1 = false;
		}
		if (Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			Body.CastSpell(Head2_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (IsPulled1==false)
			{
				GameLiving ptarget = Body.TargetObject as GameLiving;
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisThirdHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisFourthHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisFifthHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				IsPulled1 = true;
			}
		}
		base.Think();
	}
	#region spells
	private Spell m_Head2_DD;
	private Spell Head2_DD
	{
		get
		{
			if (m_Head2_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(5,8);
				spell.ClientEffect = 4159;
				spell.Icon = 4159;
				spell.TooltipId = 4159;
				spell.Damage = 350;
				spell.Name = "Breath of Darkness";
				spell.Range = 2000;
				spell.SpellID = 11845;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Cold;
				m_Head2_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Head2_DD);
			}
			return m_Head2_DD;
		}
	}
	#endregion
}
#endregion 2nd Head of Myrddraxis

#region 3rd Head of Myrddraxis
public class MyrddraxisThirdHeadBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MyrddraxisThirdHeadBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsPulled2 = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled2 = false;
		}
		if (Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			Body.CastSpell(Head3_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (IsPulled2==false)
			{
				GameLiving ptarget = Body.TargetObject as GameLiving;
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisSecondHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisFourthHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisFifthHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				IsPulled2 = true;
			}
		}
		base.Think();
	}
	#region spells
	private Spell m_Head3_DD;
	private Spell Head3_DD
	{
		get
		{
			if (m_Head3_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(5,8);
				spell.ClientEffect = 360;
				spell.Icon = 360;
				spell.TooltipId = 360;
				spell.Damage = 350;
				spell.Name = "Breath of Flame";
				spell.Range = 2000;
				spell.SpellID = 11846;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_Head3_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Head3_DD);
			}
			return m_Head3_DD;
		}
	}
	#endregion
}
#endregion 3rd Head of Myrddraxis

#region 4th Head of Myrddraxis
public class MyrddraxisFourthHeadBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MyrddraxisFourthHeadBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsPulled3 = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled3 = false;
		}
		if (Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			Body.CastSpell(Head4_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (IsPulled3==false)
			{
				GameLiving ptarget = Body.TargetObject as GameLiving;
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisSecondHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisThirdHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisFifthHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				IsPulled3 = true;
			}
		}
		base.Think();
	}
	#region spells
	private Spell m_Head4_DD;
	private Spell Head4_DD
	{
		get
		{
			if (m_Head4_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(5, 8);
				spell.ClientEffect = 759;
				spell.Icon = 759;
				spell.TooltipId = 759;
				spell.Damage = 350;
				spell.Name = "Breath of Spirit";
				spell.Range = 2000;
				spell.SpellID = 11847;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_Head4_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Head4_DD);
			}
			return m_Head4_DD;
		}
	}
	#endregion
}
#endregion 4th Head of Myrddraxis

#region 5th Head of Myrddraxis
public class MyrddraxisFifthHeadBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MyrddraxisFifthHeadBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsPulled4 = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled4 = false;
		}
		if (Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			Body.CastSpell(Head5_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (IsPulled4==false)
			{
				GameLiving ptarget = Body.TargetObject as GameLiving;
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisSecondHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisThirdHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisFourthHeadBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				foreach (GameNpc head in Body.GetNPCsInRadius(2000))
				{
					if (head != null)
					{
						if (head.IsAlive && head.Brain is MyrddraxisBrain brain)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(ptarget, 10);
						}
					}
				}
				IsPulled4 = true;
			}
		}
		base.Think();
	}
	#region spells
	private Spell m_Head5_DD;
	private Spell Head5_DD
	{
		get
		{
			if (m_Head5_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(5, 8);
				spell.ClientEffect = 219;
				spell.Icon = 219;
				spell.TooltipId = 219;
				spell.Damage = 350;
				spell.Name = "Breath of Matter";
				spell.Range = 2000;
				spell.SpellID = 11848;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Head5_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Head5_DD);
			}
			return m_Head5_DD;
		}
	}
	#endregion
}
#endregion 5th Head of Myrddraxis