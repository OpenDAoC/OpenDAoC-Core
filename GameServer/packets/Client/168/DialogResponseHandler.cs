using DOL.Events;
using DOL.GS.Housing;
using DOL.GS.Keeps;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.DialogResponse, "Response Packet from a Question Dialog", eClientStatus.PlayerInGame)]
	public class DialogResponseHandler : IPacketHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			ushort data1 = packet.ReadShort();
			ushort data2 = packet.ReadShort();
			ushort data3 = packet.ReadShort();
			var messageType = (byte) packet.ReadByte();
			var response = (byte) packet.ReadByte();

			new DialogBoxResponseAction(client.Player, data1, data2, data3, messageType, response).Start(1);
		}

		/// <summary>
		/// Handles dialog responses from players
		/// </summary>
		protected class DialogBoxResponseAction : AuxECSGameTimerWrapperBase
		{
			/// <summary>
			/// The general data field
			/// </summary>
			protected readonly int m_data1;

			/// <summary>
			/// The general data field
			/// </summary>
			protected readonly int m_data2;

			/// <summary>
			/// The general data field
			/// </summary>
			protected readonly int m_data3;

			/// <summary>
			/// The dialog type
			/// </summary>
			protected readonly int m_messageType;

			/// <summary>
			/// The players response
			/// </summary>
			protected readonly byte m_response;

			/// <summary>
			/// Constructs a new DialogBoxResponseAction
			/// </summary>
			/// <param name="actionSource">The responding player</param>
			/// <param name="data1">The general data field</param>
			/// <param name="data2">The general data field</param>
			/// <param name="data3">The general data field</param>
			/// <param name="messageType">The dialog type</param>
			/// <param name="response">The players response</param>
			public DialogBoxResponseAction(GamePlayer actionSource, int data1, int data2, int data3, int messageType, byte response)
				: base(actionSource)
			{
				m_data1 = data1;
				m_data2 = data2;
				m_data3 = data3;
				m_messageType = messageType;
				m_response = response;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(AuxECSGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;

				// log.DebugFormat("Dialog - response: {0}, messageType: {1}, data1: {2}, data2: {3}, data3: {4}", m_response, m_messageType, m_data1, m_data2, m_data3);

				switch ((eDialogCode) m_messageType)
				{
					case eDialogCode.CustomDialog:
						{
							if (m_data2 == 0x01)
							{
								CustomDialogResponse callback;
								lock (player)
								{
									callback = player.CustomDialogCallback;
									player.CustomDialogCallback = null;
								}

								if (callback == null)
									return 0;

								callback(player, m_response);
							}
							Interval = 0;
							break;
						}
					case eDialogCode.GuildInvite:
					{
						GamePlayer guildLeader = null;
							foreach (var region in WorldMgr.GetAllRegions())
							{
								guildLeader = WorldMgr.GetObjectByIDFromRegion(region.ID, (ushort) m_data1) as GamePlayer;
								if (guildLeader != null)
									break;
							}
							
							if (m_response == 0x01) //accept
							{
								if (guildLeader == null)
								{
									player.Out.SendMessage("You need to be in the same region as the guild leader to accept an invitation.",
									                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return 0;
								}
								if (!ServerProperties.Properties.ALLOW_GUILD_INVITE_IN_RVR && (guildLeader.CurrentZone.IsRvR || player.CurrentZone.IsRvR))
								{
									player.Out.SendMessage("You can't join a guild while in a RvR zone.",
										eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return 0;
								}
								if (player.Guild != null)
								{
									player.Out.SendMessage("You are still in a guild, you'll have to leave it first.", eChatType.CT_System,
									                       eChatLoc.CL_SystemWindow);
									return 0;
								}
								if (guildLeader.Guild != null)
								{
									guildLeader.Guild.AddPlayer(player);
									return 0;
								}

								player.Out.SendMessage("Player doing the invite is not in a guild!", eChatType.CT_System,
								                       eChatLoc.CL_SystemWindow);
								return 0;
							}

							if (guildLeader != null)
							{
								guildLeader.Out.SendMessage(player.Name + " declined your invite.", eChatType.CT_System,
								                            eChatLoc.CL_SystemWindow);
							}
							return 0;
						}
					case eDialogCode.GuildLeave:
						{
							if (m_response == 0x01) //accepte
							{
								if (player.Guild == null)
								{
									player.Out.SendMessage("You are not in a guild.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return 0;
								}

								player.Guild.RemovePlayer(player.Name, player);
							}
							else
							{
								player.Out.SendMessage("You decline to quit your guild.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return 0;
							}
							Interval = 0;
							break;
						}
					case eDialogCode.QuestSubscribe:
						{
							var questNPC = (GameLiving)WorldMgr.GetObjectByIDFromRegion(player.CurrentRegionID, (ushort) m_data2);
							if (questNPC == null)
								return 0;

							var args = new QuestEventArgs(questNPC, player, (ushort) m_data1);
							if (m_response == 0x01) // accept
							{
								// TODO add quest to player
								// Note: This is done withing quest code since we have to check requirements, etc for each quest individually
								// i'm reusing the questsubscribe command for quest abort since its 99% the same, only different event dets fired
								player.Notify(m_data3 == 0x01 ? GamePlayerEvent.AbortQuest : GamePlayerEvent.AcceptQuest, player, args);
								return 0;
							}
							player.Notify(m_data3 == 0x01 ? GamePlayerEvent.ContinueQuest : GamePlayerEvent.DeclineQuest, player, args);
							return 0;
						}
					case eDialogCode.GroupInvite:
						{
							if (m_response == 0x01)
							{
								GameClient cln = ClientService.GetClientFromId(m_data1);
								if (cln == null)
									return 0;

								GamePlayer groupLeader = cln.Player;
								if (groupLeader == null)
									return 0;

								if (player.Group != null)
								{
									player.Out.SendMessage("You are still in a group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return 0;
								}
								if (!GameServer.ServerRules.IsAllowedToGroup(groupLeader, player, false))
								{
									return 0;
								}
								if (player.InCombatPvE)
								{
									player.Out.SendMessage("You can't join a group while in combat!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return 0;
								}
								if (groupLeader.Group != null)
								{
									if (groupLeader.Group.Leader != groupLeader) return 0;
                                    if (groupLeader.Group.MemberCount >= ServerProperties.Properties.GROUP_MAX_MEMBER)
									{
										player.Out.SendMessage("The group is full.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
										return 0;
									}
									groupLeader.Group.AddMember(player);
									GameEventMgr.Notify(GamePlayerEvent.AcceptGroup, player);
									return 0;
								}

								var group = new Group(groupLeader);
								GroupMgr.AddGroup(group);

								group.AddMember(groupLeader);
								group.AddMember(player);

								GameEventMgr.Notify(GamePlayerEvent.AcceptGroup, player);

								return 0;
							}
							Interval = 0;
							break;
						}
					case eDialogCode.KeepClaim:
						{
							if (m_response == 0x01)
							{
								if (player.Guild == null)
								{
									player.Out.SendMessage("You have to be a member of a guild, before you can use any of the commands!",
									                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return 0;
								}

								AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(player.CurrentRegionID, player, WorldMgr.VISIBILITY_DISTANCE);
								if (keep == null)
								{
									player.Out.SendMessage("You have to be near the keep to claim it.", eChatType.CT_System,
									                       eChatLoc.CL_SystemWindow);
									return 0;
								}

								if (keep.CheckForClaim(player))
								{
									keep.Claim(player);
								}
								break;
							}
							Interval = 0;
							break;
						}
					case eDialogCode.HousePayRent:
						{
							if (m_response == 0x00)
							{
								if (player.TempProperties.GetProperty<long>(HousingConstants.MoneyForHouseRent, -1) != -1)
								{
									player.TempProperties.RemoveProperty(HousingConstants.MoneyForHouseRent);
								}

								if (player.TempProperties.GetProperty<long>(HousingConstants.BPsForHouseRent, -1) != -1)
								{
									player.TempProperties.RemoveProperty(HousingConstants.BPsForHouseRent);
								}

								player.TempProperties.RemoveProperty(HousingConstants.HouseForHouseRent);

								return 0;
							}

							var house = player.TempProperties.GetProperty<House>(HousingConstants.HouseForHouseRent, null);
							var moneyToAdd = player.TempProperties.GetProperty<long>(HousingConstants.MoneyForHouseRent, -1);
							var bpsToMoney = player.TempProperties.GetProperty<long>(HousingConstants.BPsForHouseRent, -1);

							if (moneyToAdd != -1)
							{
								// if we're giving money and already have some in the lockbox, make sure we don't
								// take more than what would cover 4 weeks of rent.
								if (moneyToAdd + house.KeptMoney > HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS)
									moneyToAdd = (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS) - house.KeptMoney;

								// take the money from the player
								if (!player.RemoveMoney(moneyToAdd))
									return 0;
								InventoryLogging.LogInventoryAction(player, "(HOUSE;" + house.HouseNumber + ")", eInventoryActionType.Other, moneyToAdd);

								// add the money to the lockbox
								house.KeptMoney += moneyToAdd;

								// save the house and the player
								house.SaveIntoDatabase();
								player.SaveIntoDatabase();

								// notify the player of what we took and how long they are prepaid for
								player.Out.SendMessage("You deposit " + Money.GetString(moneyToAdd) + " in the lockbox.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								player.Out.SendMessage("The lockbox now has " + Money.GetString(house.KeptMoney) + " in it.  The weekly payment is " +
									Money.GetString(HouseMgr.GetRentByModel(house.Model)) + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								player.Out.SendMessage("The house is now prepaid for the next " + (house.KeptMoney/HouseMgr.GetRentByModel(house.Model)) +
									" payments.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

								// clean up
								player.TempProperties.RemoveProperty(HousingConstants.MoneyForHouseRent);
							}
							else
							{
								if (bpsToMoney + house.KeptMoney > HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS)
									bpsToMoney = (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS) - house.KeptMoney;

								if (!player.RemoveBountyPoints(Money.GetGold(bpsToMoney)))
									return 0;

								// add the bps to the lockbox
								house.KeptMoney += bpsToMoney;

								// save the house and the player
								house.SaveIntoDatabase();
								player.SaveIntoDatabase();

								// notify the player of what we took and how long they are prepaid for
								player.Out.SendMessage("You deposit " + Money.GetString(bpsToMoney) + " in the lockbox.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								player.Out.SendMessage("The lockbox now has " + Money.GetString(house.KeptMoney) + " in it.  The weekly payment is " +
									Money.GetString(HouseMgr.GetRentByModel(house.Model)) + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								player.Out.SendMessage("The house is now prepaid for the next " + (house.KeptMoney/HouseMgr.GetRentByModel(house.Model)) +
									" payments.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

								// clean up
								player.TempProperties.RemoveProperty(HousingConstants.BPsForHouseRent);
							}

							// clean up
							player.TempProperties.RemoveProperty(HousingConstants.MoneyForHouseRent);
							Interval = 0;
							break;
						}
					case eDialogCode.MasterLevelWindow:
						{
							player.Out.SendMasterLevelWindow(m_response);
							Interval = 0;
							break;
						}
				}

				return Interval;
			}
		}

	}
}
