﻿using DOL.GS.Commands;


namespace DOL.GS.Scripts
{
    [Command(
       "&conquest",
       EPrivLevel.Player,
         "Displays the current conqust status.", "/conquest")]
    public class ConquestCommand : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (!IsSpammingCommand(client.Player, "task"))
            {
                client.Out.SendCustomTextWindow("Conquest Information", ConquestService.ConquestManager.GetTextList(client.Player));
            }
        }
    }
}