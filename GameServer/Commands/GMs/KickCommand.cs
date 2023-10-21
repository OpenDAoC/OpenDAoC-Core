using System;
using Core.GS.ECS;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Commands;

[Command(
	"&kick",
	new string[] { "&k" },
	EPrivLevel.GM,
	"GMCommands.Kick.Description",
	"GMCommands.Kick.Usage",
	"/kick <#ClientID>","/kick account <account>"),]
public class KickCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 2)
		{
			DisplaySyntax(client);
			return;
		}

		GameClient clientc = null;

		if (args.Length == 3)
		{
			if (args[1].Equals("account"))
			{
				try
				{
					clientc = ClientService.GetClientFromAccountName(args[2]);
				}
				catch
				{
					DisplayMessage(client, "Invalid account name");
				}
			}
		}
		else if (args[1].StartsWith("#"))
		{
			try
			{
				int sessionID = Convert.ToInt32(args[1][1..]);
				clientc = ClientService.GetClientFromId(sessionID);
			}
			catch
			{
				DisplayMessage(client, "Invalid client ID");
			}
		}
		else
		{
			clientc = ClientService.GetPlayerByExactName(args[1])?.Client;
		}

		if (clientc == null)
		{
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Kick.NoPlayerOnLine"));
			return;
		}

		if (client.Account.PrivLevel < clientc.Account.PrivLevel)
		{
			DisplayMessage(client, "Your privlevel is not high enough to kick this player.");
			return;
		}

		for (int i = 0; i < 8; i++)
		{
			string message;
			if (client != null && client.Player != null)
			{
				message = LanguageMgr.GetTranslation(clientc, "GMCommands.Kick.RemovedFromServerByGM", client.Player.Name);
			}
			else
			{
				message = LanguageMgr.GetTranslation(clientc, "GMCommands.Kick.RemovedFromServer");
			}

			clientc.Out.SendMessage(message, EChatType.CT_Help, EChatLoc.CL_SystemWindow);
			clientc.Out.SendMessage(message, EChatType.CT_Help, EChatLoc.CL_ChatWindow);
		}

		clientc.Out.SendPlayerQuit(true);
		clientc.Player.SaveIntoDatabase();
		clientc.Player.Quit(true);
	}
}