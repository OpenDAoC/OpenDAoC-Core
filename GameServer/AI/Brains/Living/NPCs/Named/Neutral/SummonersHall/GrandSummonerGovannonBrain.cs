using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.AI.Brains;

#region Grand Summoner Govannon
public class GrandSummonerGovannonBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public GrandSummonerGovannonBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool SpawnSacrifices1 = false;
	public static bool Stage2 = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
	}
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Stage2 = false;
			SpawnSacrifices1 = false;
			Body.Health = Body.MaxHealth;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && (npc.Brain is SummonedDemonBrain || npc.Brain is SummonedSacrificeBrain || npc.Brain is ShadeOfAelfgarBrain))
							npc.RemoveFromWorld();
					}
				}
				RemoveAdds = true;
			}
		}
		if (Body.InCombat && Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			if(Stage2==true)//demon form
				Body.CastSpell(GovannonDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			SpawnShadeOfAelfgar();
			foreach (GameNpc shade in Body.GetNPCsInRadius(4000))
			{
				if (shade != null)
				{
					if (shade.IsAlive && shade.Brain is ShadeOfAelfgarBrain)
						AddAggroListTo(shade.Brain as ShadeOfAelfgarBrain);
				}
			}
			if (Body.HealthPercent <= 80 && SpawnSacrifices1 == false)
			{
				if (Stage2 == false)
				{
					BroadcastMessage(String.Format(Body.Name + " gathers more strength."));
					Body.Strength = 650;
					Body.Size = 80;
				}
				foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(Body, Body, 14215, 0, false, 1);
				}
				SpawnDemonAndSacrifice();
				SpawnSacrifices1 = true;
			}
			if(Body.HealthPercent <= 50 && Stage2==false)
            {
				MorphIntoDemon();
				Stage2 = true;
            }
		}
		if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18801);
			Body.Health = Body.MaxHealth;
			Body.Model = Convert.ToUInt16(npcTemplate.Model);
			Body.Size = Convert.ToByte(npcTemplate.Size);
			Body.Strength = Convert.ToInt16(npcTemplate.Strength);
		}
		base.Think();
	}
	public void MorphIntoDemon()
    {
		Body.Health = Body.MaxHealth;//heal to max hp
		Body.Model = 636;//demon model
		Body.Size = 170;//bigger size
		Body.Strength = 800;//more dmg
		SpawnSacrifices1 = false;
    }
	public void SpawnDemonAndSacrifice() // spawn sacrifice and demon
	{
		SummonedSacrifice Add1 = new SummonedSacrifice();
		Add1.X = 31018;
		Add1.Y = 40889;
		Add1.Z = 15491;
		Add1.CurrentRegionID = 248;
		Add1.Heading = 3054;
		Add1.LoadedFromScript = true;
		Add1.Faction = FactionMgr.GetFactionByID(187);
		Add1.AddToWorld();

		SummonedDemon Add2 = new SummonedDemon();
		Add2.X = 33215;
		Add2.Y = 40883;
		Add2.Z = 15491;
		Add2.CurrentRegionID = 248;
		Add2.Heading = 1004;
		Add2.LoadedFromScript = true;
		Add2.Faction = FactionMgr.GetFactionByID(187);
		Add2.AddToWorld();
	}
	public void SpawnShadeOfAelfgar()
    {
		if (SummonedSacrifice.SacrificeKilledCount == 1 && SummonedDemon.SummonedDemonCount == 1)//both summoned demon and sacrifice must be killed
		{
			if (ShadeOfAelfgar.ShadeOfAelfgarCount == 0)//make sure there is only 1 always
			{
				ShadeOfAelfgar Add1 = new ShadeOfAelfgar();
				Add1.X = 32128;
				Add1.Y = 41667;
				Add1.Z = 15491;
				Add1.CurrentRegionID = 248;
				Add1.Heading = 2030;
				Add1.LoadedFromScript = true;
				Add1.Faction = FactionMgr.GetFactionByID(187);
				Add1.AddToWorld();
				SummonedDemon.SummonedDemonCount = 0;
				SummonedSacrifice.SacrificeKilledCount = 0;
			}
		}
	}
	private Spell m_GovannonDot;
	public Spell GovannonDot
	{
		get
		{
			if (m_GovannonDot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 4;
				spell.RecastDelay = Util.Random(20,35);
				spell.ClientEffect = 585;
				spell.Icon = 585;
				spell.TooltipId = 585;
				spell.Damage = 120;
				spell.Frequency = 20;
				spell.Duration = 24;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Name = "Govannon's Shroud of Agony";
				spell.Description = "Inflicts 150 damage to the target every 3 sec for 36 seconds.";
				spell.Message1 = "Your body is covered with painful sores!";
				spell.Message2 = "{0}'s skin erupts in open wounds!";
				spell.Message3 = "The destructive energy wounding you fades.";
				spell.Message4 = "The destructive energy around {0} fades.";
				spell.Range = 1500;
				spell.Radius = 300;
				spell.SpellID = 11763;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.Type = ESpellType.DamageOverTime.ToString();
				m_GovannonDot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GovannonDot);
			}
			return m_GovannonDot;
		}
	}
}
#endregion Grand Summoner Govannon

