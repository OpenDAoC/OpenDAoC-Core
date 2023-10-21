using Core.Database.Connection;
using Core.Language;

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
		ServerProperties.Properties.Refresh();
		DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.ServerProperties.PropertiesRefreshed"));
	}
}