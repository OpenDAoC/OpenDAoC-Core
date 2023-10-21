using DOL.GS.Commands;

namespace DOL.GS.Scripts
{
    [Command(
       "&realmtask",
       EPrivLevel.Player,
         "Displays the current realm bonuses status.", "/realmtask")]
    public class RealmTaskCommand : ACommandHandler, ICommandHandler
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
