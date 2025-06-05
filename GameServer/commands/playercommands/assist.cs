using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute("&assist", ePrivLevel.Player, "Assist your target", "/assist [playerName]")]
    public class AssistCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            // if (IsSpammingCommand(client.Player, "assist"))
            //     return;

            if (args.Length > 1)
            {
                if (args[1].Equals(client.Player.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    // We cannot assist our target when it has no target.
                    if (!HasTarget(client, client.Player))
                        return;

                    YouAssist(client, client.Player.Name, client.Player.TargetObject);
                    return;
                }

                GamePlayer assistPlayer = null;

                foreach (GamePlayer plr in client.Player.GetPlayersInRadius(2048))
                {
                    if (!plr.Name.Equals(args[1], System.StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    assistPlayer = plr;
                    break;
                }

                if (assistPlayer != null)
                {
                    // Each server type handles the assist command on it's own way.
                    switch(GameServer.Instance.Configuration.ServerType)
                    {
                        case EGameServerType.GST_Normal:
                        {
                            // We cannot assist players of an enemy realm.
                            if (!SameRealm(client, assistPlayer, false))
                                return;

                            // We cannot assist our target when it has no target.
                            if (!HasTarget(client, assistPlayer))
                                return;

                            YouAssist(client, assistPlayer.Name, assistPlayer.TargetObject);
                            return;
                        }
                        case EGameServerType.GST_PvE:
                        {
                            // We cannot assist our target when it has no target.
                            if (!HasTarget(client, assistPlayer))
                                return;

                            YouAssist(client, assistPlayer.Name, assistPlayer.TargetObject);
                            return;
                        }
                        case EGameServerType.GST_PvP:
                        {
                            // Lets check if the client and it's targeted player are in the same alliance.
                            if (client.Player.Guild != null)
                            {
                                if (client.Player.Guild.alliance != null &&
                                    client.Player.Guild.alliance.Contains(assistPlayer.Guild))
                                {
                                    //We cannot assist our target when it has no target.
                                    if (!HasTarget(client, assistPlayer))
                                        return;

                                    YouAssist(client, assistPlayer.Name, assistPlayer.TargetObject);
                                    return;
                                }

                                // They are no alliance members, maybe guild members?
                                if (client.Player.Guild.GetOnlineMemberByID(assistPlayer.InternalID) != null)
                                {
                                    // We cannot assist our target when it has no target.
                                    if (!HasTarget(client, assistPlayer))
                                        return;

                                    YouAssist(client, assistPlayer.Name, assistPlayer.TargetObject);
                                    return;
                                }
                            }

                            // They are no alliance or guild members - maybe group members?
                            if (client.Player.Group != null && client.Player.Group.IsInTheGroup(assistPlayer))
                            {
                                // We cannot assist our target when it has no target.
                                if (!HasTarget(client, assistPlayer))
                                    return;

                                YouAssist(client, assistPlayer.Name, assistPlayer.TargetObject);
                                return;
                            }

                            // Ok, they are not in the same alliance, guild or group - maybe in the same battle group?
                            BattleGroup clientBattleGroup = client.Player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
                            if (clientBattleGroup != null)
                            {
                                if (clientBattleGroup.Members.Contains(assistPlayer))
                                {
                                    // We cannot assist our target when it has no target.
                                    if (!HasTarget(client, assistPlayer))
                                        return;

                                    YouAssist(client, assistPlayer.Name, assistPlayer.TargetObject);
                                    return;
                                }
                            }

                            // Ok, they are not in the same alliance, guild, group or battle group - maybe in the same chat group?
                            ChatGroup clientChatGroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY);
                            if (clientChatGroup != null)
                            {
                                if (clientChatGroup.Members.Contains(assistPlayer))
                                {
                                    // We cannot assist our target when it has no target.
                                    if (!HasTarget(client, assistPlayer))
                                        return;

                                    YouAssist(client, assistPlayer.Name, assistPlayer.TargetObject);
                                    return;
                                }
                            }

                            // They are not in the same alliance, guild, group, battle group or chat group. And now? Well, they are enemies!
                            NoValidTarget(client, assistPlayer);
                            return;
                        }
                    }
                }

                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Assist.MemberNotFound"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (client.Player.TargetObject != null)
            {
                if (client.Player.TargetObject == client.Player)
                {
                    YouAssist(client, client.Player.Name, client.Player.TargetObject);
                    return;
                }

                if (client.Player.TargetObject is GameNPC or GamePlayer)
                {
                    if (client.Player.TargetObject is GameMovingObject)
                    {
                        NoValidTarget(client, client.Player.TargetObject as GameLiving);
                        return;
                    }

                    // Each server type handles the assist command on it's own way.
                    switch(GameServer.Instance.Configuration.ServerType)
                    {
                        case EGameServerType.GST_Normal:
                        {
                            GameLiving targetLiving = (GameLiving) client.Player.TargetObject;

                            //We cannot assist npc's or players of an enemy realm.
                            if (!SameRealm(client, targetLiving, false))
                                return;

                            //We cannot assist our target when it has no target.
                            if (!HasTarget(client, targetLiving))
                                return;

                            YouAssist(client, client.Player.TargetObject.GetName(0, true), targetLiving.TargetObject);
                            return;
                        }
                        case EGameServerType.GST_PvE:
                        {
                            if (client.Player.TargetObject is GamePlayer)
                            {
                                // We cannot assist our target when it has no target.
                                if (!HasTarget(client, client.Player.TargetObject as GameLiving))
                                    return;

                                YouAssist(client, client.Player.TargetObject.Name, (client.Player.TargetObject as GameLiving).TargetObject);
                                return;
                            }
                            else if (client.Player.TargetObject is GameNPC)
                            {
                                if (!SameRealm(client, client.Player.TargetObject as GameNPC, true))
                                    return;
                                else
                                {
                                    // We cannot assist our target when it has no target.
                                    if (!HasTarget(client, client.Player.TargetObject as GameNPC))
                                        return;

                                    YouAssist(client, client.Player.TargetObject.GetName(0, true), (client.Player.TargetObject as GameLiving).TargetObject);
                                    return;
                                }
                            }

                            break;
                        }
                        case EGameServerType.GST_PvP:
                        {
                            if(client.Player.TargetObject is GamePlayer)
                            {
                                GamePlayer targetPlayer = client.Player.TargetObject as GamePlayer;

                                // Lets check if the client and it's targeted player are in the same alliance.
                                if (client.Player.Guild != null && client.Player.Guild.alliance != null && client.Player.Guild.alliance.Contains(targetPlayer.Guild))
                                {
                                    // We cannot assist our target when it has no target.
                                    if (!HasTarget(client, targetPlayer))
                                        return;

                                    YouAssist(client, targetPlayer.Name, targetPlayer.TargetObject);
                                    return;
                                }

                                // They are no alliance members, maybe guild members?
                                if (client.Player.Guild != null && client.Player.Guild.GetOnlineMemberByID(targetPlayer.InternalID) != null)
                                {
                                    // We cannot assist our target when it has no target.
                                    if (!HasTarget(client, targetPlayer))
                                        return;

                                    YouAssist(client, targetPlayer.Name, targetPlayer.TargetObject);
                                    return;
                                }

                                // They are no alliance or guild members - maybe group members?
                                if (client.Player.Group != null && client.Player.Group.IsInTheGroup(targetPlayer))
                                {
                                    // We cannot assist our target when it has no target.
                                    if (!HasTarget(client, targetPlayer))
                                        return;

                                    YouAssist(client, targetPlayer.Name, targetPlayer.TargetObject);
                                    return;
                                }

                                // Ok, they are not in the same alliance, guild or group - maybe in the same battle group?
                                BattleGroup clientBattleGroup = client.Player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
                                if (clientBattleGroup != null)
                                {
                                    if (clientBattleGroup.Members.Contains(targetPlayer))
                                    {
                                        // We cannot assist our target when it has no target.
                                        if (!HasTarget(client, targetPlayer))
                                            return;

                                        YouAssist(client, targetPlayer.Name, targetPlayer.TargetObject);
                                        return;
                                    }
                                }

                                // Ok, they are not in the same alliance, guild, group or battle group - maybe in the same chat group?
                                ChatGroup clientChatGroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY);
                                if (clientChatGroup != null)
                                {
                                    if (clientChatGroup.Members.Contains(targetPlayer))
                                    {
                                        //We cannot assist our target when it has no target.
                                        if (!HasTarget(client, targetPlayer))
                                            return;

                                        YouAssist(client, targetPlayer.Name, targetPlayer.TargetObject);
                                        return;
                                    }
                                }

                                //They are not in the same alliance, guild, group, battle group or chat group. And now? Well, they are enemies!
                                NoValidTarget(client, targetPlayer);
                                return;
                            }

                            if (client.Player.TargetObject is GameNPC)
                            {
                                if (client.Player.TargetObject is GameSummonedPet)
                                {
                                    GameSummonedPet targetPet = client.Player.TargetObject as GameSummonedPet;

                                    if (targetPet.Owner is GamePlayer)
                                    {
                                        GamePlayer targetPlayer = targetPet.Owner as GamePlayer;

                                        // Lets check if the client and it's targeted pets owner are in the same alliance.
                                        if (client.Player.Guild != null && client.Player.Guild.alliance != null && client.Player.Guild.alliance.Contains(targetPlayer.Guild))
                                        {
                                            // We cannot assist our target when it has no target.
                                            if (!HasTarget(client, targetPet))
                                                return;

                                            YouAssist(client, targetPet.GetName(0, false), targetPet.TargetObject);
                                            return;
                                        }

                                        // They are no alliance members, maybe guild members?
                                        if (client.Player.Guild != null && client.Player.Guild.GetOnlineMemberByID(targetPlayer.InternalID) != null)
                                        {
                                            // We cannot assist our target when it has no target.
                                            if (!HasTarget(client, targetPet))
                                                return;

                                            YouAssist(client, targetPet.GetName(0, false), targetPet.TargetObject);
                                            return;
                                        }

                                        // They are no alliance or guild members - maybe group members?
                                        if (client.Player.Group != null && client.Player.Group.IsInTheGroup(targetPlayer))
                                        {
                                            // We cannot assist our target when it has no target.
                                            if (!HasTarget(client, targetPet))
                                                return;

                                            YouAssist(client, targetPet.GetName(0, false), targetPet.TargetObject);
                                            return;
                                        }

                                        // Ok, they are not in the same alliance, guild or group - maybe in the same battle group?
                                        BattleGroup clientBattleGroup = client.Player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
                                        if (clientBattleGroup != null)
                                        {
                                            if (clientBattleGroup.Members.Contains(targetPlayer))
                                            {
                                                // We cannot assist our target when it has no target.
                                                if (!HasTarget(client, targetPet))
                                                    return;

                                                YouAssist(client, targetPet.GetName(0, false), targetPet.TargetObject);
                                                return;
                                            }
                                        }

                                        // Ok, they are not in the same alliance, guild, group or battle group - maybe in the same chat group?
                                        ChatGroup clientChatGroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY);
                                        if (clientChatGroup != null)
                                        {
                                            if (clientChatGroup.Members.Contains(targetPlayer))
                                            {
                                                // We cannot assist our target when it has no target.
                                                if (!HasTarget(client, targetPet))
                                                    return;

                                                YouAssist(client, targetPet.GetName(0, false), targetPet.TargetObject);
                                                return;
                                            }
                                        }

                                        // They are not in the same alliance, guild, group, battle group or chat group. And now? Well, they are enemies!
                                        NoValidTarget(client, targetPet);
                                        return;
                                    }

                                    if (targetPet.Owner is GameNPC)
                                    {
                                        if (!SameRealm(client, targetPet.Owner as GameNPC, true))
                                            return;
                                        else
                                        {
                                            //We cannot assist our target when it has no target.
                                            if (!HasTarget(client, targetPet))
                                                return;

                                            YouAssist(client, targetPet.GetName(0, false), targetPet.TargetObject);
                                            return;
                                        }
                                    }
                                }

                                if (client.Player.TargetObject is GameKeepGuard)
                                {
                                    GameKeepGuard targetGuard = client.Player.TargetObject as GameKeepGuard;
                                    Guild targetedGuardGuild = GuildMgr.GetGuildByName(targetGuard.GuildName);

                                    // We can assist guards of an unclaimed keep!
                                    if (targetedGuardGuild == null)
                                    {
                                        //We cannot assist our target when it has no target
                                        if (!HasTarget(client, targetGuard))
                                            return;

                                        YouAssist(client, targetGuard.GetName(0, false), targetGuard.TargetObject);
                                        return;
                                    }

                                    // Is the guard of our guild?
                                    if (client.Player.Guild == targetedGuardGuild)
                                    {
                                        // We cannot assist our target when it has no target.
                                        if (!HasTarget(client, targetGuard))
                                            return;

                                        YouAssist(client, targetGuard.GetName(0, false), targetGuard.TargetObject);
                                        return;
                                    }

                                    // Is the guard of one of our alliance guilds?
                                    if (client.Player.Guild.alliance.Contains(targetedGuardGuild))
                                    {
                                        // We cannot assist our target when it has no target.
                                        if (!HasTarget(client, targetGuard))
                                            return;

                                        YouAssist(client, targetGuard.GetName(0, false), targetGuard.TargetObject);
                                        return;
                                    }

                                    // The guard is not of one of our alliance guilds and our guild. And now? Well, he is an enemy and we cannot assist enemies!
                                    NoValidTarget(client, targetGuard);
                                    return;
                                }

                                // We cannot assist npc's of an enemy realm.
                                if (!SameRealm(client, client.Player.TargetObject as GameNPC, true))
                                    return;

                                // We cannot assist our target when it has no target.
                                if (!HasTarget(client, client.Player.TargetObject as GameNPC))
                                    return;

                                YouAssist(client, (client.Player.TargetObject as GameNPC).GetName(0, false), (client.Player.TargetObject as GameNPC).TargetObject);
                                return;
                            }

                            break;
                        }
                    }
                }
            }

            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Assist.SelectMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }

        private static bool HasTarget(GameClient client, GameLiving livingToCheck)
        {
            if (livingToCheck.TargetObject != null)
                return true;

            // We cannot assist our target when it has no target.
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Assist.DoesntHaveTarget", livingToCheck.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }

        private static void NoValidTarget(GameClient client, GameLiving livingToAssist)
        {
            // Original live text: {0} is not a member of your realm!
            // The original text sounds stupid if we use it for rams or other things: The battle ram is not a member of your realm!
            // But the text is also used for rams that are a member of our realm, so we don't use it.
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Assist.NotValid", livingToAssist.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }

        private static bool SameRealm(GameClient client, GameLiving livingToCheck, bool usePvEPvPRule)
        {
            if (usePvEPvPRule)
            {
                if (livingToCheck.Realm != 0)
                    return true;
            }
            else
            {
                if (livingToCheck.Realm == client.Player.Realm)
                    return true;
            }

            // We cannot assist livings of an enemy realm.
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Assist.NoRealmMember", livingToCheck.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }

        private static void YouAssist(GameClient client, string targetName, GameObject assistTarget)
        {
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Assist.YouAssist", targetName), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            client.Out.SendChangeTarget(assistTarget);
            return;
        }
    }
}
