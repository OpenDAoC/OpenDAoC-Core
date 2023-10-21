namespace Core.GS.AI.Brains;

public class WrathOfMordredBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public WrathOfMordredBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	private bool CanWalk = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			CanWalk = false;
		}
		if (Body.IsAlive && HasAggro)
		{
			if (Body.TargetObject != null)
			{
				GameLiving living = Body.TargetObject as GameLiving;
				float angle = Body.TargetObject.GetAngle(Body);
				if ((angle >= 45 && angle < 150) || (angle >= 210 && angle < 315))//side
				{
					Body.styleComponent.NextCombatBackupStyle = WrathOfMordred.Side2H;
					Body.styleComponent.NextCombatStyle = WrathOfMordred.SideFollowUP;
				}
				if (!living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
				{
					AttackAction attackAction = Body.attackComponent.attackAction;

					if (attackAction != null && attackAction.StartTime - GameLoop.GameLoopTime <= 800 && CanWalk == false)
					{
						Body.styleComponent.NextCombatStyle = null;
						Body.styleComponent.NextCombatBackupStyle = null;
						Body.styleComponent.NextCombatBackupStyle = WrathOfMordred.Side2H;
						Body.styleComponent.NextCombatStyle = WrathOfMordred.SideFollowUP;
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkSide), 300);
						CanWalk = true;
					}
				}
			}
		}
		base.Think();
	}
	private int WalkSide(EcsGameTimer timer)
	{
		if (Body.IsAlive && HasAggro && Body.TargetObject != null && Body.IsWithinRadius(Body.TargetObject, Body.AttackRange))
		{
			if (Body.TargetObject is GameLiving)
			{
				GameLiving living = Body.TargetObject as GameLiving;
				float angle = living.GetAngle(Body);
				Point2D positionalPoint;
				positionalPoint = living.GetPointFromHeading((ushort)(living.Heading + (90 * (4096.0 / 360.0))), 65);
				//Body.WalkTo(positionalPoint.X, positionalPoint.Y, living.Z, 280);
				Body.X = positionalPoint.X;
				Body.Y = positionalPoint.Y;
				Body.Z = living.Z;
				Body.Heading = 1250;
			}
		}
		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetWalkSide), Util.Random(15000,25000));
		return 0;
	}
	private int ResetWalkSide(EcsGameTimer timer)
    {
		CanWalk = false;
		return 0;
    }
}