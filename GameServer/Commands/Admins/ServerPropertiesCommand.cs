using Core.Database.Enums;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Server;

namespace Core.GS.Commands;

[Command(
	"&serverproperties",
	EPrivLevel.Admin,
	"AdminCommands.ServerProperties.Description",
	"AdminCommands.ServerProperties.Usage")]
public class ServerPropertiesCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (GameServer.Instance.Configuration.DBType == EConnectionType.DATABASE_XML)
		{
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.ServerProperties.DataBaseXML"));
			return;
		}
		ServerProperty.Refresh();
		DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.ServerProperties.PropertiesRefreshed"));
	}
}