using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
	"&emote", new string[] {"&em", "&e"},
	EPrivLevel.Player,
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
			client.Out.SendMessage("You can't emote while dead!", EChatType.CT_Emote, EChatLoc.CL_SystemWindow);
			return;
		}

		if (args.Length < 2)
		{
			client.Out.SendMessage("You need something to emote.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		if (client.Player.IsMuted)
		{
			client.Player.Out.SendMessage("You have been muted and cannot emote!", EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
			return;
		}

		string ownRealm = string.Join(" ", args, 1, args.Length - 1);
		ownRealm = "<" + client.Player.Name + " " + ownRealm + " >";

		string diffRealm = "<" + client.Player.Name + " makes strange motions.>";

		foreach (GamePlayer player in client.Player.GetPlayersInRadius(WorldMgr.SAY_DISTANCE))
		{
			if (GameServer.ServerRules.IsAllowedToUnderstand(client.Player, player))
			{
				player.Out.SendMessage(ownRealm, EChatType.CT_Emote, EChatLoc.CL_ChatWindow);
			}
			else
			{
                if (!player.IsIgnoring(client.Player as GameLiving))
				player.Out.SendMessage(diffRealm, EChatType.CT_Emote, EChatLoc.CL_ChatWindow);
			}
		}
	}
}