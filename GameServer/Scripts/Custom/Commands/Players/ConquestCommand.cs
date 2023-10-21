using Core.GS.Commands;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.Scripts
{
    [Command(
       "&conquest",
       EPrivLevel.Player,
         "Displays the current conqust status.", "/conquest")]
    public class ConquestCommand : ACommandHandler, ICommandHandler
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
