using System.Text;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
	"&chat",
	new string[] { "&c" },
	EPrivLevel.Player,
	"Chat group command",
	"/c <text>")]
public class ChatGroupChatCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "chat"))
			return;

		ChatGroupUtil mychatgroup = client.Player.TempProperties.GetProperty<ChatGroupUtil>(ChatGroupUtil.CHATGROUP_PROPERTY, null);
		if (mychatgroup == null)
		{
			client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}
		if (mychatgroup.Listen == true && (((bool)mychatgroup.Members[client.Player]) == false))
		{
			client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.OnlyModerator"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}
		if (args.Length < 2)
		{
			client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Usage"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		StringBuilder text = new StringBuilder(7 + 3 + client.Player.Name.Length + (args.Length - 1) * 8);
		text.Append("[Chat] ");
		text.Append(client.Player.Name);
		text.Append(": \"");
		text.Append(args[1]);
		for (int i = 2; i < args.Length; i++)
		{
			text.Append(" ");
			text.Append(args[i]);
		}
		text.Append("\"");
		string message = text.ToString();
		foreach (GamePlayer ply in mychatgroup.Members.Keys)
		{
			ply.Out.SendMessage(message, EChatType.CT_Chat, EChatLoc.CL_ChatWindow);
		}
	}
}