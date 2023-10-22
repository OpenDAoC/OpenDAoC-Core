using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI.Brains;

#region Summoner Lossren
public class SummonerLossrenBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SummonerLossrenBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsCreatingSouls = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			TorturedSouls.TorturedSoulKilled = 0;
			TorturedSouls.TorturedSoulCount = 0;
			if (!RemoveAdds)
			{
				foreach (GameNpc souls in Body.GetNPCsInRadius(5000))
				{
					if (souls != null)
					{
						if (souls.IsAlive && (souls.Brain is TorturedSoulsBrain || souls.Brain is ExplodeUndeadBrain))
						{
							souls.RemoveFromWorld();
						}
					}
				}
				RemoveAdds = true;
			}
		}
		if (Body.InCombat && Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			if(IsCreatingSouls==false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DoSpawn), Util.Random(5000, 8000));//every 5-8s it will spawn tortured souls
				IsCreatingSouls =true;
            }
			foreach(GameNpc souls in Body.GetNPCsInRadius(4000))
            {
				if(souls != null)
                {
					if(souls.IsAlive && souls.Brain is TorturedSoulsBrain)
                    {
						AddAggroListTo(souls.Brain as TorturedSoulsBrain);
					}
                }
            }
		}
		if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
		{
			Body.Health = Body.MaxHealth;
		}
		base.Think();
	}
	public int DoSpawn(EcsGameTimer timer)
    {
		if (Body.InCombat && Body.IsAlive && HasAggro)
		{
			if(TorturedSouls.TorturedSoulCount == 0)
				SpawnSouls();
		}
		SpawnBigZombie();
		IsCreatingSouls = false;
		return 0;
    }
	public void SpawnSouls()
	{
		Point3D point1 = new Point3D(39189, 41889, 16000);
		Point3D point2 = new Point3D(38505, 41211, 16001);
		Point3D point3 = new Point3D(39180, 40583, 16000);
		Point3D point4 = new Point3D(39745, 41176, 16001);

		for (int i = 0; i < Util.Random(18, 25); i++)//create 18-25 souls every time timer will launch
		{

			TorturedSouls add = new TorturedSouls();
			switch (Util.Random(1, 4))
			{
				case 1:
					{
						add.X = point1.X + Util.Random(-100, 100);
						add.Y = point1.Y + Util.Random(-100, 100);
						add.Z = point1.Z;
					}
					break;
				case 2:
					{
						add.X = point2.X + Util.Random(-100, 100);
						add.Y = point2.Y + Util.Random(-100, 100);
						add.Z = point2.Z;
					}
					break;
				case 3:
					{
						add.X = point3.X + Util.Random(-100, 100);
						add.Y = point3.Y + Util.Random(-100, 100);
						add.Z = point3.Z;
					}
					break;
				case 4:
					{
						add.X = point4.X + Util.Random(-100, 100);
						add.Y = point4.Y + Util.Random(-100, 100);
						add.Z = point4.Z;
					}
					break;
			}
			add.CurrentRegion = Body.CurrentRegion;
			add.Heading = Body.Heading;
			add.AddToWorld();
			add.LoadedFromScript = true;
			++TorturedSouls.TorturedSoulCount;
		}
	}
	public void SpawnBigZombie()
    {
		Point3D point1 = new Point3D(39189, 41889, 16000);
		Point3D point2 = new Point3D(38505, 41211, 16001);
		Point3D point3 = new Point3D(39180, 40583, 16000);
		Point3D point4 = new Point3D(39745, 41176, 16001);
		if (TorturedSouls.TorturedSoulKilled == 20 && ExplodeUndead.ExplodeZombieCount==0)//spawn explode zombie
		{
			ExplodeUndead add2 = new ExplodeUndead();
			switch (Util.Random(1, 4))
			{
				case 1:
					{
						add2.X = point1.X + Util.Random(-100, 100);
						add2.Y = point1.Y + Util.Random(-100, 100);
						add2.Z = point1.Z;
					}
					break;
				case 2:
					{
						add2.X = point2.X + Util.Random(-100, 100);
						add2.Y = point2.Y + Util.Random(-100, 100);
						add2.Z = point2.Z;
					}
					break;
				case 3:
					{
						add2.X = point3.X + Util.Random(-100, 100);
						add2.Y = point3.Y + Util.Random(-100, 100);
						add2.Z = point3.Z;
					}
					break;
				case 4:
					{
						add2.X = point4.X + Util.Random(-100, 100);
						add2.Y = point4.Y + Util.Random(-100, 100);
						add2.Z = point4.Z;
					}
					break;
			}
			add2.CurrentRegion = Body.CurrentRegion;
			add2.Heading = Body.Heading;
			add2.AddToWorld();
			add2.LoadedFromScript = true;
			TorturedSouls.TorturedSoulKilled = 0;
			++ExplodeUndead.ExplodeZombieCount;
		}
	}
}
#endregion Summoner Lossren

#region Lossren adds
public class TorturedSoulsBrain : StandardMobBrain
{
	public TorturedSoulsBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 800;
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
		}
		base.Think();
	}
}

public class ExplodeUndeadBrain : StandardMobBrain
{
	public ExplodeUndeadBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 800;
	}
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public static bool IsKilled = false;
	public static bool SetAggroAmount = false;
	public override void Think()
	{
		if (Body.IsAlive)
		{
			if (Body.TargetObject == null && ExplodeUndead.RandomTarget != null)
			{
				Body.TargetObject = ExplodeUndead.RandomTarget;
			}
			if (Body.TargetObject != null)
			{
				if (Body.IsWithinRadius(Body.TargetObject, Body.AttackRange))
				{
					if (IsKilled == false)
					{
						Body.CastSpell(Zombie_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(KillZombie), 500);
						IsKilled = true;
					}
				}
			}
		}
		if (Body.IsAlive && ExplodeUndead.RandomTarget != null )
        {
			if (SetAggroAmount == false)
			{
				AddToAggroList(ExplodeUndead.RandomTarget, 2000);
				SetAggroAmount = true;
			}
		}
		base.Think();
	}
	public int KillZombie(EcsGameTimer timer)
    {
		Body.Die(Body);
		return 0;
    }
	private Spell m_Zombie_aoe;
	private Spell Zombie_aoe
	{
		get
		{
			if (m_Zombie_aoe == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 30;
				spell.ClientEffect = 6159;
				spell.Icon = 6159;
				spell.TooltipId = 6169;
				spell.Damage = 1000;
				spell.Name = "Plague";
				spell.Radius = 500;
				spell.Range = 500;
				spell.SpellID = 11760;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Zombie_aoe = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Zombie_aoe);
			}
			return m_Zombie_aoe;
		}
	}
}
#endregion Lossren adds