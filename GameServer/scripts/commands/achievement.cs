using System;
using System.Linq;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;


namespace DOL.GS.Scripts
{
    [CmdAttribute(
        "&achievements",
        ePrivLevel.Player,
        "View your progress towards various achievements", "/achievements list")]
    public class AchievementCommandHandler : AbstractCommandHandler, ICommandHandler
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