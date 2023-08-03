using System;
using System.Collections;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;
using System.Reflection;
namespace DOL.GS.Commands
{
    [Command(
        "&battlechat",
        new string[] { "&bc", "&bchat" },
        EPrivLevel.Player,
        "Battle group command",
        "/bc <text>")]
    public class BattleGroupCommand : AbstractCommandHandler, ICommandHandler
    {
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "battlechat"))
                return;

            BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
            if (mybattlegroup == null)
            {
                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            var isLeader = mybattlegroup.IsBGLeader(client.Player);
            var isModerator = mybattlegroup.IsBGModerator(client.Player);
            if (mybattlegroup.Listen && !isLeader && !isModerator)
            {
                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.OnlyModerator"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }
            if (args.Length < 2)
            {
                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Usage"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            StringBuilder text = new StringBuilder(7 + 3 + client.Player.Name.Length + (args.Length - 1) * 8);

            text.Append("[BattleGroup] ");
            text.Append(client.Player.Name);
            text.Append(": \"");
            text.Append(args[1]);
            for (int i = 2; i < args.Length; i++)
            {
                text.Append(" ");
                text.Append(args[i]);
            }
            text.Append("\"");
            var message = text.ToString();
            foreach (GamePlayer ply in mybattlegroup.Members.Keys)
            {
                EChatType type;

                if (isLeader || isModerator)
                {
                    type = EChatType.CT_BattleGroupLeader;
                }
                else
                {
                    type = EChatType.CT_BattleGroup;
                }

                if (ply.IgnoreList.Contains(client.Player)) continue;
                
                ply.Out.SendMessage(message,type, EChatLoc.CL_ChatWindow);
            }
        }
    }

    [Command(
        "&battlegroup",
        new string[] { "&bg" },
        EPrivLevel.Player,
        "Battle group command",
        "/bg <option>")]
    public class BattleGroupSetupCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "battlegroup"))
                return;

            if (args.Length < 2)
            {
                PrintHelp(client);
                return;
            }
            switch (args[1].ToLower())
            {
                case "help":
                    {
                        PrintHelp(client);
                    }
                    break;
                case "invite":
                    {
                        if (client.Player == null)
                            return;

                        if (args.Length < 3)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.UsageInvite"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        GameClient inviteeclient = WorldMgr.GetClientByPlayerName(args[2], false, true);
                        if (inviteeclient == null || !GameServer.ServerRules.IsSameRealm(inviteeclient.Player, client.Player, true)) // allow priv level>1 to invite anyone
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (client == inviteeclient)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InviteYourself"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        BattleGroup oldbattlegroup = inviteeclient.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (oldbattlegroup != null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.PlayerInBattlegroup", inviteeclient.Player.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        /*if (mybattlegroup == null)
                        {
                            mybattlegroup = new BattleGroup();
                            mybattlegroup.SetBGLeader(client.Player);
                            mybattlegroup.AddBattlePlayer(client.Player, true);
                        }
                        else*/
                        if (mybattlegroup != null && mybattlegroup.IsBGLeader(client.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderInvite"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        inviteeclient.Player.TempProperties.setProperty(JOIN_BATTLEGROUP_PROPERTY, mybattlegroup);
                        inviteeclient.Player.TempProperties.setProperty(PLAYER_INVITE_SENDER, client.Player);
                        inviteeclient.Player.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.JoinBattleGroup", client.Player.Name), new CustomDialogResponse(JoinBattleGroup));
                    }
                    break;
                case "groups":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        StringBuilder text = new StringBuilder(ServerProperties.ServerProperties.BATTLEGROUP_MAX_MEMBER); //create the string builder
                        ArrayList curBattleGroupGrouped = new ArrayList(); //create the arraylist
                        ArrayList curBattleGroupNotGrouped = new ArrayList();
                        int i = 1; //This will list each group in the battle group.
                        text.Length = 0;
                        text.Append("The group structure of your Battle Group:");
                        client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        text.Length = 0;

                        foreach (GamePlayer player in mybattlegroup.Members.Keys)
                        {
                            if (player.Group != null && player.Group.MemberCount > 1)
                            {
                                curBattleGroupGrouped.Add(player);
                            }
                            else
                            {
                                curBattleGroupNotGrouped.Add(player);
                            }
                        }

                        ArrayList ListedPeople = new ArrayList();
                        int firstrun = 0;
                        foreach (GamePlayer grouped in curBattleGroupGrouped)
                        {
                            if (firstrun == 0)
                            {
                                text.Length = 0;
                                text.Append(i);
                                text.Append(") ");
                                i++; 
                                text.Append(grouped.Group.GroupMemberString(grouped));
                                client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                firstrun = 1;
                            }
                            else if (!ListedPeople.Contains(grouped))
                            {
                                text.Length = 0;
                                text.Append(i);
                                text.Append(") ");
                                i++;
                                text.Append(grouped.Group.GroupMemberString(grouped));
                                client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            }

                            foreach (GamePlayer gpl in grouped.Group.GetPlayersInTheGroup())
                            {
                                if (mybattlegroup.IsInTheBattleGroup(gpl))
                                    ListedPeople.Add(gpl);
                            }
                        }

                        foreach (GamePlayer nongrouped in curBattleGroupNotGrouped)
                        {
                            text.Length = 0;
                            text.Append(i);
                            text.Append(") ");
                            i++;

                            text.Append(nongrouped.Name + '\n');
                            client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                    break;
                case "groupclass":
                    {
                        if (client.Player == null)
                            return;

                        var mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        var text = new StringBuilder(ServerProperties.ServerProperties.BATTLEGROUP_MAX_MEMBER); //create the string builder
                        var curBattleGroupGrouped = new ArrayList(); //create the arraylist
                        var i = 1; //This will list each group in the battle group.
                        text.Length = 0;
                        text.Append("Groups currently in the BG:");
                        client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        text.Length = 0;

                        foreach (GamePlayer player in mybattlegroup.Members.Keys)
                        {
                            if (player.Group is {MemberCount: > 1})
                            {
                                curBattleGroupGrouped.Add(player);
                            }
                        }

                        var ListedPeople = new ArrayList();
                        var firstrun = 0;
                        foreach (GamePlayer grouped in curBattleGroupGrouped)
                        {
                            if (firstrun == 0)
                            {
                                text.Length = 0;
                                text.Append($"{i}) {grouped.Group.GroupMemberClassString(grouped)}");
                                client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                i++;
                                firstrun = 1;
                            }
                            else if (!ListedPeople.Contains(grouped))
                            {
                                text.Length = 0;
                                text.Append($"{i}) {grouped.Group.GroupMemberClassString(grouped)}");
                                i++;
                                client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            }

                            foreach (GamePlayer gpl in grouped.Group.GetPlayersInTheGroup())
                            {
                                if (mybattlegroup.IsInTheBattleGroup(gpl))
                                    ListedPeople.Add(gpl);
                            }
                        }
                    }
                    break;
                
                case "solo":
                    {
                        if (client.Player == null)
                            return;

                        var mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        var text = new StringBuilder(ServerProperties.ServerProperties.BATTLEGROUP_MAX_MEMBER); //create the string builder
                        var curBattleGroupNotGrouped = new ArrayList();
                        var i = 1; //This will list each group in the battle group.
                        text.Length = 0;
                        text.Append("Solo players currently in the BG:");
                        client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        text.Length = 0;

                        foreach (GamePlayer player in mybattlegroup.Members.Keys)
                        {
                            if (player.Group == null)
                            {
                                curBattleGroupNotGrouped.Add(player);
                            }
                        }
                        foreach (GamePlayer nongrouped in curBattleGroupNotGrouped)
                        {
                            text.Length = 0;
                            text.Append(i);
                            text.Append(") ");
                            i++;

                            var nongroupedplayer = mybattlegroup.Members[nongrouped];

                            if (nongroupedplayer == null)
                                continue;

                            var player = nongroupedplayer as GamePlayer;
                            
                            if (mybattlegroup.IsBGLeader(player))
                            {
                                text.Append(" <Leader> ");
                            }
                            else
                            {
                                text.Append(" <Member> ");
                            }
                            
                            text.Append($"{nongrouped.Name}, the level {nongrouped.Level} {nongrouped.CharacterClass.Name} \n");
                            client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                    break;

                case "who":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        int i = 0;
                        StringBuilder text = new StringBuilder(ServerProperties.ServerProperties.BATTLEGROUP_MAX_MEMBER);
                        text.Length = 0;
                        text.Append("Players currently in Battle Group:");
                        client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        foreach (GamePlayer player in mybattlegroup.Members.Keys)
                        {
                            i++;
                            text.Length = 0;
                            text.Append(i);
                            text.Append(") ");

                            if (mybattlegroup.IsBGLeader(player) == true)
                            {
                                text.Append(" <Leader> ");
                            }
                            else if (mybattlegroup.IsBGTreasurer(player) == true)
                            {
                                text.Append(" <Treasurer> ");
                            }
                            else
                            {
                                text.Append(" <Member> ");
                            }

                            text.Append(player.Name);

                            client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            //TODO: make function formatstring                        
                        }
                    }
                    break;
                case "remove":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (args.Length < 3)
                        {
                            PrintHelp(client);
                        }
                        if (mybattlegroup.IsBGLeader(client.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        GameClient inviteeclient = WorldMgr.GetClientByPlayerName(args[2], false, false);
                        if (inviteeclient == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        mybattlegroup.RemoveBattlePlayer(inviteeclient.Player);
                    }
                    break;
                case "leave":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        mybattlegroup.RemoveBattlePlayer(client.Player);
                    }
                    break;
                case "listen":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (mybattlegroup.IsBGLeader(client.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        mybattlegroup.Listen = !mybattlegroup.Listen;
                        string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.ListenMode") + (mybattlegroup.Listen ? "on." : "off.");
                        foreach (GamePlayer ply in mybattlegroup.Members.Keys)
                        {
                            ply.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                    break; 
                case "promote":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (mybattlegroup.IsBGLeader(client.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (args.Length < 3)
                        {
                            PrintHelp(client);
                        }

                        string invitename = String.Join(" ", args, 2, args.Length - 2);
                        GameClient inviteeclient = WorldMgr.GetClientByPlayerName(invitename, false, false);
                        if (inviteeclient == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        var message = "";
                        var isMod = mybattlegroup.IsBGModerator(inviteeclient.Player);

                        if (!isMod)
                        {
                            mybattlegroup.Moderators.Add(inviteeclient.Player);
                            message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Moderator", inviteeclient.Player.Name);

                        }
                        else
                        {
                            mybattlegroup.Moderators.Remove(inviteeclient.Player);
                            message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.RemoveModerator", inviteeclient.Player.Name);
                        }
                        
                        foreach (GamePlayer ply in mybattlegroup.Members.Keys)
                        {
                            ply.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                    break;
                case "makeleader":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (mybattlegroup.IsBGLeader(client.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (args.Length < 3)
                        {
                            PrintHelp(client);
                        }

                        string invitename = String.Join(" ", args, 2, args.Length - 2);
                        GameClient inviteeclient = WorldMgr.GetClientByPlayerName(invitename, false, false);
                        if (inviteeclient == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        mybattlegroup.SetBGLeader(inviteeclient.Player);
                        mybattlegroup.Members[inviteeclient.Player] = true;
                        string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NewLeader", inviteeclient.Player.Name);
                        foreach (GamePlayer ply in mybattlegroup.Members.Keys)
                        {
                            ply.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                    break;
                case "public":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (mybattlegroup.IsBGLeader(client.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        mybattlegroup.IsPublic = true;
                        string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Public");
                        foreach (GamePlayer ply in mybattlegroup.Members.Keys)
                        {
                            ply.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                    break;
                case "private":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (mybattlegroup.IsBGLeader(client.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        mybattlegroup.IsPublic = false;
                        string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Private");
                        foreach (GamePlayer ply in mybattlegroup.Members.Keys)
                        {
                            ply.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                    break;
                case "join":
                    {
                        if (args.Length < 3)
                        {
                            PrintHelp(client);
                            return;
                        }

                        if (client.Player == null)
                            return;

                        BattleGroup oldbattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (oldbattlegroup != null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.AlreadyInBattlegroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        GameClient inviteeclient = WorldMgr.GetClientByPlayerName(args[2], false, false);
                        if (inviteeclient == null || !GameServer.ServerRules.IsSameRealm(client.Player, inviteeclient.Player, true)) // allow priv level>1 to join anywhere
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (client == inviteeclient)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.OwnBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        BattleGroup mybattlegroup = inviteeclient.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NotBattleGroupMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (mybattlegroup.IsBGLeader(inviteeclient.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NotBattleGroupLeader"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (!mybattlegroup.IsPublic)
                        {
                            if (args.Length == 4 && args[3] == mybattlegroup.Password)
                            {
                                mybattlegroup.AddBattlePlayer(client.Player, false);
                            }
                            else
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NotPublic"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            }
                        }
                        else
                            mybattlegroup.AddBattlePlayer(client.Player, false);
                    }
                    break;
                case "password":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (mybattlegroup.IsBGLeader(client.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (args.Length < 3)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Password", mybattlegroup.Password) + mybattlegroup.Password, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (args[2] == "clear")
                        {
                            mybattlegroup.Password = "";
                            return;
                        }
                        mybattlegroup.Password = args[2];
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.PasswordChanged", mybattlegroup.Password) + mybattlegroup.Password, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    }
                    break;
                case "count":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup curbattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (curbattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.BattlegroupCount", curbattlegroup.Members.Count), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    }
                    break;
                case "loot":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (mybattlegroup.Listen == true && (mybattlegroup.IsBGLeader(client.Player) == false))
                        {
                            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (args.Length < 3)
                        {
                            client.Out.SendMessage("You should use /bg loot normal or /bg loot treasurer.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (args[2] == "normal" || args[2] == "norm" || args[2] == "n" || args[2] == "N" || args[2] == "Norm" || args[2] == "Normal")
                        {
                            mybattlegroup.SetBGLootType(false);
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.BattleGroupLootNormal"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                        else if (args[2] == "treasurer" || args[2] == "treasure" || args[2] == "t" || args[2] == "T" || args[2] == "Treasurer" || args[2] == "Treasure")
                        {
                            mybattlegroup.SetBGLootType(true);
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.BattleGroupLootTreasurer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                    break;
                case "lootlevel":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (mybattlegroup.Listen == true && (mybattlegroup.IsBGLeader(client.Player) == false))
                        {
                            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (args.Length == 2)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.BattlegroupLootThresholdOn", mybattlegroup.GetBGLootTypeThreshold()), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        else if (args.Length == 3)
                        {
                            int id_level;
                            if (Int32.TryParse((args[2]), out id_level))
                            {
                                mybattlegroup.SetBGLootTypeThreshold(id_level);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.BattlegroupLootThresholdOff"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            }
                        }
                    }
                    break;
                case "treasurer":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (mybattlegroup.IsBGLeader(client.Player) == false)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (args.Length < 3)
                        {
                            PrintHelp(client);
                        }

                        string treasname = String.Join(" ", args, 2, args.Length - 2);
                        if (treasname == null || treasname == "")
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NoPlayer", treasname), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        //log.Debug("treasname= (" + treasname + ")");
                        GameClient treasclient = WorldMgr.GetClientByPlayerName(treasname, true, true);
                        if (treasclient == null || treasclient.Player == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NoPlayer", treasname), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (treasclient.Player.Realm != client.Player.Realm)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.NoPlayer", treasname), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (!mybattlegroup.IsInTheBattleGroup(treasclient.Player))
                        {
                            client.Out.SendMessage("This player is not in your battleground.", EChatType.CT_System, EChatLoc.CL_SystemWindow);

                            return;
                        }
                        mybattlegroup.SetBGTreasurer(treasclient.Player);
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.BattlegroupTreasurerOn", treasclient.Player.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        treasclient.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.BattlegroupTreasurerIsYou"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        foreach (GamePlayer ply in mybattlegroup.Members.Keys)
                        {
                            ply.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.BattlegroupTreasurerIs", treasclient.Player.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                        if (mybattlegroup.GetBGTreasurer() == null)
                        {
                            foreach (GamePlayer ply in mybattlegroup.Members.Keys)
                            {
                                ply.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.BattlegroupTreasurerOff"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            }
                        }
                    }
                    break;
                case "recordstart":
                    {
                        if (client.Player == null)
                            return;

                        BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                        if (mybattlegroup == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (!mybattlegroup.IsBGLeader(client.Player))
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        
                        if (args.Length < 3)
                        {
                            client.Out.SendMessage("You need to specify a valid max roll value (i.e. /bg recordstart 1000)", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        int maxRoll;
                        
                        try
                        {
                            maxRoll = Convert.ToInt32(args[2]);
                        }
                        catch (Exception)
                        {
                            client.Out.SendMessage("You need to specify a valid max roll value (i.e. /bg recordstart 1000)", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }
                        
                        mybattlegroup.StartRecordingRolls(maxRoll);
                        
                    }
                    break;
                case "recordstop":
                {
                    if (client.Player == null)
                        return;

                    BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                    if (mybattlegroup == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        return;
                    }
                    if (!mybattlegroup.IsBGLeader(client.Player))
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        return;
                    }
                        
                    mybattlegroup.StopRecordingRolls();
                    mybattlegroup.ShowRollsWindow(client.Player);
                        
                } 
                    break;
                case "showrolls":
                {
                    if (client.Player == null)
                        return;

                    BattleGroup mybattlegroup = client.Player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
                    if (mybattlegroup == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.InBattleGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        return;
                    }
                    
                    mybattlegroup.ShowRollsWindow(client.Player);
                        
                } 
                    break;

                default:
                    {
                        PrintHelp(client);
                    }
                    break;
            }
        }

        public void PrintHelp(GameClient client)
        {
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Usage"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Help"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Invite"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Who"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Remove"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Leave"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Listen"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Leader"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.MakeLeader"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Public"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Private"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.JoinPublic"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.JoinPrivate"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.PasswordDisplay"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.PasswordClear"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.PasswordNew"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Count"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Groups"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.GroupClass"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Solo"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Loot"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.Treasurer"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.LootLevel"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.RecordStart"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.RecordStop"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Battlegroup.Help.ShowRolls"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
        }

        protected const string JOIN_BATTLEGROUP_PROPERTY = "JOIN_BATTLEGROUP_PROPERTY";
        public const string PLAYER_INVITE_SENDER = "PLAYER_INVITE_SENDER";
        public static void JoinBattleGroup(GamePlayer player, byte response)
        {
            /*BattleGroup mybattlegroupinvite = player.TempProperties.getProperty<BattleGroup>(JOIN_BATTLEGROUP_PROPERTY, null);
            if (mybattlegroupinvite == null) 
                return;*/

            GamePlayer leader = player.TempProperties.getProperty<GamePlayer>(PLAYER_INVITE_SENDER, null);
            if (leader == null)
            {
                player.TempProperties.removeProperty(JOIN_BATTLEGROUP_PROPERTY);
                player.TempProperties.removeProperty(PLAYER_INVITE_SENDER);
                return;
            }


            BattleGroup mybattlegroup = leader.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
            if (mybattlegroup != null)
            {
                //log.Debug("bg");
                lock (mybattlegroup)
                {
                    if (mybattlegroup.Members.Count < 1)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Battlegroup.NoBattleGroup"), EChatType.CT_BattleGroup, EChatLoc.CL_SystemWindow);
                        return;
                    }
                    if (response == 0x01)
                    {
                        mybattlegroup.AddBattlePlayer(player, false);
                    }

                }
            }
            else
            {
                //log.Debug("no bg");
                if (response == 0x01)
                {
                    GameClient inviteeclient = WorldMgr.GetClientByPlayerName(leader.Name, false, true);
                    if (inviteeclient != null)
                    {
                        BattleGroup battlegroup = new BattleGroup();

                        lock (battlegroup)
                        {
                            battlegroup.SetBGLeader(leader);
                            battlegroup.AddBattlePlayer(leader, true);
                            battlegroup.AddBattlePlayer(player, false);
                        }
                    }
                    else
                        player.Client.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Battlegroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    
                }
            }
            player.TempProperties.removeProperty(JOIN_BATTLEGROUP_PROPERTY);
            player.TempProperties.removeProperty(PLAYER_INVITE_SENDER);
        }
    }
}