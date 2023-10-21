using System;
using System.Collections.Generic;
using Core.GS;
using Core.GS.PacketHandler;

namespace Core.AI.Brain;

public class LegendaryAfancBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public LegendaryAfancBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	private bool BringAdds = false;
	private bool CanPort = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			BringAdds = false;
			CanPort = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "AfancMinion")
							npc.Die(npc);
					}
				}
				RemoveAdds = true;
			}
		}
		if (HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			if (BringAdds == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Minions), Util.Random(15000, 35000));
				BringAdds = true;
			}
			if(CanPort == false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ThrowPlayer), Util.Random(25000, 45000));
				CanPort = true;
            }
		}
		base.Think();
	}
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
	}
	private List<GamePlayer> Port_Enemys = new List<GamePlayer>();
	private int ThrowPlayer(EcsGameTimer timer)
    {
		if (HasAggro)
        {
			foreach(GamePlayer player in Body.GetPlayersInRadius(2500))
            {
				if(player != null)
                {
					if (player.IsAlive && player.Client.Account.PrivLevel == 1 && !Port_Enemys.Contains(player))
						Port_Enemys.Add(player);
                }
            }
			if(Port_Enemys.Count > 0)
            {
				GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
				if (Target != null && Target.IsAlive)
                {					
					Target.MoveTo(Body.CurrentRegionID, 451486, 393503, 2754, 2390);
					if(Target.PlayerClass.ID != (int)EPlayerClass.Necromancer)
                    {
						Target.TakeDamage(Target, EDamageType.Falling, Target.MaxHealth / 5, 0);
						Target.Out.SendMessage("You take falling damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
					}
                }
				CanPort = false;
			}
        }
		return 0;
    }
	public int Minions(EcsGameTimer timer)
	{
		if (HasAggro)
		{
			for (int i = 0; i < Util.Random(2, 5); i++)
			{
				GameNpc add = new GameNpc();
				add.Name = Body.Name+"'s minion";
				add.Model = 607;
				add.Level = (byte)(Util.Random(38, 45));
				add.Size = (byte)(Util.Random(8, 12));
				add.X = Body.X + Util.Random(-100, 100);
				add.Y = Body.Y + Util.Random(-100, 100);
				add.Z = Body.Z;
				add.CurrentRegion = Body.CurrentRegion;
				add.Heading = Body.Heading;
				add.RespawnInterval = -1;
				add.MaxSpeedBase = 200;
				add.PackageID = "AfancMinion";
				add.Faction = FactionMgr.GetFactionByID(18);
				add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(18));
				StandardMobBrain brain = new StandardMobBrain();
				add.SetOwnBrain(brain);
				brain.AggroRange = 1000;
				brain.AggroLevel = 100;
				add.AddToWorld();
			}
		}
		BringAdds = false;
		return 0;
	}
}