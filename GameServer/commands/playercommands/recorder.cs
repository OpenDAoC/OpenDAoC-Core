using DOL.GS.PacketHandler;
using DOL.GS.Commands;
using System.Linq;

namespace DOL.GS
{
    [CmdAttribute("&recorder", ePrivLevel.Player, "Recorder Steuerung", "/recorder start", "/recorder stop <name>")]
    public class RecorderCommandHandler : ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client.Player == null || args.Length < 2) 
            {
                client.Player?.Out.SendMessage("Benutzung: /recorder start ODER /recorder stop <name>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            string action = args[1].ToLower();

            if (action == "start") 
            {
                RecorderMgr.StartRecording(client.Player);
            }
            // Wir unterstützen 'stop' und 'save', falls du dich umgewöhnen musst
            else if ((action == "stop" || action == "save") && args.Length >= 3) 
            {
                // Das Icon wird jetzt automatisch im RecorderMgr ermittelt
                RecorderMgr.StopAndSaveRecording(client.Player, args[2]);
            }
            else 
            {
                client.Player.Out.SendMessage("Benutzung: /recorder start ODER /recorder stop <name>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}