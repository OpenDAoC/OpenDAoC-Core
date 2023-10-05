using DOL.GS.Commands;

namespace DOL.GS.Scripts
{
    [Command(
       "&realmtask",
       ePrivLevel.Player,
         "Displays the current realm bonuses status.", "/realmtask")]
    public class TaskCommandHandler : ACommandHandler, ICommandHandler
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
