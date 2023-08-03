using System;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[Command(
		"&chat",
		new string[] { "&c" },
		EPrivLevel.Player,
		"Chat group command",
		"/c <text>")]
	public class ChatGroupCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "chat"))
				return;

			ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
			if (mychatgroup == null)
			{
				client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (mychatgroup.Listen == true && (((bool)mychatgroup.Members[client.Player]) == false))
			{
				client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.OnlyModerator"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (args.Length < 2)
			{
				client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Usage"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			StringBuilder text = new StringBuilder(7 + 3 + client.Player.Name.Length + (args.Length - 1) * 8);
			text.Append("[Chat] ");
			text.Append(client.Player.Name);
			text.Append(": \"");
			text.Append(args[1]);
			for (int i = 2; i < args.Length; i++)
			{
				text.Append(" ");
				text.Append(args[i]);
			}
			text.Append("\"");
			string message = text.ToString();
			foreach (GamePlayer ply in mychatgroup.Members.Keys)
			{
				ply.Out.SendMessage(message, EChatType.CT_Chat, EChatLoc.CL_ChatWindow);
			}
		}
	}

	[Command(
		"&chatgroup",
		new string[] { "&cg" },
		EPrivLevel.Player,
		"Chat group command",
		"/cg <option>")]
	public class ChatGroupSetupCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "chatgroup"))
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
						if (args.Length < 3)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.UsageInvite"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						GameClient inviteeclient = WorldMgr.GetClientByPlayerName(args[2], false, true);
						if (inviteeclient == null || !GameServer.ServerRules.IsSameRealm(inviteeclient.Player, client.Player, true)) // allow priv level>1 to invite anyone
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if (client == inviteeclient)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InviteYourself"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						ChatGroup oldchatgroup = (ChatGroup)inviteeclient.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (oldchatgroup != null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PlayerInChatgroup", inviteeclient.Player.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							mychatgroup = new ChatGroup();
							mychatgroup.AddPlayer(client.Player, true);
						}
						else if (((bool)mychatgroup.Members[client.Player]) == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderInvite"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						inviteeclient.Player.TempProperties.setProperty(JOIN_CHATGROUP_PROPERTY, mychatgroup);
						inviteeclient.Player.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.JoinChatGroup", client.Player.Name), new CustomDialogResponse(JoinChatGroup));
					}
					break;
				case "who":
					{
						ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						int i = 0;
						StringBuilder text = new StringBuilder(64);
						foreach (GamePlayer player in mychatgroup.Members.Keys)
						{
							i++;
							text.Length = 0;
							text.Append(i);
							text.Append(") ");
							text.Append(player.Name);
							if (player.Guild != null)
							{
								text.Append(" <");
								text.Append(player.GuildName);
								text.Append(">");
							}
							text.Append(" (");
							text.Append(player.CharacterClass.Name);
							text.Append(")");
							client.Out.SendMessage(text.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							//TODO: make function formatstring
						}
					}
					break;
				case "remove":
					{
						ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if (args.Length < 3)
						{
							PrintHelp(client);
						}
						GameClient inviteeclient = WorldMgr.GetClientByPlayerName(args[2], false, false);
						if (inviteeclient == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.RemovePlayer(inviteeclient.Player);
					}
					break;
				case "leave":
					{
						ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.RemovePlayer(client.Player);
					}
					break;
				case "listen":
					{
						ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.Listen = !mychatgroup.Listen;
						string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.ListenMode") + (mychatgroup.Listen ? "on." : "off.");
						foreach (GamePlayer ply in mychatgroup.Members.Keys)
						{
							ply.Out.SendMessage(message, EChatType.CT_Chat, EChatLoc.CL_ChatWindow);
						}
					}
					break;
				case "leader":
					{
						ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
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
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.Members[inviteeclient.Player] = true;
						string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Moderator", inviteeclient.Player.Name);
						foreach (GamePlayer ply in mychatgroup.Members.Keys)
						{
							ply.Out.SendMessage(message, EChatType.CT_Chat, EChatLoc.CL_ChatWindow);
						}
					}
					break;
				case "public":
					{
						ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if (mychatgroup.IsPublic)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PublicAlready"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						else
						{
							mychatgroup.IsPublic = true;
							string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Public");
							foreach (GamePlayer ply in mychatgroup.Members.Keys)
							{
								ply.Out.SendMessage(message, EChatType.CT_Chat, EChatLoc.CL_ChatWindow);
							}
						}
					}
					break;
				case "private":
					{
						ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if (!mychatgroup.IsPublic)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PrivateAlready"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						else
						{
							mychatgroup.IsPublic = false;
							string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Private");
							foreach (GamePlayer ply in mychatgroup.Members.Keys)
							{
								ply.Out.SendMessage(message, EChatType.CT_Chat, EChatLoc.CL_ChatWindow);
							}
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
						GameClient inviteeclient = WorldMgr.GetClientByPlayerName(args[2], false, false);
						if (inviteeclient == null || !GameServer.ServerRules.IsSameRealm(client.Player, inviteeclient.Player, true)) // allow priv level>1 to join anywhere
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NoPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if (client == inviteeclient)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.OwnChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						ChatGroup mychatgroup = (ChatGroup)inviteeclient.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NotChatGroupMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[inviteeclient.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NotChatGroupLeader"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if (!mychatgroup.IsPublic)
						{
							if (args.Length == 4 && args[3] == mychatgroup.Password)
							{
								mychatgroup.AddPlayer(client.Player, false);
							}
							else
							{
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NotPublic"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
						}
						else
							mychatgroup.AddPlayer(client.Player, false);
					}
					break;
				case "password":
					{
						ChatGroup mychatgroup = (ChatGroup)client.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if (args.Length < 3)
						{
							if (mychatgroup.Password.Equals(""))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PasswordUnset", mychatgroup.Password), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Password", mychatgroup.Password), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
						}
						if (args[2] == "clear")
						{
							mychatgroup.Password = "";
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PasswordClear", mychatgroup.Password), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.Password = args[2];
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PasswordChanged", mychatgroup.Password), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
					break;
			}
		}

		public void PrintHelp(GameClient client)
		{
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Usage"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Help"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Invite"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Who"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Remove"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Leave"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Listen"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Leader"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Public"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Private"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.JoinPublic"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.JoinPrivate"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.PasswordDisplay"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.PasswordClear"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.PasswordNew"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}

		protected const string JOIN_CHATGROUP_PROPERTY = "JOIN_CHATGROUP_PROPERTY";

		public static void JoinChatGroup(GamePlayer player, byte response)
		{
			ChatGroup mychatgroup = (ChatGroup)player.TempProperties.getProperty<object>(JOIN_CHATGROUP_PROPERTY, null);
			if (mychatgroup == null) return;
			lock (mychatgroup)
			{
				if (mychatgroup.Members.Count < 1)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Chatgroup.NoChatGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
				if (response == 0x01)
				{
					mychatgroup.AddPlayer(player, false);
				}
				player.TempProperties.removeProperty(JOIN_CHATGROUP_PROPERTY);
			}
		}
	}
}