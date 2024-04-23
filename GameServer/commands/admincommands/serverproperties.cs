using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&serverproperties",
        ePrivLevel.Admin,
        "AdminCommands.ServerProperties.Description",
        "AdminCommands.ServerProperties.Usage")]
    public class ServerPropertiesCommand : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (GameServer.Instance.Configuration.DBType == Database.Connection.EConnectionType.DATABASE_XML)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.ServerProperties.DataBaseXML"));
                return;
            }

            if (args.Length == 1)
            {
                DisplaySyntax(client);
                return;
            }

            if (args.Length == 2)
            {
                if (args[1].Equals("refresh", System.StringComparison.OrdinalIgnoreCase))
                {
                    ServerProperties.Properties.Refresh();
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.ServerProperties.PropertiesRefreshed"));
                    return;
                }

                if (args[1].Equals("cleanup", System.StringComparison.OrdinalIgnoreCase))
                {
                    DisplayMessage(client, "Removed properties will be shown in the server console.");
                    ServerProperties.Properties.CleanUpDatabase();
                    return;
                }
            }
        }
    }
}
