using System.Linq;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
	"&disband",
	EPrivLevel.Player,
	"Disband from a group", "/disband")]
public class DisbandCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (client.Player.Group == null)
		{
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Disband.NotInGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		if (args.Length < 2)//disband myslef
		{
			client.Player.Group.RemoveMember(client.Player);
			return;
		}
		else//disband by name
		{
			if (client.Player.Group.Leader != client.Player)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Disband.NotLeader"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			string name = args[1];

			if (name.Equals(client.Player.Name, System.StringComparison.OrdinalIgnoreCase))
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Disband.NoYourself"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			int startCount = client.Player.Group.MemberCount;

			foreach (GameLiving living in client.Player.Group.GetMembersInTheGroup().Where(gl => gl.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase)))
			{
					client.Player.Group.RemoveMember(living);
			}

			//no target found to remove
			if (client.Player.Group != null && client.Player.Group.MemberCount == startCount)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Disband.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
		}
	}
}