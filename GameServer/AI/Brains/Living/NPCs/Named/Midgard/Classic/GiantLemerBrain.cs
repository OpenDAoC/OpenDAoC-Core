using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

public class GiantLemerBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public GiantLemerBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 300;
		ThinkInterval = 1000;
	}

	ushort oldModel;
	ENpcFlags oldFlags;
	bool changed;
	bool spawnRats = false;
	private bool RemoveAdds = false;

	public void BroadcastMessage(string message)
	{
		foreach (GamePlayer player in ClientService.GetPlayersOfZone(Body.CurrentZone))
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
	}

	public override void Think()
	{
		if (Body.CurrentRegion.IsNightTime == false)
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
		if (Body.CurrentRegion.IsNightTime)
		{
			if (changed)
			{
				Body.Flags = oldFlags;
				Body.Model = oldModel;
				BroadcastMessage("A great growl goes through the woods.");
				changed = false;
			}

		}
		if (!CheckProximityAggro())
		{
			spawnRats = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is GiantLemerAddBrain)
						npc.RemoveFromWorld();
				}
				RemoveAdds = true;
			}
		}

		if(HasAggro && Body.TargetObject != null)
        {
			RemoveAdds = false;
			if(!spawnRats)
            {
				SpawnRats();
				spawnRats = true;
            }
			foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
			{
				if (npc != null && npc.IsAlive && npc.Brain is GiantLemerAddBrain brain)
                {
					GameLiving target = Body.TargetObject as GameLiving;
					if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
						brain.AddToAggroList(target, 10);
                }
			}
		}
		base.Think();
	}
	private void SpawnRats()
	{
		for (int i = 0; i < Util.Random(2,4); i++)
		{
			GiantLemerAdd npc = new GiantLemerAdd();
			npc.X = Body.X + Util.Random(-100, 100);
			npc.Y = Body.Y + Util.Random(-100, 100);
			npc.Z = Body.Z;
			npc.Heading = Body.Heading;
			npc.CurrentRegion = Body.CurrentRegion;
			npc.AddToWorld();
		}
	}
}

public class GiantLemerAddBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public GiantLemerAddBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}