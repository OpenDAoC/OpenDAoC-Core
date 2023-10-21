using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command("&target", EPrivLevel.Player, "target a player by name", "/target <playerName>")]
public class TargetCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (IsSpammingCommand(client.Player, "target"))
            return;

        if (args.Length == 2)
        {
            GamePlayer targetPlayer = ClientService.GetPlayerByPartialName(args[1], out ClientService.PlayerGuessResult result);

            if (result is ClientService.PlayerGuessResult.FOUND_PARTIAL or ClientService.PlayerGuessResult.FOUND_EXACT)
            {
                if (!client.Player.IsWithinRadius(targetPlayer, WorldMgr.YELL_DISTANCE) || targetPlayer.IsStealthed || GameServer.ServerRules.IsAllowedToAttack(client.Player, targetPlayer, true))
                {
                    client.Out.SendMessage($"You don't see {args[1]} around here!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    return;
                }

                client.Out.SendChangeTarget(targetPlayer);
                client.Out.SendMessage($"You target {targetPlayer.GetName(0, true)}.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }
            else if (client.Account.PrivLevel > 1)
            {
                foreach (GameNpc npc in client.Player.GetNPCsInRadius(800))
                {
                    if (npc.Name == args[1])
                    {
                        client.Out.SendChangeTarget(npc);
                        client.Out.SendMessage($"[GM] You target {npc.GetName(0, true)}.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        return;
                    }
                }
            }

            client.Out.SendMessage($"You don't see {args[1]} around here!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return;
        }

        if (client.Account.PrivLevel > 1)
            client.Out.SendMessage("/target <player/mobname>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
        else
            client.Out.SendMessage("/target <playername>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
    }
}