namespace Core.GS.AI.Brains;

public class MouthBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MouthBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	private bool SpawnAdd = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			SpawnAdd = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "MouthAdd")
						npc.Die(Body);
				}
				RemoveAdds = true;
			}
		}
		if (HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			if (SpawnAdd==false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(MouthAdds), Util.Random(20000, 35000));
				SpawnAdd = true;
            }
		}
		base.Think();
	}
	private int MouthAdds(EcsGameTimer timer)
    {
		if (HasAggro && Body.IsAlive)
		{
			GameNpc add = new GameNpc();
			add.Name = Body.Name + "'s minion";
			add.Model = 584;
			add.Size = (byte)Util.Random(45, 55);
			add.Level = (byte)Util.Random(55, 59);
			add.Strength = 150;
			add.Quickness = 80;
			add.MeleeDamageType = EDamageType.Thrust;
			add.MaxSpeedBase = 225;
			add.PackageID = "MouthAdd";
			add.RespawnInterval = -1;
			add.X = Body.X + Util.Random(-100, 100);
			add.Y = Body.Y + Util.Random(-100, 100);
			add.Z = Body.Z;
			add.CurrentRegion = Body.CurrentRegion;
			add.Heading = Body.Heading;
			add.Faction = FactionMgr.GetFactionByID(18);
			add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(18));
			StandardMobBrain brain = new StandardMobBrain();
			add.SetOwnBrain(brain);
			brain.AggroRange = 600;
			brain.AggroLevel = 100;
			add.AddToWorld();
		}
		SpawnAdd = false;
		return 0;
    }
}