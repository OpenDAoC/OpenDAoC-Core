using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[Command(
		"&xp",
		EPrivLevel.Player,
		"toggle receiving experience points",
		"/xp <on/off>")]
	public class XpCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			if (ServerProperties.ServerProperties.EVENT_DISABLE_XP && client.Account.PrivLevel == 1)
			{
				client.Out.SendMessage("This command is disabled during the event.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				return;
			}

			if (IsSpammingCommand(client.Player, "xp"))
				return;

			if (args[1].ToLower().Equals("on"))
			{
				client.Player.GainXP = true;
				client.Out.SendMessage("Your xp flag is ON. You will gain experience points. Use '/xp off' to stop gaining experience points.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			}
			else if (args[1].ToLower().Equals("off"))
			{
				client.Player.GainXP = false;
				client.Out.SendMessage("Your xp flag is OFF. You will no longer gain experience points. Use '/xp on' to start gaining experience points again.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			}
		}
	}
}