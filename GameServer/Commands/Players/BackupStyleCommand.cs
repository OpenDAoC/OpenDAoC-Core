using Core.GS.Enums;
using Core.GS.ServerProperties;

namespace Core.GS.Commands;

[Command("&backupstyle", EPrivLevel.Player, "Modify automatic backup style.", "/backupstyle <set|clear>")]
public class BackupStyleCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (IsSpammingCommand(client.Player, "backupstyle"))
            return;

        if (!Properties.ALLOW_AUTO_BACKUP_STYLES)
        {
            client.Out.SendMessage("This command is not enabled on this server.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
            return;
        }
        
        if (args.Length < 2)
        {
            DisplaySyntax(client);
            return;
        }

        switch (args[1])
        {
            case "set":
                client.Player.styleComponent.AwaitingBackupInput = true;
                client.Player.styleComponent.AutomaticBackupStyle = null;
                if(Properties.ALLOW_NON_ANYTIME_BACKUP_STYLES)
                    client.Out.SendMessage($"The next style you use will be set as your automatic backup style.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                else
                    client.Out.SendMessage($"The next anytime style you use will be set as your automatic backup style.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                break;
            case "clear":
                client.Player.styleComponent.AutomaticBackupStyle = null;
                client.Out.SendMessage($"You will no longer use an automatic backup style.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                break;
            default:
                DisplaySyntax(client);
                return;
        }
    }
}