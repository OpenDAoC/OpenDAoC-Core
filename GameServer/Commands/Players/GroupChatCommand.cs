using DOL.GS.PacketHandler;

namespace DOL.GS.Commands;

[Command(
	"&group",
	new string[] {"&g"},
	EPrivLevel.Player,
	"Say something to other chat group players",
	"/g <message>")]
public class GroupChatCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (client.Player.Group == null)
		{
			DisplayMessage(client, "You are not part of a group");
			return;
		}

		if (IsSpammingCommand(client.Player, "group", 500))
		{
			DisplayMessage(client, "Slow down! Think before you say each word!");
			return;
		}

		if (args.Length >= 2)
		{
			string msg = "";
			for (int i = 1; i < args.Length; ++i)
			{
				msg += args[i] + " ";
			}

			client.Player.Group.SendMessageToGroupMembers(client.Player, msg, EChatType.CT_Group, EChatLoc.CL_ChatWindow);
		}
		else
		{
			DisplaySyntax(client);
		}
	}
}