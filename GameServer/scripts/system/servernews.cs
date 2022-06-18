using System;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&servernews",
         new string[] { "&sn" },
        ePrivLevel.Player,
        "Shows the current Server News",
        "Usage: /servernews")]
    public class ServerNewsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            var today = DateTime.Today;

            if (client.Account.PrivLevel > 1 && args.Length > 1 && args[1] == "update")
            {
                GameServer.Instance.GetPatchNotes();
                return;
            } 
            
            client.Out.SendCustomTextWindow("Server News " + today.ToString("d"), GameServer.Instance.PatchNotes);
        }
    }
}