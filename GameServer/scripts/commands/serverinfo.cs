using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute("&serverinfo", //command to handle
        ePrivLevel.Player, //minimum privelege level
        "Shows information about the server", //command description
        "/serverinfo")] //usage
    public class ServerInfoCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            client.Out.SendMessage("Server information", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage($"Online: {ClientService.ClientCount}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            TimeSpan uptime = DateTime.Now.Subtract(GameServer.Instance.StartupTime);
            double sec = uptime.TotalSeconds;
            long min = Convert.ToInt64(sec) / 60;
            long hours = min / 60;
            long days = hours / 24;
            DisplayMessage(client, $"Uptime: {days}d {hours % 24}h {min % 60}m {sec % 60:00}s");
        }
    }
}
