using DOL.GS;
using DOL.GS.Movement;

namespace DOL.AI.Brain
{
	/// <summary>
	/// A brain that make npc walk on rounds with way point
	/// </summary>
	public class RoundsBrain : StandardMobBrain
	{
		/// <summary>
		/// Load the path of the mob
		/// </summary>
		/// <returns>True if is ok</returns>
		public override bool Start()
		{
			if (!base.Start()) return false;
			Body.CurrentWaypoint = MovementMgr.LoadPath(!string.IsNullOrEmpty(Body.PathID) ? Body.PathID : Body.InternalID + " Rounds");
			Body.MoveOnPath(Body.CurrentWaypoint.MaxSpeed);
			return true;
		}
		/// <summary>
		/// Add living to the aggrolist
		/// save path of player before attack to walk back to way point after fight
		/// </summary>
		public override void AddToAggroList(GameLiving living, long aggroAmount)
		{
			//save current position in path go to here and reload path point
			//insert path in pathpoint
			PathPoint temporaryPathPoint = new PathPoint(Body.X, Body.Y, Body.Z, Body.CurrentSpeed, Body.CurrentWaypoint.Type);
			temporaryPathPoint.Next = Body.CurrentWaypoint;
			temporaryPathPoint.Prev = Body.CurrentWaypoint.Prev;
			Body.CurrentWaypoint = temporaryPathPoint;
			//this path point will be not available after the following point because no link to itself
			base.AddToAggroList(living, aggroAmount);
		}

		/// <summary>
		/// Returns the best target to attack
		/// if no target go to saved pathpoint to continue the round
		/// </summary>
		/// <returns>the best target</returns>
		protected override GameLiving CalculateNextAttackTarget()
		{
			GameLiving living = base.CalculateNextAttackTarget();
			if (living == null)
				Body.MoveOnPath(Body.CurrentWaypoint.MaxSpeed);
			return living;
		}
	}
}
