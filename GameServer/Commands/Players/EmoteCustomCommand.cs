using DOL.GS.PacketHandler;

namespace DOL.GS.Commands;

[Command(
	"&emote", new string[] {"&em", "&e"},
	ePrivLevel.Player,
	"Roleplay an action or emotion", "/emote <text>")]
public class EmoteCustomCommand : ACommandHandler, ICommandHandler
{
	/// <summary>
	/// Method to handle the command from the client
	/// </summary>
	/// <param name="client"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "emote"))
			return;

		// no emotes if dead
		if (!client.Player.IsAlive)
		{
			client.Out.SendMessage("You can't emote while dead!", eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
			return;
		}

		if (args.Length < 2)
		{
			client.Out.SendMessage("You need something to emote.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			return;
		}

		if (client.Player.IsMuted)
		{
			client.Player.Out.SendMessage("You have been muted and cannot emote!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
			return;
		}

		string ownRealm = string.Join(" ", args, 1, args.Length - 1);
		ownRealm = "<" + client.Player.Name + " " + ownRealm + " >";

		string diffRealm = "<" + client.Player.Name + " makes strange motions.>";

		foreach (GamePlayer player in client.Player.GetPlayersInRadius(WorldMgr.SAY_DISTANCE))
		{
			if (GameServer.ServerRules.IsAllowedToUnderstand(client.Player, player))
			{
				player.Out.SendMessage(ownRealm, eChatType.CT_Emote, eChatLoc.CL_ChatWindow);
			}
			else
			{
                if (!player.IsIgnoring(client.Player as GameLiving))
				player.Out.SendMessage(diffRealm, eChatType.CT_Emote, eChatLoc.CL_ChatWindow);
			}
		}
	}
}