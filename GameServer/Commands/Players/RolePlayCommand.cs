using DOL.GS.PacketHandler;

namespace DOL.GS.Commands;

[Command(
	"&roleplay",
	ePrivLevel.Player,
   "Flags a player with an  tag to indicate the player is a role player.",
   "/roleplay on/off")]
public class RolePlayCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "roleplay"))
			return;

		if (args.Length < 2)
		{
			DisplaySyntax(client);
			return;
		}

		if (args[1].ToLower().Equals("on"))
		{
			client.Player.RPFlag = true;
			client.Out.SendMessage("Your roleplay flag is ON. You will now be flagged as a roleplayer. Use '/roleplay off' to turn the flag off.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
		else if (args[1].ToLower().Equals("off"))
		{
			client.Player.RPFlag = false;
			client.Out.SendMessage("Your roleplay flag is OFF. You will be flagged as a roleplayer. Use '/roleplay on' to turn the flag back on.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
	}
}