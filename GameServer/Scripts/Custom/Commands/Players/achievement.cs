using DOL.GS.Commands;

namespace DOL.GS.Scripts
{
    [Command(
        "&achievements",
        ePrivLevel.Player,
        "View your progress towards various achievements", "/achievements list")]
    public class AchievementCommandHandler : ACommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            
            if (IsSpammingCommand(client.Player, "Achievement"))
            {
                return;
            }

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }
            
            if (args[1] == "list")
            {
                client.Out.SendCustomTextWindow("Achievements", AchievementUtils.GetAchievementInfoForPlayer(client.Player));
            }
            else
            {
                DisplaySyntax(client);
            }
        }
    }
}