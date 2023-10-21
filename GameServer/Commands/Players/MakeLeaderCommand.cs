using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command("&makeleader",
     new string[] { "&m" },
     EPrivLevel.Player,
     "Set a new group leader (can be used by current leader).",
     "/m <playerName>")]

public class MakeLeaderCommand : ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (client.Player.Group == null || client.Player.Group.MemberCount < 2)
        {
            client.Out.SendMessage("You are not part of a group.",EChatType.CT_System,EChatLoc.CL_SystemWindow);
            return;
        }

        if(client.Player.Group.Leader != client.Player)
        {
            client.Out.SendMessage("You are not the leader of your group.",EChatType.CT_System,EChatLoc.CL_SystemWindow);
            return;
        }

        GamePlayer target;

        if (args.Length < 2) // Setting by target
        {
            if (client.Player.TargetObject == null || client.Player.TargetObject == client.Player)
            {
                client.Out.SendMessage("You have not selected a valid player as your target.",EChatType.CT_System,EChatLoc.CL_SystemWindow);
                return;
            }

            if(client.Player.TargetObject is not GamePlayer)
            {
                client.Out.SendMessage("You have not selected a valid player as your target.",EChatType.CT_System,EChatLoc.CL_SystemWindow);
                return;
            }

            target = (GamePlayer) client.Player.TargetObject;

            if(client.Player.Group != target.Group)
            {
                client.Out.SendMessage("You have not selected a valid player as your target.",EChatType.CT_System,EChatLoc.CL_SystemWindow);
                return;
            }
        }
        else //Setting by name
        {
            string targetName = args[1];
            target = ClientService.GetPlayerByPartialName(targetName, out _);

            if(target==null || client.Player.Group != target.Group)
            { // Invalid target
                client.Out.SendMessage("No players in group with that name.",EChatType.CT_System,EChatLoc.CL_SystemWindow);
                return;
            }

            if(target==client.Player)
            {
                client.Out.SendMessage("You are the group leader already.",EChatType.CT_System,EChatLoc.CL_SystemWindow);
                return;
            }

        }

        client.Player.Group.MakeLeader(target);
    }
}