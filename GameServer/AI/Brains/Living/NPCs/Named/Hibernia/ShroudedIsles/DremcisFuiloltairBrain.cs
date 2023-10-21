namespace Core.GS.AI.Brains;

#region Dremcis Fuiloltair
public class DremcisFuiloltairBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public DremcisFuiloltairBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}

	public static bool CanSpawnStag = false;
	private bool CanSpawnBlobs = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			CanSpawnBlobs = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is FuilslathachBrain)
						npc.RemoveFromWorld();
				}
				RemoveAdds = true;
			}
		}
		if(HasAggro && Body.TargetObject != null)
        {
			RemoveAdds = false;
			if (!CanSpawnBlobs)
			{
				SpawnBlobs();
				CanSpawnBlobs = true;
			}
			if (!CanSpawnStag)
			{
				SpawnStag();
				CanSpawnStag = true;
			}
			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is FuilslathachBrain brain)
				{
					if (!brain.HasAggro && target.IsAlive && target != null)
						brain.AddToAggroList(target, 10);
				}
			}
			foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is BeomarbhanBrain brain)
				{
					if (!brain.HasAggro && target.IsAlive && target != null)
						brain.AddToAggroList(target, 10);
				}
			}
		}
		base.Think();
	}
	private void SpawnStag()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
		{
			if (npc.Brain is BeomarbhanBrain)
				return;
		}
		Beomarbhan stag = new Beomarbhan();
		stag.X = Body.X + Util.Random(-200, 200);
		stag.Y = Body.Y + Util.Random(-200, 200);
		stag.Z = Body.Z;
		stag.Heading = Body.Heading;
		stag.CurrentRegion = Body.CurrentRegion;
		stag.AddToWorld();
	}
	private void SpawnBlobs()
    {
		foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
		{
			if (npc.Brain is FuilslathachBrain)
				return;
		}
		for (int i = 0; i < Util.Random(4,6); i++)
		{
			Fuilslathach blobs = new Fuilslathach();
			blobs.X = Body.X + Util.Random(-200, 200);
			blobs.Y = Body.Y + Util.Random(-200, 200);
			blobs.Z = Body.Z;
			blobs.Heading = Body.Heading;
			blobs.CurrentRegion = Body.CurrentRegion;
			blobs.AddToWorld();
		}
	}
}
#endregion Dremcis Fuiloltair

#region Stag pet
public class BeomarbhanBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public BeomarbhanBrain() : base()
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
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
		}
		base.Think();
	}
}
#endregion Stag pet

#region Dremcis adds
public class FuilslathachBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public FuilslathachBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Dremcis adds