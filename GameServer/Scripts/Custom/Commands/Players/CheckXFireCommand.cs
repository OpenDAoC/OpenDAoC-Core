using Core.GS.Commands;
using Core.GS.Enums;

namespace Core.GS.Scripts.Custom;

[Command("&xfire", EPrivLevel.Player, "Xfire support", "/xfire <on|off>")]
public class CheckXFireCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (client.Player == null)
            return;
        if (args.Length < 2)
        {
            DisplaySyntax(client);
            return;
        }
        byte flag = 0;
        if (args[1].ToLower().Equals("on"))
        {
            client.Player.ShowXFireInfo = true;
            DisplayMessage(client, "Your XFire flag is ON. Your character data will be sent to the XFire service ( if you have XFire installed ). Use '/xfire off' to disable sending character data to the XFire service.");
            flag = 1;
        }
        else if (args[1].ToLower().Equals("off"))
        {
            client.Player.ShowXFireInfo = false;
            DisplayMessage(client, "Your XFire flag is OFF. TODO correct message.");
        }
        else
        {
            DisplaySyntax(client);
            return;
        }
        client.Player.Out.SendXFireInfo(flag);
    }
}