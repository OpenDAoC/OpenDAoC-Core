using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute("&target", ePrivLevel.Player, "target a player by name", "/target <playerName>")]
    public class TargetCommandHandler : AbstractCommandHandler, ICommandHandler
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
                        client.Out.SendMessage($"You don't see {args[1]} around here!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    client.Out.SendChangeTarget(targetPlayer);
                    client.Out.SendMessage($"You target {targetPlayer.GetName(0, true)}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
                else if (client.Account.PrivLevel > 1)
                {
                    foreach (GameNPC npc in client.Player.GetNPCsInRadius(800))
                    {
                        if (npc.Name == args[1])
                        {
                            client.Out.SendChangeTarget(npc);
                            client.Out.SendMessage($"[GM] You target {npc.GetName(0, true)}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                    }
                }

                client.Out.SendMessage($"You don't see {args[1]} around here!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (client.Account.PrivLevel > 1)
                client.Out.SendMessage("/target <player/mobname>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else
                client.Out.SendMessage("/target <playername>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
