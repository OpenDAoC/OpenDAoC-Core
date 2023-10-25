using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.AI;

#region Caithor
public class CaithorBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public CaithorBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 1200;
		ThinkInterval = 1500;
	}

	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
		}
		if(Body.TargetObject != null && HasAggro)
        {
			foreach (GameNpc npc in Body.GetNPCsInRadius(2000))
			{
				GameLiving target = Body.TargetObject as GameLiving;
				if (npc != null && npc.IsAlive)
				{
					if (npc.Brain is CaithorDorochaBrain brain)
					{
						if (brain != null && target != null && !brain.HasAggro && target.IsAlive)
							brain.AddToAggroList(target, 10);
					}
					if(npc.PackageID == "RealCaithorDorocha" && npc.Brain is StandardMobBrain brain2)
                    {
						if (brain2 != null && target != null && !brain2.HasAggro && target.IsAlive)
							brain2.AddToAggroList(target, 10);
					}

				}
			}
        }
		base.Think();
	}
}
#endregion Caithor

#region Ghost of Caithor
public class GhostOfCaithorBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public GhostOfCaithorBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 1200;
		ThinkInterval = 1500;
	}
	ushort oldModel;
	ENpcFlags oldFlags;
	bool changed;
	public static bool despawnGiantCaithor = false;
	public static bool CanDespawn = false;
	public override void Think()
	{
		if (CaithorDorocha.DorochaKilled >= 5 && !Caithor.RealCaithorUp)
		{
			if (changed)
			{
				Body.Flags = oldFlags;
				Body.Model = oldModel;
				changed = false;
			}
		}
		else
		{
			if (changed == false)
			{
				oldFlags = Body.Flags;
				Body.Flags ^= ENpcFlags.CANTTARGET;
				Body.Flags ^= ENpcFlags.DONTSHOWNAME;
				Body.Flags ^= ENpcFlags.PEACE;

				if (oldModel == 0)
					oldModel = Body.Model;

				Body.Model = 1;
				changed = true;
			}
		}
		if (!Body.InCombatInLast(30000) && !despawnGiantCaithor && Body.Model == 339)//5min
        {
			EcsGameTimer _despawnTimer2 = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DespawnGiantCaithor), 300000);//5min to despawn
			Body.TempProperties.SetProperty("giantcaithor_despawn2", _despawnTimer2);
			despawnGiantCaithor = true;
        }
		base.Think();
	}
	
	private int DespawnGiantCaithor(EcsGameTimer timer)
	{
		if (!HasAggro)
		{
			var despawnGiantCaithorTimer = Body.TempProperties.GetProperty<EcsGameTimer>("giantcaithor_despawn");
			if (despawnGiantCaithorTimer != null)
			{
				despawnGiantCaithorTimer.Stop();
				Body.TempProperties.RemoveProperty("giantcaithor_despawn");
			}				
			CaithorDorocha.DorochaKilled = 0;
			oldFlags = Body.Flags;
			Body.Flags ^= ENpcFlags.CANTTARGET;
			Body.Flags ^= ENpcFlags.DONTSHOWNAME;
			Body.Flags ^= ENpcFlags.PEACE;

			if (oldModel == 0)
				oldModel = Body.Model;

			Body.Model = 1;
			changed = true;
		}
		despawnGiantCaithor = false;
		return 0;
	}
}
#endregion Ghost of Caithor

#region Caithor far dorochas
public class CaithorDorochaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public CaithorDorochaBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Caithor far dorochas