namespace DOL.GS.Commands
{
	[CmdAttribute("&groundassist", //command to handle
		 ePrivLevel.Player, //minimum privelege level
		 "Show the current coordinates", //command description
		 "/groundassist")] //command usage
	public class GroundAssistCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "groundassist"))
				return;
			DisplayMessage(client, "/groundassist is disabled on this server.");
			
			// GameLiving target = client.Player.TargetObject as GameLiving;
			// if (args.Length > 1)
			// {
			// 	GameClient myclient;
			// 	myclient = WorldMgr.GetClientByPlayerName(args[1], true, true);
			// 	if (myclient == null)
			// 	{
			// 		client.Player.Out.SendMessage("No player with this name in game.", eChatType.CT_Say, eChatLoc.CL_SystemWindow);
			// 		return;
			// 	}
			// 	target = myclient.Player;
			// }
			//
			// if (target == client.Player)
			// {
			// 	client.Out.SendMessage("You can't groundassist yourself.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			// 	return;
			// }
			//
			// if (target == null)
			// 	return;
			//
			// // can't assist an enemy
			// if (GameServer.ServerRules.IsAllowedToAttack(client.Player, target as GameLiving, true))
			// 	return;
			//
			// if (!client.Player.IsWithinRadius( target, 2048 ))
			// {
			// 	client.Out.SendMessage("You don't see " + args[1] + " around here!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			// 	return;
			// }
			//
			// if (!target.GroundTarget.IsValid)
			// {
			// 	client.Out.SendMessage(target.Name + " doesn't currently have a ground target.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			// 	return;
			// }
			// client.Player.Out.SendChangeGroundTarget(target.GroundTarget);
			// client.Player.SetGroundTarget(target.GroundTarget.X, target.GroundTarget.Y, target.GroundTarget.Z);
		}
	}
}