#region Summoned Sacrifice
public class SummonedSacrificeBrain : StandardMobBrain
{
	public SummonedSacrificeBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 0;
	}
	public override void AttackMostWanted()// mob doesnt attack
	{
	}
	public override void OnAttackedByEnemy(AttackData ad)//another check to not attack enemys
	{
		base.OnAttackedByEnemy(ad);
	}
	public override void Think()
	{
		Point3D point1 = new Point3D(32063, 40896, 15468);
		if(!Body.IsWithinRadius(point1,40))
		{
			Body.WalkTo(point1, 35);
		}
		else
		{
			SummonedSacrifice.SacrificeKilledCount = 1;
			Body.Die(Body);//is at point so it die
		}
		base.Think();
	}
}
#endregion Summoned Sacrifice

#region Summoned Demon
public class SummonedDemonBrain : StandardMobBrain
{
	public SummonedDemonBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 0;
	}
	public override void AttackMostWanted()// mob doesnt attack
	{
	}
	public override void OnAttackedByEnemy(AttackData ad)//another check to not attack enemys
	{
		base.OnAttackedByEnemy(ad);
	}
	public override void Think()
	{
		Point3D point1 = new Point3D(32063, 40896, 15468);
		if (!Body.IsWithinRadius(point1, 40))
		{
			Body.WalkTo(point1, 35);
		}
		else
		{
			SummonedDemon.SummonedDemonCount = 1;
			Body.Die(Body);//is at point so it die
		}
		base.Think();
	}
}
#endregion Summoned Demon

#region Shade of Aelfgar
public class ShadeOfAelfgarBrain : StandardMobBrain
{
	public ShadeOfAelfgarBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 800;
	}
	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	List<GamePlayer> Enemys_To_Port = new List<GamePlayer>();
	public static bool CanPort = false;
	public void PickRandomTarget()
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
		{
			if (player != null)
			{
				if (player.IsAlive && player.Client.Account.PrivLevel == 1)
				{
					if (!Enemys_To_Port.Contains(player))
						Enemys_To_Port.Add(player);//add player to list
				}
			}
		}
		if (Enemys_To_Port.Count > 0)
		{
			if (CanPort == false)
			{
				GamePlayer Target = (GamePlayer)Enemys_To_Port[Util.Random(0, Enemys_To_Port.Count - 1)];//pick random target from list
				RandomTarget = Target;//set random target to static RandomTarget
				RandomTarget.MoveTo(Body.CurrentRegionID, 32091, 39684, 16302, 4094);
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetPort), Util.Random(10000,20000));//port every 10-20s
				CanPort = true;
			}
		}
	}
	public int ResetPort(EcsGameTimer timer)//reset here so boss can start dot again
	{
		RandomTarget = null;
		CanPort = false;
		return 0;
	}
	public override void Think()
	{
		if(!CheckProximityAggro())
		{
			RandomTarget = null;
			CanPort = false;
			if (Enemys_To_Port.Count > 0)
				Enemys_To_Port.Clear();//clear list if it reset
		}
		if(Body.IsAlive && Body.InCombat && HasAggro && Body.TargetObject != null)
		{
			if(Util.Chance(5))
				PickRandomTarget();
		}
		base.Think();
	}
}
#endregion Shade of Aelfgar