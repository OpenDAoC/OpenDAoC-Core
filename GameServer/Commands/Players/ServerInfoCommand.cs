using System;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command("&serverinfo", //command to handle
    EPrivLevel.Player, //minimum privelege level
    "Shows information about the server", //command description
    "/serverinfo")] //usage
public class ServerInfoCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        client.Out.SendMessage("Server information", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
        client.Out.SendMessage($"Online: {ClientService.ClientCount}", EChatType.CT_System, EChatLoc.CL_SystemWindow);
        TimeSpan uptime = DateTime.Now.Subtract(GameServer.Instance.StartupTime);
        double sec = uptime.TotalSeconds;
        long min = Convert.ToInt64(sec) / 60;
        long hours = min / 60;
        long days = hours / 24;
        DisplayMessage(client, $"Uptime: {days}d {hours % 24}h {min % 60}m {sec % 60:00}s");
    }
}