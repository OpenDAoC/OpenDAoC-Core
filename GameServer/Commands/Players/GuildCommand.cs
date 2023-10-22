using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Keeps;
using Core.GS.Languages;
using Core.GS.Packets;
using Core.GS.Packets.Server;
using Core.GS.Server;
using Core.GS.World;

namespace Core.GS.Commands
{
	/// <summary>
	/// command handler for /gc command
	/// </summary>
	[Command(
		"&gc",
		new string[] { "&guildcommand" },
		EPrivLevel.Player,
		"Guild command (use /gc help for options)",
		"/gc <option>")]
	public class GuildCommand : ACommandHandler, ICommandHandler
	{
		
		private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public long GuildFormCost = MoneyMgr.GetMoney(0, 0, 1, 0, 0); //Cost to form guild : live = 1g : (mith/plat/gold/silver/copper)
		/// <summary>
		/// Checks if a guildname has valid characters
		/// </summary>
		/// <param name="guildName"></param>
		/// <returns></returns>
		public static bool IsValidGuildName(string guildName)
		{
			if (!Regex.IsMatch(guildName, @"^[a-zA-Z àâäèéêëîïôœùûüÿçÀÂÄÈÉÊËÎÏÔŒÙÛÜŸÇ]+$") || guildName.Length < 0)

			{
				return false;
			}
			return true;
		}
		private static bool IsNearRegistrar(GamePlayer player)
		{
			foreach (GameNpc registrar in player.GetNPCsInRadius(500))
			{
				if (registrar is GuildRegistrar)
					return true;
			}
			return false;
		}
		private static bool GuildFormCheck(GamePlayer leader)
		{
			GroupUtil group = leader.Group;
			#region No group check - Ensure we still have a group
			if (group == null)
			{
				leader.Out.SendMessage(LanguageMgr.GetTranslation(leader.Client.Account.Language, "Scripts.Player.Guild.FormNoGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}
			#endregion
			#region Enough members to form Check - Ensure our group still has enough players in to form
			if (group.MemberCount < ServerProperty.GUILD_NUM)
			{
				leader.Out.SendMessage(LanguageMgr.GetTranslation(leader.Client.Account.Language, "Scripts.Player.Guild.FormNoMembers" + ServerProperty.GUILD_NUM), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}
			#endregion

			return true;
		}

		protected void CreateGuild(GamePlayer player, byte response)
		{
			if (player.Group == null)
			{
				player.Out.SendMessage("There was an issue processing guild request. Please try again.", EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				return;
			}

			#region Player Declines
			if (response != 0x01)
			{
				//remove all guild consider to enable re try
				foreach (GamePlayer ply in player.Group.GetPlayersInTheGroup())
				{
					ply.TempProperties.RemoveProperty("Guild_Consider");
				}
				player.Group.Leader.TempProperties.RemoveProperty("Guild_Name");
				player.Group.SendMessageToGroupMembers(player, "Declines to form the guild", EChatType.CT_Group, EChatLoc.CL_ChatWindow);
				return;
			}
			#endregion
			#region Player Accepts
			player.Group.SendMessageToGroupMembers(player, "Agrees to form the guild", EChatType.CT_Group, EChatLoc.CL_ChatWindow);
			player.TempProperties.SetProperty("Guild_Consider", true);
			var guildname = player.Group.Leader.TempProperties.GetProperty<string>("Guild_Name");

			var memnum = player.Group.GetPlayersInTheGroup().Count(p => p.TempProperties.GetProperty<bool>("Guild_Consider"));

			if (!GuildFormCheck(player) || memnum != player.Group.MemberCount) return;

			if (ServerProperty.GUILD_NUM > 1)
			{
				GroupUtil group = player.Group;
				lock (group)
				{
					GuildUtil newGuild = GuildMgr.CreateGuild(player.Realm, guildname, player);
					if (newGuild == null)
					{
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.UnableToCreateLead", guildname, player.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
					else
					{
						foreach (GamePlayer ply in group.GetPlayersInTheGroup())
						{
							if (ply != group.Leader)
							{
								newGuild.AddPlayer(ply);
							}
							else
							{
								newGuild.AddPlayer(ply, newGuild.GetRankByID(0));
							}
							ply.TempProperties.RemoveProperty("Guild_Consider");
						}
						player.Group.Leader.TempProperties.RemoveProperty("Guild_Name");
						player.Group.Leader.RemoveMoney(GuildFormCost);
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.GuildCreated", guildname, player.Group.Leader.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
				}
			}
			else
			{
				GuildUtil newGuild = GuildMgr.CreateGuild(player.Realm, guildname, player);

				if (newGuild == null)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.UnableToCreateLead", guildname, player.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				else
				{
					newGuild.AddPlayer(player, newGuild.GetRankByID(0));
					player.TempProperties.RemoveProperty("Guild_Name");
					player.RemoveMoney(10000);
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.GuildCreated", guildname, player.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				}
			}
			#endregion
		}

		/// <summary>
		/// method to handle /gc commands from a client
		/// </summary>
		/// <param name="client"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "gc", 500))
				return;

			try
			{
				if (args.Length == 1)
				{
					DisplayHelp(client);
					return;
				}

				if (client.Player.IsIncapacitated)
				{
					return;
				}


				string message;

				// Use this to aid in debugging social window commands
				//string debugArgs = "";
				//foreach (string arg in args)
				//{
				//    debugArgs += arg + " ";
				//}
				//log.Debug(debugArgs);

				switch (args[1])
				{
						#region Create
						// --------------------------------------------------------------------------------
						// CREATE
						// --------------------------------------------------------------------------------
					case "create":
						{
							if (client.Account.PrivLevel == (uint)EPrivLevel.Player)
								return;

							if (args.Length < 3)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMCreate"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							GameLiving guildLeader = client.Player.TargetObject as GameLiving;
							string guildname = String.Join(" ", args, 2, args.Length - 2);
							guildname = GameServer.Database.Escape(guildname);
							if (!GuildMgr.DoesGuildExist(guildname))
							{
								if (guildLeader == null)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PlayerNotFound"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									return;
								}

								if (!IsValidGuildName(guildname))
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InvalidLetters"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									return;
								}
								else
								{
									GuildUtil newGuild = GuildMgr.CreateGuild(client.Player.Realm, guildname, client.Player);
									if (newGuild == null)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.UnableToCreate", newGuild.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									}
									else
									{
										newGuild.AddPlayer((GamePlayer)guildLeader);
										((GamePlayer)guildLeader).GuildRank = ((GamePlayer)guildLeader).Guild.GetRankByID(0);
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.GuildCreated", guildname, ((GamePlayer)guildLeader).Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									}
									return;
								}
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.GuildExists"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Purge
						// --------------------------------------------------------------------------------
						// PURGE
						// --------------------------------------------------------------------------------
					case "purge":
						{
							if (client.Account.PrivLevel == (uint)EPrivLevel.Player)
								return;

							if (args.Length < 3)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMPurge"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							string guildname = String.Join(" ", args, 2, args.Length - 2);
							if (!GuildMgr.DoesGuildExist(guildname))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.GuildNotExist"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (GuildMgr.DeleteGuild(guildname))
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Purged", guildname), EChatType.CT_System, EChatLoc.CL_SystemWindow);
						}
						break;
						#endregion
						#region Rename
						// --------------------------------------------------------------------------------
						// RENAME
						// --------------------------------------------------------------------------------
					case "rename":
						{
							if (client.Account.PrivLevel == (uint)EPrivLevel.Player)
								return;

							if (args.Length < 5)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMRename"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							int i;
							for (i = 2; i < args.Length; i++)
							{
								if (args[i] == "to")
									break;
							}

							string oldguildname = String.Join(" ", args, 2, i - 2);
							string newguildname = String.Join(" ", args, i + 1, args.Length - i - 1);
							if (!GuildMgr.DoesGuildExist(oldguildname))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildNotExist"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							GuildUtil myguild = GuildMgr.GetGuildByName(oldguildname);
							myguild.Name = newguildname;
							GuildMgr.AddGuild(myguild);
							foreach (GamePlayer ply in myguild.GetListOfOnlineMembers())
							{
								ply.GuildName = newguildname;
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region AddPlayer
						// --------------------------------------------------------------------------------
						// ADDPLAYER
						// --------------------------------------------------------------------------------
					case "addplayer":
						{
							if (client.Account.PrivLevel == (uint)EPrivLevel.Player)
								return;

							if (args.Length < 5)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMAddPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							int i;
							for (i = 2; i < args.Length; i++)
							{
								if (args[i] == "to")
									break;
							}

							string playername = String.Join(" ", args, 2, i - 2);
							string guildname = String.Join(" ", args, i + 1, args.Length - i - 1);

							GuildMgr.GetGuildByName(guildname).AddPlayer(ClientService.GetPlayerByExactName(playername));
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region RemovePlayer
						// --------------------------------------------------------------------------------
						// REMOVEPLAYER
						// --------------------------------------------------------------------------------
					case "removeplayer":
						{
							if (client.Account.PrivLevel == (uint)EPrivLevel.Player)
								return;

							if (args.Length < 5)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMRemovePlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							int i;
							for (i = 2; i < args.Length; i++)
							{
								if (args[i] == "from")
									break;
							}

							string playername = String.Join(" ", args, 2, i - 2);
							string guildname = String.Join(" ", args, i + 1, args.Length - i - 1);

							if (!GuildMgr.DoesGuildExist(guildname))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildNotExist"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							GuildMgr.GetGuildByName(guildname).RemovePlayer("gamemaster", ClientService.GetPlayerByExactName(playername));
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Invite
						/****************************************guild member command***********************************************/
						// --------------------------------------------------------------------------------
						// INVITE
						// --------------------------------------------------------------------------------
					case "invite":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Invite))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							GamePlayer obj = client.Player.TargetObject as GamePlayer;
							if (args.Length > 2)
							{
								obj = ClientService.GetPlayerByExactName(args[2]);
							}
							if (obj == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InviteNoSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (obj == client.Player)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InviteNoSelf"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (obj.Guild != null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AlreadyInGuild"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!obj.IsAlive)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InviteDead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!GameServer.ServerRules.IsAllowedToGroup(client.Player, obj, true))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InviteNotThis"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!GameServer.ServerRules.IsAllowedToJoinGuild(obj, client.Player.Guild))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InviteNotThis"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							obj.Out.SendGuildInviteCommand(client.Player, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InviteRecieved", client.Player.Name, client.Player.Guild.Name));
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InviteSent", obj.Name, client.Player.Guild.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Remove
						// --------------------------------------------------------------------------------
						// REMOVE
						// --------------------------------------------------------------------------------
					case "remove":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Remove))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (args.Length < 3)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildRemove"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							object obj = null;
							string playername = args[2];
							if (playername == "")
								obj = client.Player.TargetObject as GamePlayer;
							else
							{
								obj = ClientService.GetPlayerByExactName(playername);
								obj ??= CoreDb<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(playername));
							}
							if (obj == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PlayerNotFound"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							string guildId = "";
							ushort guildRank = 9;
							string plyName = "";
							GamePlayer ply = obj as GamePlayer;
							DbCoreCharacter ch = obj as DbCoreCharacter;
							if (obj is GamePlayer)
							{
								plyName = ply.Name;
								guildId = ply.GuildID;
								if (ply.GuildRank != null)
									guildRank = ply.GuildRank.RankLevel;
							}
							else
							{
								plyName = ch.Name;
								guildId = ch.GuildID;
								guildRank = (byte)ch.GuildRank;
							}
							if (guildId != client.Player.GuildID)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotInYourGuild"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							foreach (GamePlayer plyon in client.Player.Guild.GetListOfOnlineMembers())
							{
								plyon.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.MemberRemoved", client.Player.Name, plyName), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							if (obj is GamePlayer)
								client.Player.Guild.RemovePlayer(client.Player.Name, ply);
							else
							{
								ch.GuildID = "";
								ch.GuildRank = 9;
								GameServer.Database.SaveObject(ch);
							}

							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Remove account
						// --------------------------------------------------------------------------------
						// REMOVE ACCOUNT (Patch 1.84)
						// --------------------------------------------------------------------------------
					case "removeaccount":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Remove))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (args.Length < 3)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildRemAccount"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							string accountName = String.Join(" ", args, 2, args.Length - 2);
							// Patch 1.84: look for offline players
							var chs = CoreDb<DbCoreCharacter>.SelectObjects(DB.Column("AccountName").IsEqualTo(accountName).And(DB.Column("GuildID").IsEqualTo(client.Player.GuildID)));
							if (chs.Count > 0)
							{
								GameClient myclient = ClientService.GetClientFromAccountName(accountName);
								string plys = "";
								bool isOnline = (myclient != null);
								foreach (DbCoreCharacter ch in chs)
								{
									plys += (plys != "" ? "," : "") + ch.Name;
									if (isOnline && ch.Name == myclient.Player.Name)
										client.Player.Guild.RemovePlayer(client.Player.Name, myclient.Player);
									else
									{
										ch.GuildID = "";
										ch.GuildRank = 9;
										GameServer.Database.SaveObject(ch);
									}
								}

								foreach (GamePlayer ply in client.Player.Guild.GetListOfOnlineMembers())
								{
									ply.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AccountRemoved", client.Player.Name, plys), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								}
							}
							else
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayersInAcc"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Info
						// --------------------------------------------------------------------------------
						// INFO
						// --------------------------------------------------------------------------------
					case "info":
						{
							bool typed = false;
							if (args.Length != 3)
								typed = true;

							if (client.Player.Guild == null)
							{
								if (!(args.Length == 3 && args[2] == "1"))
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								}
								return;
							}

							if (typed)
							{
								/*
								 * Guild Info for Clan Cotswold:
								 * Realm Points: xxx Bouty Points: xxx Merit Points: xxx
								 * Guild Level: xx
								 * Dues: 0% Bank: 0 copper pieces
								 * Current Merit Bonus: None
								 * Banner available for purchase
								 * Webpage: xxx
								 * Contact Email:
								 * Message: motd
								 * Officer Message: xxx
								 * Alliance Message: xxx
								 * Claimed Keep: xxx
								 */
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InfoGuild", client.Player.Guild.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InfoRPBPMP", client.Player.Guild.RealmPoints, client.Player.Guild.BountyPoints, client.Player.Guild.MeritPoints), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InfoGuildLevel", client.Player.Guild.GuildLevel), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InfoGDuesBank", client.Player.Guild.GetGuildDuesPercent().ToString() + "%", MoneyMgr.GetString(long.Parse(client.Player.Guild.GetGuildBank().ToString()))), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

								client.Out.SendMessage(string.Format("Current Merit Bonus: {0}", GuildUtil.BonusTypeToName(client.Player.Guild.BonusType)), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

								if (client.Player.Guild.GuildBanner)
								{
									client.Out.SendMessage("Banner: " + client.Player.Guild.GuildBannerStatus(client.Player), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								}
								else if (client.Player.Guild.GuildLevel >= 7)
								{
									TimeSpan lostTime = DateTime.Now.Subtract(client.Player.Guild.GuildBannerLostTime);

									if (lostTime.TotalMinutes < ServerProperty.GUILD_BANNER_LOST_TIME)
									{
										client.Out.SendMessage("Banner lost to the enemy", EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
									}
									else
									{
										client.Out.SendMessage("Banner available for purchase", EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
									}
								}

								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InfoWebpage", client.Player.Guild.Webpage), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InfoCEmail", client.Player.Guild.Email), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

								string motd = client.Player.Guild.Motd;
								if (!string.IsNullOrEmpty(motd) && client.Player.GuildRank.GcHear)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InfoMotd", motd), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								}

								string omotd = client.Player.Guild.Omotd;
								if (!string.IsNullOrEmpty(omotd) && client.Player.GuildRank.OcHear)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InfoOMotd", omotd), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								}

								if (client.Player.Guild.alliance != null)
								{
									string amotd = client.Player.Guild.alliance.Dballiance.Motd;
									if (!string.IsNullOrEmpty(amotd) && client.Player.GuildRank.AcHear)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InfoaMotd", amotd), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
									}
								}
								if (client.Player.Guild.ClaimedKeeps.Count > 0)
								{
									foreach (AGameKeep keep in client.Player.Guild.ClaimedKeeps)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Keep", keep.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
									}
								}
							}
							else
							{
								switch (args[2])
								{
									case "1": // show guild info
										{
											if (client.Player.Guild == null)
												return;

											int housenum;
											if (client.Player.Guild.GuildOwnsHouse)
											{
												housenum = client.Player.Guild.GuildHouseNumber;
											}
											else
												housenum = 0;

											string mes = "I";
											mes += ',' + client.Player.Guild.GuildLevel.ToString(); // Guild Level
											mes += ',' + client.Player.Guild.GetGuildBank().ToString(); // Guild Bank money
											mes += ',' + client.Player.Guild.GetGuildDuesPercent().ToString(); // Guild Dues enable/disable
											mes += ',' + client.Player.Guild.BountyPoints.ToString(); // Guild Bounty
											mes += ',' + client.Player.Guild.RealmPoints.ToString(); // Guild Experience
											mes += ',' + client.Player.Guild.MeritPoints.ToString(); // Guild Merit Points
											mes += ',' + housenum.ToString(); // Guild houseLot ?
											mes += ',' + (client.Player.Guild.MemberOnlineCount + 1).ToString(); // online Guild member ?
											mes += ',' + client.Player.Guild.GuildBannerStatus(client.Player); //"Banner available for purchase", "Missing banner buying permissions"
											mes += ",\"" + client.Player.Guild.Motd + '\"'; // Guild Motd
											mes += ",\"" + client.Player.Guild.Omotd + '\"'; // Guild oMotd
											client.Out.SendMessage(mes, EChatType.CT_SocialInterface, EChatLoc.CL_SystemWindow);
											break;
										}
									case "2": //enable/disable social windows
										{
											// "P,ShowGuildWindow,ShowAllianceWindow,?,ShowLFGuildWindow(only with guild),0,0" // news and friend windows always showed
											client.Out.SendMessage("P," + (client.Player.Guild == null ? "0" : "1") + (client.Player.Guild.AllianceId != string.Empty ? "0" : "1") + ",0,0,0,0", EChatType.CT_SocialInterface, EChatLoc.CL_SystemWindow);
											break;
										}
									default:
										break;
								}
							}

							SendSocialWindowData(client, 1, 1, 2);
							break;
						}
						#endregion
						#region Buybanner
					case "buybanner":
						{
							if (client.Player.Guild.GuildLevel < 7)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.GuildLevelReq"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							long bannerPrice = (client.Player.Guild.GuildLevel * 100);

							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.GuildBanner)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerAlready"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							TimeSpan lostTime = DateTime.Now.Subtract(client.Player.Guild.GuildBannerLostTime);

							if (lostTime.TotalMinutes < ServerProperty.GUILD_BANNER_LOST_TIME)
							{
								int hoursLeft = (int)((ServerProperty.GUILD_BANNER_LOST_TIME - lostTime.TotalMinutes + 30) / 60);
								if (hoursLeft < 2)
								{
									int minutesLeft = (int)(ServerProperty.GUILD_BANNER_LOST_TIME - lostTime.TotalMinutes + 1);
									client.Out.SendMessage("Your guild banner was lost to the enemy. You must wait " + minutesLeft + " minutes before you can purchase another one.", EChatType.CT_Guild, EChatLoc.CL_ChatWindow);
								}
								else
								{
									client.Out.SendMessage("Your guild banner was lost to the enemy. You must wait " + hoursLeft + " hours before you can purchase another one.", EChatType.CT_Guild, EChatLoc.CL_ChatWindow);
								}
								return;
							}


							client.Player.Guild.UpdateGuildWindow();

							if (client.Player.Guild.BountyPoints > bannerPrice || client.Account.PrivLevel > (int)EPrivLevel.Player)
							{
								client.Out.SendCustomDialog("Are you sure you buy a guild banner for " + bannerPrice + " guild bounty points? ", ConfirmBannerBuy);
								client.Player.TempProperties.SetProperty(GUILD_BANNER_PRICE, bannerPrice);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNotAfford"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							break;
						}
						#endregion
						#region Summon
					case "summon":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.GuildBanner)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNone"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Group == null && client.Account.PrivLevel == (int)EPrivLevel.Player)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNoGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							foreach (GamePlayer guildPlayer in client.Player.Guild.GetListOfOnlineMembers())
							{
								if (guildPlayer.GuildBanner != null)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerGuildSummoned"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
									return;
								}
							}

							if (client.Player.Group != null)
							{
								foreach (GamePlayer groupPlayer in client.Player.Group.GetPlayersInTheGroup())
								{
									if (groupPlayer.GuildBanner != null)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerGroupSummoned"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
										return;
									}
								}
							}

							if (client.Player.CurrentRegion.IsRvR)
							{
								GuildBannerUtil banner = new GuildBannerUtil(client.Player);
								banner.Start();
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerSummoned"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								client.Player.Guild.SendMessageToGuildMembers(string.Format("{0} has summoned the guild banner!", client.Player.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								client.Player.Guild.UpdateGuildWindow();
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNotRvR"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							break;
						}
						#endregion
						#region Buff
						// --------------------------------------------------------------------------------
						// GUILD BUFF
						// --------------------------------------------------------------------------------
					case "buff":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader) && !client.Player.Guild.HasRank(client.Player, EGuildRank.Buff))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (client.Player.Guild.MeritPoints < 1000)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.MeritPointReq"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (client.Player.Guild.BonusType == EGuildBonusType.None && args.Length > 2)
							{
								if (args[2] == "rps")
								{
									if (ServerProperty.GUILD_BUFF_RP > 0)
									{
										client.Player.TempProperties.SetProperty(GUILD_BUFF_TYPE, EGuildBonusType.RealmPoints);
										client.Out.SendCustomDialog("Are you sure you want to activate a guild RP buff for 1000 merit points?", ConfirmBuffBuy);
									}
									else
									{
										client.Out.SendMessage("This buff type is not available.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									}
									return;
								}
								else if (args[2] == "bps")
								{
									if (ServerProperty.GUILD_BUFF_BP > 0)
									{
										client.Player.TempProperties.SetProperty(GUILD_BUFF_TYPE, EGuildBonusType.BountyPoints);
										client.Out.SendCustomDialog("Are you sure you want to activate a guild BP buff for 1000 merit points?", ConfirmBuffBuy);
									}
									else
									{
										client.Out.SendMessage("This buff type is not available.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									}
									return;
								}
								else if (args[2] == "crafting")
								{
									if (ServerProperty.GUILD_BUFF_CRAFTING > 0)
									{
										client.Player.TempProperties.SetProperty(GUILD_BUFF_TYPE, EGuildBonusType.CraftingHaste);
										client.Out.SendCustomDialog("Are you sure you want to activate a guild Crafting Haste buff for 1000 merit points?", ConfirmBuffBuy);
									}
									else
									{
										client.Out.SendMessage("This buff type is not available.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									}
									return;
								}
								else if (args[2] == "xp")
								{
									if (ServerProperty.GUILD_BUFF_XP > 0)
									{
										client.Player.TempProperties.SetProperty(GUILD_BUFF_TYPE, EGuildBonusType.Experience);
										client.Out.SendCustomDialog("Are you sure you want to activate a guild XP buff for 1000 merit points?", ConfirmBuffBuy);
									}
									else
									{
										client.Out.SendMessage("This buff type is not available.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									}
									return;
								}
								else if (args[2] == "artifact")
								{
									if (ServerProperty.GUILD_BUFF_ARTIFACT_XP > 0)
									{
										client.Player.TempProperties.SetProperty(GUILD_BUFF_TYPE, EGuildBonusType.ArtifactXP);
										client.Out.SendCustomDialog("Are you sure you want to activate a guild Artifact XP buff for 1000 merit points?", ConfirmBuffBuy);
									}
									else
									{
										client.Out.SendMessage("This buff type is not available.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									}
									return;
								}
								else if (args[2] == "mlxp")
								{
									if (ServerProperty.GUILD_BUFF_MASTERLEVEL_XP > 0)
									{
										client.Out.SendMessage("This buff type has not been implemented.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
										return;

										//client.Player.TempProperties.setProperty(GUILD_BUFF_TYPE, Guild.eBonusType.MasterLevelXP);
										//client.Out.SendCustomDialog("Are you sure you want to activate a guild Masterlevel XP buff for 1000 merit points?", ConfirmBuffBuy);
									}
									else
									{
										client.Out.SendMessage("This buff type is not available.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									}

									return;
								}
								else
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildBuff"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
									return;
								}
							}
							else
							{
								if (client.Player.Guild.BonusType == EGuildBonusType.None)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildBuff"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								}
								else
								{
									switch (client.Player.Guild.BonusType)
									{
										case EGuildBonusType.Experience:
											client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.XPBuffActive"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
											break;
										case EGuildBonusType.RealmPoints:
											client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RPBuffActive"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
											break;
										case EGuildBonusType.BountyPoints:
											client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BPBuffActive"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
											break;
										case EGuildBonusType.CraftingHaste:
											client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.CraftBuffActive"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
											break;
									}
									//client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.ActiveBuff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								}
							}

							if(client.Player.Guild.BonusType == EGuildBonusType.None)
								client.Out.SendMessage("Available buffs:", EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

							//if (ServerProperties.Properties.GUILD_BUFF_ARTIFACT_XP > 0)
							//	client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.ArtifactXP), ServerProperties.Properties.GUILD_BUFF_ARTIFACT_XP), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

							if (ServerProperty.GUILD_BUFF_BP > 0 && client.Player.Guild.BonusType == EGuildBonusType.None)
								client.Out.SendMessage(string.Format("{0}: {1}%", GuildUtil.BonusTypeToName(EGuildBonusType.BountyPoints), ServerProperty.GUILD_BUFF_BP), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

							if (ServerProperty.GUILD_BUFF_CRAFTING > 0 && client.Player.Guild.BonusType == EGuildBonusType.None)
								client.Out.SendMessage(string.Format("{0}: {1}%", GuildUtil.BonusTypeToName(EGuildBonusType.CraftingHaste), ServerProperty.GUILD_BUFF_CRAFTING), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

							if (ServerProperty.GUILD_BUFF_XP > 0 && client.Player.Guild.BonusType == EGuildBonusType.None)
								client.Out.SendMessage(string.Format("{0}: {1}%", GuildUtil.BonusTypeToName(EGuildBonusType.Experience), ServerProperty.GUILD_BUFF_XP), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

							//if (ServerProperties.Properties.GUILD_BUFF_MASTERLEVEL_XP > 0)
							//    client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.MasterLevelXP), ServerProperties.Properties.GUILD_BUFF_MASTERLEVEL_XP), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

							if (ServerProperty.GUILD_BUFF_RP > 0 && client.Player.Guild.BonusType == EGuildBonusType.None)
								client.Out.SendMessage(string.Format("{0}: {1}%", GuildUtil.BonusTypeToName(EGuildBonusType.RealmPoints), ServerProperty.GUILD_BUFF_RP), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

							return;
						}
						#endregion
						#region Unsummon
					case "unsummon":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.GuildBanner)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNone"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Group == null && client.Account.PrivLevel == (int)EPrivLevel.Player)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNoGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.InCombat)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InCombat"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							foreach (GamePlayer player in client.Player.Guild.GetListOfOnlineMembers())
							{
								if (client.Player.Name == player.Name && player.GuildBanner != null && player.GuildBanner.BannerItem.Status == GuildBannerItem.eStatus.Active)
								{
									client.Player.GuildBanner.Stop();
									client.Player.GuildBanner = null;
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerUnsummoned"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									client.Player.Guild.SendMessageToGuildMembers(string.Format("{0} has put away the guild banner!", client.Player.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
									client.Player.Guild.UpdateGuildWindow();
									break;
								}

								client.Out.SendMessage("You aren't carrying a banner!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							break;
						}
						#endregion
						#region Ranks
					case "ranks":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							client.Player.Guild.UpdateGuildWindow();
							if (!client.Player.GuildRank.GcHear)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							List<DbGuildRank> rankList = client.Player.Guild.Ranks.ToList();
							foreach (DbGuildRank rank in rankList.OrderBy(rank => rank.RankLevel))
							{

								client.Out.SendMessage("RANK: " + rank.RankLevel.ToString() + " NAME: " + rank.Title,
									EChatType.CT_System, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(
									"AcHear: " + (rank.AcHear ? "y" : "n") + " AcSpeak: " + (rank.AcSpeak ? "y" : "n"),
									EChatType.CT_System, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(
									"OcHear: " + (rank.OcHear ? "y" : "n") + " OcSpeak: " + (rank.OcSpeak ? "y" : "n"),
									EChatType.CT_System, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(
									"GcHear: " + (rank.GcHear ? "y" : "n") + " GcSpeak: " + (rank.GcSpeak ? "y" : "n"),
									EChatType.CT_System, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(
									"Emblem: " + (rank.Emblem ? "y" : "n") + " Promote: " + (rank.Promote ? "y" : "n"),
									EChatType.CT_System, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(
									"Remove: " + (rank.Remove ? "y" : "n") + " View: " + (rank.View ? "y" : "n"),
									EChatType.CT_System, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(
									"Dues: " + (rank.Dues ? "y" : "n") + " Withdraw: " + (rank.Withdraw ? "y" : "n"),
									EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}

							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Webpage
					case "webpage":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							client.Player.Guild.UpdateGuildWindow();
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							message = String.Join(" ", args, 2, args.Length - 2);
							client.Player.Guild.Webpage = message;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.WebpageSet", client.Player.Guild.Webpage), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Email
					case "email":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							client.Player.Guild.UpdateGuildWindow();
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							message = String.Join(" ", args, 2, args.Length - 2);
							client.Player.Guild.Email = message;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.EmailSet", client.Player.Guild.Email), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region List
						// --------------------------------------------------------------------------------
						// LIST
						// --------------------------------------------------------------------------------
					case "list":
						{
							// Changing this to list online only, not sure if this is live like or not but list can be huge
							// and spam client.  - Tolakram
							List<GuildUtil> guildList = GuildMgr.GetAllGuilds();
							foreach (GuildUtil guild in guildList)
							{
								if (guild.MemberOnlineCount > 0)
								{
									string mesg = guild.Name + "  " + guild.MemberOnlineCount + " members ";
									client.Out.SendMessage(mesg, EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								}
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Edit
						// --------------------------------------------------------------------------------
						// EDIT
						// --------------------------------------------------------------------------------
					case "edit":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							client.Player.Guild.UpdateGuildWindow();
							GCEditCommand(client, args);
						}
						client.Player.Guild.UpdateGuildWindow();
						break;
						#endregion
						#region Form
						// --------------------------------------------------------------------------------
						// FORM
						// --------------------------------------------------------------------------------
					case "form":
						{
							GroupUtil group = client.Player.Group;
							if (args.Length < 3)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildForm"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							#region Near Registrar
							if (!IsNearRegistrar(client.Player))
							{
								client.Out.SendMessage("You must be near a guild registrar to use this command!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region No group Check
							if (group == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.FormNoGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Groupleader Check
							if (group != null && client.Player != client.Player.Group.Leader)
							{
								client.Out.SendMessage("Only the group leader can create a guild", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Enough members to form Check
							if (group.MemberCount < ServerProperty.GUILD_NUM)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.FormNoMembers" + ServerProperty.GUILD_NUM), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Player already in guild check and Cross Realm Check

							foreach (GamePlayer ply in group.GetPlayersInTheGroup())
							{
								if (ply.Guild != null)
								{
									client.Player.Group.SendMessageToGroupMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AlreadyInGuildName", ply.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									return;
								}
								if (ply.Realm != client.Player.Realm && ServerProperty.ALLOW_CROSS_REALM_GUILDS == false)
								{
									client.Out.SendMessage("All group members must be of the same realm in order to create a guild.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									return;
								}
							}
							#endregion
							#region Guild Length Naming Checks
							//Check length of guild name.
							string guildname = String.Join(" ", args, 2, args.Length - 2);
							if (guildname.Length > 30)
							{
								client.Out.SendMessage("Sorry, your guild name is too long.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Valid Characters Check
							if (!IsValidGuildName(guildname))
							{
								// Mannen doesn't know the live server message, so someone needs to enter it . ;-)
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InvalidLetters"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Guild Exist Checks
							if (GuildMgr.DoesGuildExist(guildname))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.GuildExists"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Enoguh money to form Check
							if (client.Player.Group.Leader.GetCurrentMoney() < GuildFormCost)
							{
								client.Out.SendMessage("It cost 1 gold piece to create a guild", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							#endregion


							client.Player.Group.Leader.TempProperties.SetProperty("Guild_Name", guildname);
							if (GuildFormCheck(client.Player))
							{
								client.Player.Group.Leader.TempProperties.SetProperty("Guild_Consider", true);
								foreach (GamePlayer p in group.GetPlayersInTheGroup().Where(p => p != @group.Leader))
								{
									p.Out.SendCustomDialog(string.Format("Do you wish to create the guild {0} with {1} as Guild Leader", guildname, client.Player.Name), new CustomDialogResponse(CreateGuild));
								}
							}
						}
						break;
					#endregion
						#region Quit
						// --------------------------------------------------------------------------------
						// QUIT
						// --------------------------------------------------------------------------------
					case "quit":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							client.Out.SendGuildLeaveCommand(client.Player, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.ConfirmLeave", client.Player.Guild.Name));
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Promote
						// --------------------------------------------------------------------------------
						// PROMOTE
						// /gc promote [name] <rank#>' to promote player to a superior rank
						// --------------------------------------------------------------------------------
					case "promote":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Promote))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (args.Length < 3)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildPromote"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							object obj = null;
							string playerName = string.Empty;
							bool useDB = false;

							if (args.Length >= 4)
							{
								playerName = args[2];
							}

							if (playerName == string.Empty)
							{
								obj = client.Player.TargetObject as GamePlayer;
							}
							else
							{
								obj = ClientService.GetPlayerByExactName(playerName);
								if (obj == null)
								{
									obj = CoreDb<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(playerName));
									useDB = true;
								}
							}

							if (obj == null)
							{
								if (useDB)
								{
									client.Out.SendMessage("No player with that name can be found!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								}
								else if (playerName == string.Empty)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								}
								else
								{
									client.Out.SendMessage("You need to target a player or provide a player name!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildPromote"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								}
								return;
							}
							//First Check Routines, GuildIDControl search for player or character.
							string guildId = "";
							string plyName = "";
							ushort currentTargetGuildRank = 9;
							GamePlayer ply = obj as GamePlayer;
							DbCoreCharacter ch = obj as DbCoreCharacter;

							if (ply != null)
							{
								plyName = ply.Name;
								guildId = ply.GuildID;
								currentTargetGuildRank = ply.GuildRank.RankLevel;
							}
							else if (ch != null)
							{
								plyName = ch.Name;
								guildId = ch.GuildID;
								currentTargetGuildRank = ch.GuildRank;
							}
							else
							{
								client.Out.SendMessage("Error during promotion, player not found!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (guildId != client.Player.GuildID)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotInYourGuild"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							//Second Check, Autorisation Checks, a player can promote another to it's own RealmRank or above only if: newrank(rank to be applied) >= commandUserGuildRank(usercommandRealmRank)

							ushort commandUserGuildRank = client.Player.GuildRank.RankLevel;
							ushort newrank;
							try
							{
								newrank = Convert.ToUInt16(args[3]);

								if (newrank > 9)
								{
									client.Out.SendMessage("Error changing to new rank! Realm Rank have to be set to 0-9.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									return;
								}
							}
							catch
							{
								client.Out.SendMessage("Error changing to new rank!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildPromote"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								return;
							}
							//if (commandUserGuildRank != 0 && (newrank < commandUserGuildRank || newrank < 0)) // Do we have to authorize Self Retrograde for GuildMaster?
							if ((newrank < commandUserGuildRank) || (newrank < 0))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromoteHigherThanPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (newrank > currentTargetGuildRank && commandUserGuildRank != 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromoteHaveToUseDemote"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (obj is GamePlayer)
							{
								ply.GuildRank = client.Player.Guild.GetRankByID(newrank);
								ply.SaveIntoDatabase();
								currentTargetGuildRank = ply.GuildRank.RankLevel;
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromotedSelf", plyName, newrank.ToString(), currentTargetGuildRank.ToString()), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
								client.Player.Guild.SendMessageToGuildMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromotedOther", client.Player.Name, plyName, newrank.ToString(), currentTargetGuildRank.ToString()), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							}
							else
							{
								ch.GuildRank = newrank;
								GameServer.Database.SaveObject(ch);
								GameServer.Database.FillObjectRelations(ch);
								currentTargetGuildRank = ch.GuildRank;
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromotedSelf", plyName, newrank.ToString(), currentTargetGuildRank.ToString()), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
								client.Player.Guild.SendMessageToGuildMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromotedOther", client.Player.Name, plyName, newrank.ToString(), currentTargetGuildRank.ToString()), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
					#endregion
						#region Demote
						// --------------------------------------------------------------------------------
						// DEMOTE
						// --------------------------------------------------------------------------------
						case "demote":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.NotMember"), EChatType.CT_System,
									EChatLoc.CL_SystemWindow);
								return;
							}

							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Demote))
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System,
									EChatLoc.CL_SystemWindow);
								return;
							}

							if (args.Length < 3)
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.Help.GuildDemote"), EChatType.CT_System,
									EChatLoc.CL_SystemWindow);
								return;
							}


							object obj = null;
							string playername = string.Empty;
							bool useDB = false;
							
							if (args.Length >= 4)
							{
								playername = args[2];
							}

							if (playername == string.Empty)
							{
								obj = client.Player.TargetObject as GamePlayer;
							}
							else
							{
								obj = ClientService.GetPlayerByExactName(playername);
								if (obj == null)
								{
									obj = CoreDb<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(playername));
									useDB = true;
								}
							}
							if (obj == null)
							{
								if (useDB)
								{
									client.Out.SendMessage("No player with that name can be found!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								}
								else if (playername == string.Empty)
								{
									client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.NoPlayerSelected"), EChatType.CT_System,
									EChatLoc.CL_SystemWindow);
								}
								else
								{
									client.Out.SendMessage("You need to target a player or provide a player name!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDemote"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								}
								return;
							}

							string guildId = "";
							ushort guildRank = 1;
							string plyName = "";
							GamePlayer ply = obj as GamePlayer;
							DbCoreCharacter ch = obj as DbCoreCharacter;
							if (obj is GamePlayer)
							{
								plyName = ply.Name;
								guildId = ply.GuildID;
								if (ply.GuildRank != null)
									guildRank = ply.GuildRank.RankLevel;
							}
							else
							{
								plyName = ch.Name;
								guildId = ch.GuildID;
								guildRank = ch.GuildRank;
							}

							if (guildId != client.Player.GuildID)
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.NotInYourGuild"), EChatType.CT_System,
									EChatLoc.CL_SystemWindow);
								return;
							}

							try
							{
								ushort newrank = Convert.ToUInt16(args[3]);
								if (newrank < guildRank || newrank > 10)
								{
									client.Out.SendMessage(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedHigherThanPlayer"), EChatType.CT_System,
										EChatLoc.CL_SystemWindow);
									return;
								}

								if (obj is GamePlayer)
								{
									ply.GuildRank = client.Player.Guild.GetRankByID(newrank);
									ply.SaveIntoDatabase();
									guildRank = ply.GuildRank.RankLevel;
									client.Out.SendMessage(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedSelf", plyName, newrank.ToString(),
											guildRank.ToString()), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
									client.Player.Guild.SendMessageToGuildMembers(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedOther", client.Player.Name, plyName,
											newrank.ToString(), guildRank.ToString()), EChatType.CT_Important,
										EChatLoc.CL_SystemWindow);
								}
								else
								{
									ch.GuildRank = newrank;
									GameServer.Database.SaveObject(ch);
									guildRank = ch.GuildRank;
									client.Out.SendMessage(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedSelf", plyName, newrank.ToString(),
											guildRank.ToString()), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
									client.Player.Guild.SendMessageToGuildMembers(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedOther", client.Player.Name, plyName,
											newrank.ToString(), guildRank.ToString()), EChatType.CT_Important,
										EChatLoc.CL_SystemWindow);
								}
							}
							catch
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.InvalidRank"), EChatType.CT_System,
									EChatLoc.CL_SystemWindow);
							}

							client.Player.Guild.UpdateGuildWindow();
				}
						break;
						#endregion
						#region Who
						// --------------------------------------------------------------------------------
						// WHO
						// --------------------------------------------------------------------------------
					case "who":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							int ind = 0;
							int startInd = 0;

							#region Social Window
							if (args.Length == 6 && args[2] == "window")
							{
								int sortTemp;
								byte showTemp;
								int page;

								//Lets get the variables that were sent over
								if (Int32.TryParse(args[3], out sortTemp) && Int32.TryParse(args[4], out page) && Byte.TryParse(args[5], out showTemp) && sortTemp >= -7 && sortTemp <= 7)
								{
									SendSocialWindowData(client, sortTemp, page, showTemp);
								}
								return;
							}
							#endregion

							#region Alliance Who
							else if (args.Length == 3)
							{
								if (args[2] == "alliance" || args[2] == "a")
								{
									foreach (GuildUtil guild in client.Player.Guild.alliance.Guilds)
									{
										lock (guild.GetListOfOnlineMembers())
										{
											foreach (GamePlayer ply in guild.GetListOfOnlineMembers())
											{
												if (ply.Client.IsPlaying && !ply.IsAnonymous)
												{
													ind++;
													string zoneName = (ply.CurrentZone == null ? "(null)" : ply.CurrentZone.Description);
													string mesg = ind + ") " + ply.Name + " <guild=" + guild.Name + "> the Level " + ply.Level + " " + ply.PlayerClass.Name + " in " + zoneName;
													client.Out.SendMessage(mesg, EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
												}
											}
										}
									}
									return;
								}
								else
								{
									int.TryParse(args[2], out startInd);
								}
							}
							#endregion

							#region Who
							IList<GamePlayer> onlineGuildMembers = client.Player.Guild.GetListOfOnlineMembers();

							foreach (GamePlayer ply in onlineGuildMembers)
							{
								if (ply.Client.IsPlaying && !ply.IsAnonymous)
								{
									if (startInd + ind > startInd + WhoCommand.MAX_LIST_SIZE)
										break;
									ind++;
									string zoneName = (ply.CurrentZone == null ? "(null)" : ply.CurrentZone.Description);
									string mesg;
									if (ply.GuildRank.Title != null)
										mesg = ind.ToString() + ") " + ply.Name + " <" + ply.GuildRank.Title + "> the Level " + ply.Level.ToString() + " " + ply.PlayerClass.Name + " in " + zoneName;
									else
										mesg = ind.ToString() + ") " + ply.Name + " <" + ply.GuildRank.RankLevel.ToString() + "> the Level " + ply.Level.ToString() + " " + ply.PlayerClass.Name + " in " + zoneName;
									if (ServerProperty.ALLOW_CHANGE_LANGUAGE)
										mesg += " <" + ply.Client.Account.Language + ">";
									if (ind >= startInd)
										client.Out.SendMessage(mesg, EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								}
							}
							if (ind > WhoCommand.MAX_LIST_SIZE && ind < onlineGuildMembers.Count)
								client.Out.SendMessage(string.Format(WhoCommand.MESSAGE_LIST_TRUNCATED, onlineGuildMembers.Count), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							else client.Out.SendMessage("total member online:        " + ind.ToString(), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

							break;
							#endregion
						}
						#endregion
						#region Leader
						// --------------------------------------------------------------------------------
						// LEADER
						// --------------------------------------------------------------------------------
					case "leader":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer newLeader = client.Player.TargetObject as GamePlayer;
							if (args.Length > 2)
							{
								GamePlayer player = ClientService.GetPlayerByExactName(args[2]);
								if (player != null && GameServer.ServerRules.IsAllowedToGroup(client.Player, player, true))
									newLeader = player;
							}
							if (newLeader == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (newLeader.Guild != client.Player.Guild)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotInYourGuild"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							newLeader.GuildRank = newLeader.Guild.GetRankByID(0);
							newLeader.SaveIntoDatabase();
							newLeader.Out.SendMessage(LanguageMgr.GetTranslation(newLeader.Client, "Scripts.Player.Guild.MadeLeader", newLeader.Guild.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							foreach (GamePlayer ply in client.Player.Guild.GetListOfOnlineMembers())
							{
								ply.Out.SendMessage(LanguageMgr.GetTranslation(ply.Client, "Scripts.Player.Guild.MadeLeaderOther", newLeader.Name, newLeader.Guild.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Emblem
						// --------------------------------------------------------------------------------
						// EMBLEM
						// --------------------------------------------------------------------------------
					case "emblem":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.Emblem != 0)
							{
								if (client.Player.TargetObject is GuildEmblemeer == false)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.EmblemAlready"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									return;
								}
								client.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.EmblemRedo"), new CustomDialogResponse(EmblemChange));
								return;
							}
							if (client.Player.TargetObject is GuildEmblemeer == false)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.EmblemNPCNotSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							client.Out.SendEmblemDialogue();

							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Autoremove
					case "autoremove":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Remove))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (args.Length == 4 && args[3].ToLower() == "account")
							{
								//#warning how can player name  !=  account if args[3] = account ?
								string playername = args[3];
								string accountId = "";

								GamePlayer targetPlayer = ClientService.GetPlayerByPartialName(args[3], out _);
								if (targetPlayer != null)
								{
									OnCommand(client, new string[] { "gc", "remove", args[3] });
									accountId = targetPlayer.Client.Account.Name;
								}
								else
								{
									DbCoreCharacter c = CoreDb<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(playername));

									if (c == null)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PlayerNotFound"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
										return;
									}

									accountId = c.AccountName;
								}
								List<DbCoreCharacter> chars = new List<DbCoreCharacter>();
								chars.AddRange(CoreDb<DbCoreCharacter>.SelectObjects(DB.Column("AccountName").IsEqualTo(accountId)));
								//chars.AddRange((Character[])DOLDB<CharacterArchive>.SelectObjects("AccountID = '" + accountId + "'"));

								foreach (DbCoreCharacter ply in chars)
								{
									ply.GuildID = "";
									ply.GuildRank = 0;
								}
								GameServer.Database.SaveObject(chars);
								break;
							}
							else if (args.Length == 3)
							{
								GamePlayer targetPlayer = ClientService.GetPlayerByPartialName(args[2], out _);
								if (targetPlayer != null)
								{
									OnCommand(client, new string[] { "gc", "remove", args[2] });
									return;
								}
								else
								{
									var c = CoreDb<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(args[2]));
									if (c == null)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PlayerNotFound"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
										return;
									}
									if (c.GuildID != client.Player.GuildID)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotInYourGuild"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
										return;
									}
									else
									{
										c.GuildID = "";
										c.GuildRank = 0;
										GameServer.Database.SaveObject(c);
									}
								}
								break;
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAutoRemoveAcc"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAutoRemove"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region MOTD
						// --------------------------------------------------------------------------------
						// MOTD
						// --------------------------------------------------------------------------------
					case "motd":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							message = String.Join(" ", args, 2, args.Length - 2);
							client.Player.Guild.Motd = message;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.MotdSet"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region AMOTD
						// --------------------------------------------------------------------------------
						// AMOTD
						// --------------------------------------------------------------------------------
					case "amotd":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.AllianceId == string.Empty)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							message = String.Join(" ", args, 2, args.Length - 2);
							client.Player.Guild.alliance.Dballiance.Motd = message;
							GameServer.Database.SaveObject(client.Player.Guild.alliance.Dballiance);
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AMotdSet"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region OMOTD
						// --------------------------------------------------------------------------------
						// OMOTD
						// --------------------------------------------------------------------------------
					case "omotd":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							message = String.Join(" ", args, 2, args.Length - 2);
							client.Player.Guild.Omotd = message;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.OMotdSet"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Alliance
						// --------------------------------------------------------------------------------
						// ALLIANCE
						// --------------------------------------------------------------------------------
					case "alliance":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							AllianceUtil alliance = null;
							if (client.Player.Guild.AllianceId != null && client.Player.Guild.AllianceId != string.Empty)
							{
								alliance = client.Player.Guild.alliance;
							}
							else
							{
								DisplayMessage(client, "Your guild is not a member of an alliance!");
								return;
							}

							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceInfo", alliance.Dballiance.AllianceName));
							DbGuild leader = alliance.Dballiance.DBguildleader;
							if (leader != null)
								DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceLeader", leader.GuildName));
							else
								DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNoLeader"));

							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMembers"));
							int i = 0;
							foreach (DbGuild guild in alliance.Dballiance.DBguilds)
								if (guild != null)
									DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMember", i++, guild.GuildName));
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Alliance Invite
						// --------------------------------------------------------------------------------
						// AINVITE
						// --------------------------------------------------------------------------------
					case "ainvite":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer obj = client.Player.TargetObject as GamePlayer;
							if (obj == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (obj.GuildRank.RankLevel != 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNoGMSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (obj.Guild.alliance != null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceAlreadyOther"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (ServerProperty.ALLIANCE_MAX == 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceDisabled"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (ServerProperty.ALLIANCE_MAX != -1)
							{
								if (client.Player.Guild.alliance != null)
								{
									if (client.Player.Guild.alliance.Guilds.Count + 1 > ServerProperty.ALLIANCE_MAX)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMax"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
										return;
									}
								}
							}
							obj.TempProperties.SetProperty("allianceinvite", client.Player); //finish that
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceInvite"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							obj.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceInvited", client.Player.Guild.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						case "aleader":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer obj = client.Player.TargetObject as GamePlayer;
							if (obj == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (obj.GuildRank.RankLevel != 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNoGMSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (obj.Guild.alliance != client.Player.Guild.alliance)
							{
								client.Out.SendMessage("You're not in the same alliance", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.alliance.Dballiance.LeaderGuildID != client.Player.Guild.GuildID)
							{
								client.Out.SendMessage("You're not the leader of the alliance", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
								return;
							}
							
							client.Player.Guild.alliance.Dballiance.AllianceName = obj.Guild.Name;
							client.Player.Guild.alliance.Dballiance.LeaderGuildID = obj.Guild.GuildID;
							GameServer.Database.SaveObject(client.Player.Guild.alliance.Dballiance);
							client.Player.Guild.alliance.SendMessageToAllianceMembers(obj.Guild.Name + " is the new leader of the alliance", EChatType.CT_Alliance, EChatLoc.CL_SystemWindow);
							
							// client.Player.Guild.alliance.PromoteGuild(obj.Guild);

							break;
						}
						
						#endregion
						#region Alliance Invite Accept
						// --------------------------------------------------------------------------------
						// AINVITE
						// --------------------------------------------------------------------------------
					case "aaccept":
						{
							AllianceInvite(client.Player, 0x01);
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Alliance Invite Cancel
						// --------------------------------------------------------------------------------
						// ACANCEL
						// --------------------------------------------------------------------------------
					case "acancel":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer obj = client.Player.TargetObject as GamePlayer;
							if (obj == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer inviter = client.Player.TempProperties.GetProperty<GamePlayer>("allianceinvite", null);
							if (inviter == client.Player)
								obj.TempProperties.RemoveProperty("allianceinvite");
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceAnsCancel"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							obj.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceAnsCancel"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							return;
						}
						#endregion
						#region Alliance Invite Decline
						// --------------------------------------------------------------------------------
						// ADECLINE
						// --------------------------------------------------------------------------------
					case "adecline":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer inviter = client.Player.TempProperties.GetProperty<GamePlayer>("allianceinvite", null);
							client.Player.TempProperties.RemoveProperty("allianceinvite");
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceDeclined"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							inviter.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceDeclinedOther"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							return;
						}
						#endregion
						#region Alliance Remove
						// --------------------------------------------------------------------------------
						// AREMOVE
						// --------------------------------------------------------------------------------
					case "aremove":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.alliance == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.GuildID != client.Player.Guild.alliance.Dballiance.DBguildleader.GuildID)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNotLeader"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (args.Length > 3)
							{
								if (args[2] == "alliance")
								{
									try
									{
										int index = Convert.ToInt32(args[3]);
										GuildUtil myguild = (GuildUtil)client.Player.Guild.alliance.Guilds[index];
										if (myguild != null)
											client.Player.Guild.alliance.RemoveGuild(myguild);
									}
									catch
									{
										client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceIndexNotVal"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									}

								}
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildARemove"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildARemoveAlli"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								return;
							}
							else
							{
								GamePlayer obj = client.Player.TargetObject as GamePlayer;
								if (obj == null)
								{
									client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									return;
								}
								if (obj.Guild == null)
								{
									client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMemNotSel"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									return;
								}
								if (obj.Guild.alliance != client.Player.Guild.alliance)
								{
									client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMemNotSel"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									return;
								}
								client.Player.Guild.alliance.RemoveGuild(obj.Guild);
							}
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Alliance Leave
						// --------------------------------------------------------------------------------
						// ALEAVE
						// --------------------------------------------------------------------------------
					case "aleave":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.alliance == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							client.Player.Guild.alliance.RemoveGuild(client.Player.Guild);
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Claim
						// --------------------------------------------------------------------------------
						//ClAIM
						// --------------------------------------------------------------------------------
					case "claim":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							AGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(client.Player.CurrentRegionID, client.Player, WorldMgr.VISIBILITY_DISTANCE);
							if (keep == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.ClaimNotNear"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (keep.CheckForClaim(client.Player))
							{
								keep.Claim(client.Player);
							}
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Release
						// --------------------------------------------------------------------------------
						//RELEASE
						// --------------------------------------------------------------------------------
					case "release":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.ClaimedKeeps.Count == 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoKeep"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Release))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.ClaimedKeeps.Count == 1)
							{
								if (client.Player.Guild.ClaimedKeeps[0].CheckForRelease(client.Player))
								{
									client.Player.Guild.ClaimedKeeps[0].Release();
								}
							}
							else
							{
								foreach (AArea area in client.Player.CurrentAreas)
								{
									if (area is KeepArea && ((KeepArea)area).Keep.Guild == client.Player.Guild)
									{
										if (((KeepArea)area).Keep.CheckForRelease(client.Player))
										{
											((KeepArea)area).Keep.Release();
										}
									}
								}
							}
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Upgrade
						// --------------------------------------------------------------------------------
						//UPGRADE
						// --------------------------------------------------------------------------------
					case "upgrade":
						{
							client.Out.SendMessage("Keep upgrading is currently disabled!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
							/* un-comment this to work on allowing keep upgrading
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.ClaimedKeeps.Count == 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoKeep"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.GotAccess(client.Player, Guild.eGuildRank.Upgrade))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (args.Length != 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.KeepNoLevel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            byte targetlevel = 0;
                            try
                            {
                                targetlevel = Convert.ToByte(args[2]);
                                if (targetlevel > 10 || targetlevel < 1)
                                    return;
                            }
                            catch
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.UpgradeScndArg"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.ClaimedKeeps.Count == 1)
                            {
                                foreach (AbstractGameKeep keep in client.Player.Guild.ClaimedKeeps)
                                    keep.StartChangeLevel(targetlevel);
                            }
                            else
                            {
                                foreach (AbstractArea area in client.Player.CurrentAreas)
                                {
                                    if (area is KeepArea && ((KeepArea)area).Keep.Guild == client.Player.Guild)
                                        ((KeepArea)area).Keep.StartChangeLevel(targetlevel);
                                }
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            return;
							 */
						}
						#endregion
						#region Type
						//TYPE
						// --------------------------------------------------------------------------------
					case "type":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.ClaimedKeeps.Count == 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoKeep"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Upgrade))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							int type = 0;
							try
							{
								type = Convert.ToInt32(args[2]);
								if (type != 1 || type != 2 || type != 4)
									return;
							}
							catch
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.UpgradeScndArg"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								return;
							}
							//client.Player.Guild.ClaimedKeep.Release();
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Noteself
					case "noteself":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							string note = String.Join(" ", args, 2, args.Length - 2);
							client.Player.GuildNote = note;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoteSet", note), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Note
						case "note":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
							{
								client.Out.SendMessage("Use '/gc noteself <note>' to set your own note", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							if (args[2] is null)
							{
								client.Out.SendMessage("You need to specify a target guild member.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							bool noteSet = false;
							foreach (var guildMember in GuildMgr.GetAllGuildMembers(client.Player.GuildID))
							{
								if (guildMember.Value.Name.ToLower() != args[2].ToLower()) continue;
								string note = String.Join(" ", args, 3, args.Length - 3);
								guildMember.Value.Note = note;
								noteSet = true;
								break;
							}
							
							if(!noteSet)
								client.Out.SendMessage("No guild member with that name found.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							else
								client.Out.SendMessage($"Note set correctly for {args[2]}", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							
							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Dues
					case "dues":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Dues))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}
							if (args[2] == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDues"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							long amount = long.Parse(args[2]);
							if (amount == 0)
							{
								client.Player.Guild.SetGuildDues(false);
								client.Player.Guild.SetGuildDuesPercent(0);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DuesOff"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else if (amount > 0 && amount <= 25)
							{
								client.Player.Guild.SetGuildDues(true);
								if (ServerProperty.NEW_GUILD_DUES)
								{
									client.Player.Guild.SetGuildDuesPercent(amount);
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DuesOn", amount), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								}
								else
								{
									client.Player.Guild.SetGuildDuesPercent(2);
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DuesOn", 2), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								}
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDues"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Deposit
					case "deposit":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}

							double amount = double.Parse(args[2]);
							if (amount < 0 || amount > 1000000001)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DepositInvalid"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							else if (client.Player.GetCurrentMoney() < amount)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DepositTooMuch"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Player.Guild.SetGuildBank(client.Player, amount);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Withdraw
					case "withdraw":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Withdraw))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							double amount = double.Parse(args[2]);
							if (amount < 0 || amount > 1000000001)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.WithdrawInvalid"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							else if ((client.Player.Guild.GetGuildBank() - amount) < 0)
							{
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Player.Client, "Scripts.Player.Guild.WithdrawTooMuch"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								return;
							}
							else
							{
								client.Player.Guild.WithdrawGuildBank(client.Player, amount);

							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Logins
					case "logins":
						{
							client.Player.ShowGuildLogins = !client.Player.ShowGuildLogins;

							if (client.Player.ShowGuildLogins)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.LoginsOn"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.LoginsOff"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Default
					default:
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.UnknownCommand", args[1]), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							DisplayHelp(client);
						}
						break;
						#endregion
				}
			}
			catch (Exception e)
			{
				if (ServerProperty.ENABLE_DEBUG)
				{
					log.Debug("Error in /gc script, " + args[1] + " command: " + e.ToString());
				}

				DisplayHelp(client);
			}
		}

		private const string GUILD_BANNER_PRICE = "GUILD_BANNER_PRICE";

		protected void ConfirmBannerBuy(GamePlayer player, byte response)
		{
			if (response != 0x01)
				return;

			long bannerPrice = player.TempProperties.GetProperty<long>(GUILD_BANNER_PRICE, 0);
			player.TempProperties.RemoveProperty(GUILD_BANNER_PRICE);

			if (bannerPrice == 0 || player.Guild.GuildBanner)
				return;

			if (player.Guild.BountyPoints >= bannerPrice || player.Client.Account.PrivLevel > (int)EPrivLevel.Player)
			{
				player.Guild.RemoveBountyPoints(bannerPrice);
				player.Guild.GuildBanner = true;
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.BannerBought", bannerPrice), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			}
			else
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.BannerNotAfford"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

		}


		private const string GUILD_BUFF_TYPE = "GUILD_BUFF_TYPE";

		protected void ConfirmBuffBuy(GamePlayer player, byte response)
		{
			if (response != 0x01)
				return;

			EGuildBonusType buffType = player.TempProperties.GetProperty<EGuildBonusType>(GUILD_BUFF_TYPE, EGuildBonusType.None);
			player.TempProperties.RemoveProperty(GUILD_BUFF_TYPE);

			if (buffType == EGuildBonusType.None || player.Guild.MeritPoints < 1000 || player.Guild.BonusType != EGuildBonusType.None)
				return;

			player.Guild.BonusType = buffType;
			player.Guild.RemoveMeritPoints(1000);
			player.Guild.BonusStartTime = DateTime.Now;

			string buffName = GuildUtil.BonusTypeToName(buffType);

			foreach (GamePlayer ply in player.Guild.GetListOfOnlineMembers())
			{
				ply.Out.SendMessage(LanguageMgr.GetTranslation(ply.Client, "Scripts.Player.Guild.BuffActivated", player.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				ply.Out.SendMessage(string.Format("Your guild now has a bonus to {0} for 24 hours!", buffName), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			}

			player.Guild.UpdateGuildWindow();

		}


		/// <summary>
		/// method to handle the aliance invite
		/// </summary>
		/// <param name="player"></param>
		/// <param name="reponse"></param>
		protected void AllianceInvite(GamePlayer player, byte reponse)
		{
			if (reponse != 0x01)
				return; //declined

			GamePlayer inviter = player.TempProperties.GetProperty<GamePlayer>("allianceinvite", null);

			if (player.Guild == null)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.NotMember"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if (inviter == null || inviter.Guild == null)
			{
				return;
			}

			if (!player.Guild.HasRank(player, EGuildRank.Alli))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			player.TempProperties.RemoveProperty("allianceinvite");

			if (inviter.Guild.alliance == null)
			{
				//create alliance
				AllianceUtil alli = new AllianceUtil();
				DbGuildAlliance dballi = new DbGuildAlliance();
				dballi.AllianceName = inviter.Guild.Name;
				dballi.LeaderGuildID = inviter.GuildID;
				dballi.DBguildleader = null;
				dballi.Motd = "";
				alli.Dballiance = dballi;
				alli.Guilds.Add(inviter.Guild);
				inviter.Guild.alliance = alli;
				inviter.Guild.AllianceId = inviter.Guild.alliance.Dballiance.ObjectId;
			}
			inviter.Guild.alliance.AddGuild(player.Guild);
			inviter.Guild.alliance.SaveIntoDatabase();
			player.Guild.UpdateGuildWindow();
			inviter.Guild.UpdateGuildWindow();
		}

		/// <summary>
		/// method to handle the emblem change
		/// </summary>
		/// <param name="player"></param>
		/// <param name="reponse"></param>
		public static void EmblemChange(GamePlayer player, byte reponse)
		{
			if (reponse != 0x01)
				return;
			if (player.TargetObject is GuildEmblemeer == false)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.EmblemNeedNPC"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (player.GetCurrentMoney() < GuildMgr.COST_RE_EMBLEM) //200 gold to re-emblem
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.EmblemNeedGold"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			player.Out.SendEmblemDialogue();
			player.Guild.UpdateGuildWindow();
		}

		public void DisplayHelp(GameClient client)
		{
			if (client.Account.PrivLevel > 1)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMCommands"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMCreate"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMPurge"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMRename"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMAddPlayer"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMRemovePlayer"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			}
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildUsage"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildForm"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildInfo"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildRanks"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildCancel"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDecline"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildClaim"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildQuit"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildMotd"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAMotd"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildOMotd"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildPromote"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDemote"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildRemove"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildRemAccount"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEmblem"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEdit"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildLeader"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAccept"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildInvite"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildWho"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildList"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAlli"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAAccept"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildACancel"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildADecline"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAInvite"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildARemove"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildARemoveAlli"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildALeader"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildNoteSelf"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDues"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDeposit"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildWithdraw"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildWebpage"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEmail"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildBuff"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildBuyBanner"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildBannerSummon"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// method to handle commands for /gc edit
		/// </summary>
		/// <param name="client"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public int GCEditCommand(GameClient client, string[] args)
		{
			if (args.Length < 4)
			{
				DisplayEditHelp(client);
				return 0;
			}

			bool reponse = true;
			if (args.Length > 4)
			{
				if (args[4].StartsWith("y"))
					reponse = true;
				else if (args[4].StartsWith("n"))
					reponse = false;
				else if (args[3] != "title" && args[3] != "ranklevel")
				{
					DisplayEditHelp(client);
					return 1;
				}
			}
			byte number;
			try
			{
				number = Convert.ToByte(args[2]);
				if (number > 9 || number < 0)
					return 0;
			}
			catch
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.ThirdArgNotNum"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return 0;
			}

			switch (args[3])
			{
				case "title":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						string message = String.Join(" ", args, 4, args.Length - 4);
						client.Player.Guild.GetRankByID(number).Title = message;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankTitleSet", number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						client.Player.Guild.UpdateGuildWindow();
					}
					break;
				case "ranklevel":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						if (args.Length >= 5)
						{
							byte lvl = Convert.ToByte(args[4]);
							client.Player.Guild.GetRankByID(number).RankLevel = lvl;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankLevelSet", lvl.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						else
						{
							DisplayEditHelp(client);
						}
					}
					break;

				case "emblem":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Emblem = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankEmblemSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "gchear":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).GcHear = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankGCHearSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "gcspeak":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}

						client.Player.Guild.GetRankByID(number).GcSpeak = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankGCSpeakSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "ochear":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).OcHear = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankOCHearSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "ocspeak":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).OcSpeak = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankOCSpeakSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "achear":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).AcHear = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankACHearSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "acspeak":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).AcSpeak = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankACSpeakSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "invite":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Invite = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankInviteSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "promote":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Promote = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankPromoteSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "remove":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Remove = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankRemoveSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "alli":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Alli = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankAlliSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "view":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.View))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).View = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankViewSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "buff":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Buff = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankBuffSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "claim":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Claim))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Claim = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankClaimSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "upgrade":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Upgrade))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Upgrade = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankUpgradeSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "release":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Release))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Release = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankReleaseSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "dues":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Dues))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Release = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankDuesSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				case "withdraw":
					{
						if (!client.Player.Guild.HasRank(client.Player, EGuildRank.Withdraw))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Release = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankWithdrawSet", (reponse ? "enabled" : "disabled"), number.ToString()), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
					}
					break;
				default:
					{
						DisplayEditHelp(client);
						return 0;
					}
			} //switch
			DbGuildRank rank = client.Player.Guild.GetRankByID(number);
			if (rank != null)
				GameServer.Database.SaveObject(rank);
			return 1;
		}

		/// <summary>
		/// Send social window data to the client
		/// </summary>
		/// <param name="client"></param>
		/// <param name="sort"></param>
		/// <param name="page"></param>
		/// <param name="offline">0 = false, 1 = true, 2 to try and recall last setting used by player</param>
		private void SendSocialWindowData(GameClient client, int sort, int page, byte offline)
		{
			Dictionary<string, GuildMgr.GuildMemberDisplay> allGuildMembers = GuildMgr.GetAllGuildMembers(client.Player.GuildID);

			if (allGuildMembers == null || allGuildMembers.Count == 0)
			{
				return;
			}

			bool showOffline = false;

			if (offline < 2)
			{
				showOffline = (offline == 0 ? false : true);
			}
			else
			{
				// try to recall last setting
				showOffline = client.Player.TempProperties.GetProperty<bool>("SOCIALSHOWOFFLINE", false);
			}

			client.Player.TempProperties.SetProperty("SOCIALSHOWOFFLINE", showOffline);

			//The type of sorting we will be sending
			ESocialWindowSort sortOrder = (ESocialWindowSort)sort;

			//Let's sort the sorted list - we don't need to sort if sort = name
			SortedList<string, GuildMgr.GuildMemberDisplay> sortedWindowList = null;

			ESocialWindowSortColumn sortColumn = ESocialWindowSortColumn.Name;

			#region Determine Sort
			switch (sortOrder)
			{
				case ESocialWindowSort.ClassAsc:
				case ESocialWindowSort.ClassDesc:
					sortColumn = ESocialWindowSortColumn.ClassID;
					break;
				case ESocialWindowSort.GroupAsc:
				case ESocialWindowSort.GroupDesc:
					sortColumn = ESocialWindowSortColumn.Group;
					break;
				case ESocialWindowSort.LevelAsc:
				case ESocialWindowSort.LevelDesc:
					sortColumn = ESocialWindowSortColumn.Level;
					break;
				case ESocialWindowSort.NoteAsc:
				case ESocialWindowSort.NoteDesc:
					sortColumn = ESocialWindowSortColumn.Note;
					break;
				case ESocialWindowSort.RankAsc:
				case ESocialWindowSort.RankDesc:
					sortColumn = ESocialWindowSortColumn.Rank;
					break;
				case ESocialWindowSort.ZoneOrOnlineAsc:
				case ESocialWindowSort.ZoneOrOnlineDesc:
					sortColumn = ESocialWindowSortColumn.ZoneOrOnline;
					break;
			}
			#endregion

			if (showOffline == false) // show only a sorted list of online players
			{
				IList<GamePlayer> onlineGuildPlayers = client.Player.Guild.GetListOfOnlineMembers();
				sortedWindowList = new SortedList<string, GuildMgr.GuildMemberDisplay>(onlineGuildPlayers.Count);

				foreach (GamePlayer player in onlineGuildPlayers)
				{
					if (allGuildMembers.ContainsKey(player.InternalID))
					{
						GuildMgr.GuildMemberDisplay memberDisplay = allGuildMembers[player.InternalID];
						memberDisplay.UpdateMember(player);
						string key = memberDisplay[sortColumn];

						if (sortedWindowList.ContainsKey(key))
							key += sortedWindowList.Count.ToString();

						sortedWindowList.Add(key, memberDisplay);
					}
				}
			}
			else // sort and display entire list
			{
				sortedWindowList = new SortedList<string, GuildMgr.GuildMemberDisplay>();
				int keyIncrement = 0;

				foreach (GuildMgr.GuildMemberDisplay memberDisplay in allGuildMembers.Values)
				{
					GamePlayer p = client.Player.Guild.GetOnlineMemberByID(memberDisplay.InternalID);
					if (p != null)
					{
						//Update to make sure we have the most up to date info
						memberDisplay.UpdateMember(p);
					}
					else
					{
						//Make sure that since they are offline they get the offline flag!
						memberDisplay.GroupSize = "0";
					}
					//Add based on the new index
					string key = memberDisplay[sortColumn];

					if (sortedWindowList.ContainsKey(key))
					{
						key += keyIncrement++;
					}

					try
					{
						sortedWindowList.Add(key, memberDisplay);
					}
					catch
					{
						if (log.IsErrorEnabled)
							log.Error(string.Format("Sorted List duplicate entry - Key: {0} Member: {1}. Replacing - Member: {2}.  Sorted count: {3}.  Guild ID: {4}", key, memberDisplay.Name, sortedWindowList[key].Name, sortedWindowList.Count, client.Player.GuildID));
					}
				}
			}

			//Finally lets send the list we made

			IList<GuildMgr.GuildMemberDisplay> finalList = sortedWindowList.Values;

			int i = 0;
			string[] buffer = new string[10];
			for (i = 0; i < 10 && finalList.Count > i + (page - 1) * 10; i++)
			{
				GuildMgr.GuildMemberDisplay memberDisplay;

				if ((int)sortOrder > 0)
				{
					//They want it normal
					memberDisplay = finalList[i + (page - 1) * 10];
				}
				else
				{
					//They want it in reverse
					memberDisplay = finalList[(finalList.Count - 1) - (i + (page - 1) * 10)];
				}

				buffer[i] = memberDisplay.ToString((i + 1) + (page - 1) * 10, finalList.Count);
			}

			client.Out.SendMessage("TE," + page.ToString() + "," + finalList.Count + "," + i.ToString(), EChatType.CT_SocialInterface, EChatLoc.CL_SystemWindow);

			foreach (string member in buffer)
				client.Player.Out.SendMessage(member, EChatType.CT_SocialInterface, EChatLoc.CL_SystemWindow);

		}

		public void DisplayEditHelp(GameClient client)
		{
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildUsage"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditTitle"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditRankLevel"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditEmblem"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditGCHear"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditGCSpeak"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditOCHear"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditOCSpeak"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditACHear"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditACSpeak"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditInvite"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditPromote"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditRemove"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditView"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditAlli"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditClaim"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditUpgrade"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditRelease"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditDues"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage("/gc edit <ranknum> buff <y/n>", EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditWithdraw"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
		}
	}
}
