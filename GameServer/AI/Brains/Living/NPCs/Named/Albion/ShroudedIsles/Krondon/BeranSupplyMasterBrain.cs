using System;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain;

#region Beran Supply Master
public class BeranSupplyMasterBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public BeranSupplyMasterBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool Ignite_Barrel = false;
	public static bool BringAdds = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			Ignite_Barrel = false;
			BringAdds = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Name.ToLower() == "onstal hyrde" && npc.RespawnInterval == -1)
							npc.Die(npc);
					}
				}
				RemoveAdds = true;
			}
		}
		if (HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			if(Ignite_Barrel == false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(IgniteBarrel), Util.Random(15000, 35000));
				Ignite_Barrel = true;
            }
			if (BringAdds == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CallForHelp), Util.Random(35000, 65000));
				BringAdds = true;
			}
		}
		base.Think();
	}
	public int CallForHelp(EcsGameTimer timer)
    {
		if (HasAggro)
		{
			for (int i = 0; i < Util.Random(2, 4); i++)
			{
				GameNpc add = new GameNpc();
				add.Name = "Onstal Hyrde";
				add.Model = 919;
				add.Level = (byte)(Util.Random(64, 68));
				add.Size = (byte)(Util.Random(100, 115));
				add.X = Body.X + Util.Random(-100, 100);
				add.Y = Body.Y + Util.Random(-100, 100);
				add.Z = Body.Z;
				add.CurrentRegion = Body.CurrentRegion;
				add.Heading = Body.Heading;
				add.RespawnInterval = -1;
				add.MaxSpeedBase = 225;
				add.Faction = FactionMgr.GetFactionByID(8);
				add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));
				StandardMobBrain brain = new StandardMobBrain();
				add.SetOwnBrain(brain);
				brain.AggroRange = 1000;
				brain.AggroLevel = 100;
				add.AddToWorld();
			}
			BroadcastMessage(String.Format(Body.Name + " yells for help."));
		}
		BringAdds = false;
		return 0;
    }
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
	}
	public int IgniteBarrel(EcsGameTimer timer)
    {
		if (HasAggro)
		{
			BarrelExplosive npc = new BarrelExplosive();
			switch (Util.Random(1, 5))
			{
				case 1:
					npc.X = 49409;
					npc.Y = 26596;
					npc.Z = 17161;
					break;
				case 2:
					npc.X = 48547;
					npc.Y = 26031;
					npc.Z = 17156;
					break;
				case 3:
					npc.X = 48508;
					npc.Y = 26695;
					npc.Z = 17159;
					break;
				case 4:
					npc.X = 48484;
					npc.Y = 27274;
					npc.Z = 17159;
					break;
				case 5:
					npc.X = 49226;
					npc.Y = 27325;
					npc.Z = 17247;
					break;
			}
			npc.RespawnInterval = -1;
			npc.Heading = Body.Heading;
			npc.CurrentRegion = Body.CurrentRegion;
			npc.AddToWorld();
			BroadcastMessage(String.Format(Body.Name + " ignites barrel."));
			Ignite_Barrel = false;
			Body.TurnTo(npc);
			Body.Emote(EEmote.LetsGo);
		}
		return 0;
	}
}
#endregion Beran Supply Master

#region Barrel Explosion Mob
public class BarrelExplosiveBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public BarrelExplosiveBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 2500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Barrel Explosion Mob