using System;
using Core.AI.Brain;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class FearBrain : StandardMobBrain
{
	/// <summary>
	/// Fixed thinking Interval for Fleeing
	/// </summary>
	public override int ThinkInterval {
		get {
			return 3000;
		}
	}
	
	/// <summary>
	/// Flee from Players on Brain Think
	/// </summary>
	public override void Think()
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)Math.Max(AggroRange, 750)))
		{
			CalculateFleeTarget(player);
			break;
		}
	}

	///<summary>
	/// Calculate flee target.
	/// </summary>
	///<param name="target">The target to flee.</param>
	protected virtual void CalculateFleeTarget(GameLiving target)
	{
		ushort TargetAngle = (ushort)((Body.GetHeading(target) + 2048) % 4096);

        Point2D fleePoint = Body.GetPointFromHeading(TargetAngle, 300);
		Body.StopFollowing();
		Body.StopAttack();
		Body.WalkTo(new Point3D(fleePoint.X, fleePoint.Y, Body.Z), Body.MaxSpeed);
	}
}