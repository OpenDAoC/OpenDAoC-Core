using DOL.GS.Commands;


namespace DOL.GS.Scripts
{
    [CmdAttribute(
       "&realmtask",
       ePrivLevel.Player,
         "Displays the current realm bonuses status.", "/realmtask")]
    public class TaskCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (!IsSpammingCommand(client.Player, "task"))
            {
                client.Out.SendCustomTextWindow("Task Bonuses", ZoneBonusRotator.GetTextList());
            }
        }
    }
}
