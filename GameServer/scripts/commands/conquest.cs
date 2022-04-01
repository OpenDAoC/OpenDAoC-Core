using DOL.GS.Commands;


namespace DOL.GS.Scripts
{
    [CmdAttribute(
       "&conquest",
       ePrivLevel.Player,
         "Displays the current conqust status.", "/conquest")]
    public class ConquestCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (!IsSpammingCommand(client.Player, "task"))
            {
                client.Out.SendCustomTextWindow("Conquest Information", ConquestService.ConquestManager.GetTextList());
            }
        }
    }
}
