using System;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&chat",
		new string[] { "&c" },
		ePrivLevel.Player,
		"Chat group command",
		"/c <text>")]
	public class ChatGroupCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "chat"))
				return;

			ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
			if (mychatgroup == null)
			{
				client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			if (mychatgroup.Listen == true && (((bool)mychatgroup.Members[client.Player]) == false))
			{
				client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.OnlyModerator"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			if (args.Length < 2)
			{
				client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Usage"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
				ply.Out.SendMessage(message, eChatType.CT_Chat, eChatLoc.CL_ChatWindow);
			}
		}
	}

	[CmdAttribute(
		"&chatgroup",
		new string[] { "&cg" },
		ePrivLevel.Player,
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
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.UsageInvite"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}

						GamePlayer inviteePlayer = ClientService.GetPlayerByPartialName(args[2], out _);

						if (inviteePlayer == null || !GameServer.ServerRules.IsSameRealm(inviteePlayer, client.Player, true)) // allow priv level>1 to invite anyone
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NoPlayer"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if (client == inviteePlayer.Client)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InviteYourself"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						ChatGroup oldchatgroup = inviteePlayer.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (oldchatgroup != null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PlayerInChatgroup", inviteePlayer.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							mychatgroup = new ChatGroup();
							mychatgroup.AddPlayer(client.Player, true);
						}
						else if (((bool)mychatgroup.Members[client.Player]) == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderInvite"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						inviteePlayer.TempProperties.SetProperty(JOIN_CHATGROUP_PROPERTY, mychatgroup);
						inviteePlayer.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.JoinChatGroup", client.Player.Name), new CustomDialogResponse(JoinChatGroup));
					}
					break;
				case "who":
					{
						ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
							client.Out.SendMessage(text.ToString(), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							//TODO: make function formatstring
						}
					}
					break;
				case "remove":
					{
						ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if (args.Length < 3)
						{
							PrintHelp(client);
						}

						GamePlayer inviteePlayer = ClientService.GetPlayerByPartialName(args[2], out _);

						if (inviteePlayer == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NoPlayer"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.RemovePlayer(inviteePlayer);
					}
					break;
				case "leave":
					{
						ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.RemovePlayer(client.Player);
					}
					break;
				case "listen":
					{
						ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.Listen = !mychatgroup.Listen;
						string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.ListenMode") + (mychatgroup.Listen ? "on." : "off.");
						foreach (GamePlayer ply in mychatgroup.Members.Keys)
						{
							ply.Out.SendMessage(message, eChatType.CT_Chat, eChatLoc.CL_ChatWindow);
						}
					}
					break;
				case "leader":
					{
						ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if (args.Length < 3)
						{
							PrintHelp(client);
						}
						string invitename = String.Join(" ", args, 2, args.Length - 2);
						GamePlayer inviteePlayer = ClientService.GetPlayerByPartialName(invitename, out _);

						if (inviteePlayer == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NoPlayer"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.Members[inviteePlayer] = true;
						string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Moderator", inviteePlayer.Name);
						foreach (GamePlayer ply in mychatgroup.Members.Keys)
						{
							ply.Out.SendMessage(message, eChatType.CT_Chat, eChatLoc.CL_ChatWindow);
						}
					}
					break;
				case "public":
					{
						ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if (mychatgroup.IsPublic)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PublicAlready"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						else
						{
							mychatgroup.IsPublic = true;
							string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Public");
							foreach (GamePlayer ply in mychatgroup.Members.Keys)
							{
								ply.Out.SendMessage(message, eChatType.CT_Chat, eChatLoc.CL_ChatWindow);
							}
						}
					}
					break;
				case "private":
					{
						ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if (!mychatgroup.IsPublic)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PrivateAlready"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						else
						{
							mychatgroup.IsPublic = false;
							string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Private");
							foreach (GamePlayer ply in mychatgroup.Members.Keys)
							{
								ply.Out.SendMessage(message, eChatType.CT_Chat, eChatLoc.CL_ChatWindow);
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

						GamePlayer inviteePlayer = ClientService.GetPlayerByPartialName(args[2], out _);

						if (inviteePlayer == null || !GameServer.ServerRules.IsSameRealm(client.Player, inviteePlayer, true)) // allow priv level>1 to join anywhere
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NoPlayer"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if (client == inviteePlayer.Client)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.OwnChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}

						ChatGroup mychatgroup = inviteePlayer.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NotChatGroupMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[inviteePlayer] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NotChatGroupLeader"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.NotPublic"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
						}
						else
							mychatgroup.AddPlayer(client.Player, false);
					}
					break;
				case "password":
					{
						ChatGroup mychatgroup = client.Player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
						if (mychatgroup == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.InChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if ((bool)mychatgroup.Members[client.Player] == false)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.LeaderCommand"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if (args.Length < 3)
						{
							if (mychatgroup.Password.Equals(""))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PasswordUnset", mychatgroup.Password), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Password", mychatgroup.Password), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
						}
						if (args[2] == "clear")
						{
							mychatgroup.Password = "";
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PasswordClear", mychatgroup.Password), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						mychatgroup.Password = args[2];
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.PasswordChanged", mychatgroup.Password), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					break;
			}
		}

		public void PrintHelp(GameClient client)
		{
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Usage"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Help"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Invite"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Who"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Remove"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Leave"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Listen"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Leader"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Public"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.Private"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.JoinPublic"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.JoinPrivate"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.PasswordDisplay"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.PasswordClear"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Chatgroup.Help.PasswordNew"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}

		protected const string JOIN_CHATGROUP_PROPERTY = "JOIN_CHATGROUP_PROPERTY";

		public static void JoinChatGroup(GamePlayer player, byte response)
		{
			ChatGroup mychatgroup = player.TempProperties.GetProperty<ChatGroup>(JOIN_CHATGROUP_PROPERTY, null);
			if (mychatgroup == null) return;
			lock (mychatgroup)
			{
				if (mychatgroup.Members.Count < 1)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Chatgroup.NoChatGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}
				if (response == 0x01)
				{
					mychatgroup.AddPlayer(player, false);
				}
				player.TempProperties.RemoveProperty(JOIN_CHATGROUP_PROPERTY);
			}
		}
	}
}
