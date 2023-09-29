using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&invite",
        ePrivLevel.Player,
        "Invite a specified or targeted player to join your group", "/invite <player>")]
    public class InviteCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client.Player.Group != null && client.Player.Group.Leader != client.Player)
            {
                client.Out.SendMessage("You are not the leader of your group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (IsSpammingCommand(client.Player, "invite"))
                return;

            string targetName = string.Join(" ", args, 1, args.Length - 1);
            GamePlayer target;

            if (args.Length < 2)
            {
                // Inviting by target
                if (client.Player.TargetObject == null || client.Player.TargetObject == client.Player)
                {
                    client.Out.SendMessage("You have not selected a valid player as your target.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (client.Player.TargetObject is not GamePlayer targetPlayer)
                {
                    client.Out.SendMessage("You have not selected a valid player as your target.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                target = targetPlayer;

                if (!GameServer.ServerRules.IsAllowedToGroup(client.Player, target, false))
                    return;

                if (target.NoHelp)
                {
                    client.Out.SendMessage(target.Name + "has chosen the solo path and can't join your group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
            }
            else
            {
                // Inviting by name
                target = ClientService.GetPlayerByPartialName(targetName, out ClientService.PlayerGuessResult result);

                switch (result)
                {
                    case ClientService.PlayerGuessResult.NOT_FOUND:
                    {
                        client.Out.SendMessage("No players online with that name.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }
                    case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                    {
                        client.Out.SendMessage("More than one online player matches that name.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }
                    case ClientService.PlayerGuessResult.FOUND_EXACT:
                    case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                    {
                        if (!GameServer.ServerRules.IsAllowedToGroup(client.Player, target, true))
                        {
                            client.Out.SendMessage("No players online with that name.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (target == client.Player)
                        {
                            client.Out.SendMessage("You can't invite yourself.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (target.NoHelp)
                        {
                            client.Out.SendMessage(target.Name + "has chosen the solo path and can't join your group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        break;
                    }
                }
            }

            if (target.Group != null)
            {
                client.Out.SendMessage("The player is still in a group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (client.Account.PrivLevel > target.Client.Account.PrivLevel)
            {
                // you have no choice!
                if (client.Player.Group == null)
                {
                    Group group = new(client.Player);
                    GroupMgr.AddGroup(group);
                    group.AddMember(client.Player);
                    group.AddMember(target);
                }
                else if (target.NoHelp)
                {
                    client.Out.SendMessage("Grouping this player would void their SOLO challenge", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return;
                }
                else
                    client.Player.Group.AddMember(target);

                client.Out.SendMessage($"(GM) You have added {target.Name} to your group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                target.Out.SendMessage($"GM {client.Player.Name} has added you to {client.Player.GetPronoun(1, false)} group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
            {
                client.Out.SendMessage($"You have invited {target.Name} to join your group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                target.Out.SendGroupInviteCommand(client.Player, $"{client.Player.Name} has invited you to join\n{client.Player.GetPronoun(1, false)} group. Do you wish to join?");
                target.Out.SendMessage($"{client.Player.Name} has invited you to join {client.Player.GetPronoun(1, false)} group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}
