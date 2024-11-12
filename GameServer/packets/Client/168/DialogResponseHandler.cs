using DOL.Events;
using DOL.GS.Housing;
using DOL.GS.Keeps;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.DialogResponse, "Response Packet from a Question Dialog", eClientStatus.PlayerInGame)]
    public class DialogResponseHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            ushort data1 = packet.ReadShort();
            ushort data2 = packet.ReadShort(); // 0x01 = accept.
            ushort data3 = packet.ReadShort();
            eDialogCode messageType = (eDialogCode) (byte) packet.ReadByte();
            byte response = (byte) packet.ReadByte();

            GamePlayer player = client.Player;

            switch (messageType)
            {
                case eDialogCode.CustomDialog:
                {
                    if (data2 == 0x01)
                    {
                        CustomDialogResponse callback;

                        lock (player) // Why?
                        {
                            callback = player.CustomDialogCallback;
                            player.CustomDialogCallback = null;
                        }

                        if (callback == null)
                            return;

                        callback(player, response);
                    }

                    return;
                }
                case eDialogCode.GuildInvite:
                {
                    GamePlayer guildLeader = null;

                    foreach (Region region in WorldMgr.GetAllRegions())
                    {
                        guildLeader = WorldMgr.GetObjectByIDFromRegion(region.ID, data1) as GamePlayer;

                        if (guildLeader != null)
                            break;
                    }

                    if (response == 0x01)
                    {
                        if (guildLeader == null)
                        {
                            player.Out.SendMessage("You need to be in the same region as the guild leader to accept an invitation.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (player.Guild != null)
                        {
                            player.Out.SendMessage("You are still in a guild, you'll have to leave it first.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (guildLeader.Guild != null)
                        {
                            guildLeader.Guild.AddPlayer(player);
                            return;
                        }

                        player.Out.SendMessage("Player doing the invite is not in a guild!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    guildLeader?.Out.SendMessage(player.Name + " declined your invite.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
                case eDialogCode.GuildLeave:
                {
                    if (response == 0x01)
                    {
                        if (player.Guild == null)
                        {
                            player.Out.SendMessage("You are not in a guild.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        player.Guild.RemovePlayer(player.Name, player);
                    }
                    else
                    {
                        player.Out.SendMessage("You decline to quit your guild.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    return;
                }
                case eDialogCode.QuestSubscribe:
                {
                    if (WorldMgr.GetObjectByIDFromRegion(player.CurrentRegionID, data2) is not GameLiving questNPC)
                        return;

                    QuestEventArgs args = new(questNPC, player, data1);

                    if (response == 0x01)
                        player.Notify(data3 == 0x01 ? GamePlayerEvent.AbortQuest : GamePlayerEvent.AcceptQuest, player, args);
                    else
                        player.Notify(data3 == 0x01 ? GamePlayerEvent.ContinueQuest : GamePlayerEvent.DeclineQuest, player, args);

                    return;
                }
                case eDialogCode.GroupInvite:
                {
                    if (response == 0x01)
                    {
                        GameClient otherClient = ClientService.GetClientFromId(data1);

                        if (otherClient == null)
                            return;

                        GamePlayer groupLeader = otherClient.Player;

                        if (groupLeader == null)
                            return;

                        if (player.Group != null)
                        {
                            player.Out.SendMessage("You are still in a group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (!GameServer.ServerRules.IsAllowedToGroup(groupLeader, player, false))
                        {
                            return;
                        }

                        if (player.InCombatPvE)
                        {
                            player.Out.SendMessage("You can't join a group while in combat!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (groupLeader.Group != null)
                        {
                            if (groupLeader.Group.Leader != groupLeader)
                                return;

                            if (groupLeader.Group.MemberCount >= ServerProperties.Properties.GROUP_MAX_MEMBER)
                            {
                                player.Out.SendMessage("The group is full.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            groupLeader.Group.AddMember(player);
                            GameEventMgr.Notify(GamePlayerEvent.AcceptGroup, player);
                            return;
                        }

                        Group group = new(groupLeader);
                        GroupMgr.AddGroup(group);
                        group.AddMember(groupLeader);
                        group.AddMember(player);
                        GameEventMgr.Notify(GamePlayerEvent.AcceptGroup, player);
                        return;
                    }

                    return;
                }
                case eDialogCode.KeepClaim:
                {
                    if (response == 0x01)
                    {
                        if (player.Guild == null)
                        {
                            player.Out.SendMessage("You have to be a member of a guild, before you can use any of the commands!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(player.CurrentRegionID, player, WorldMgr.VISIBILITY_DISTANCE);

                        if (keep == null)
                        {
                            player.Out.SendMessage("You have to be near the keep to claim it.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (keep.CheckForClaim(player))
                            keep.Claim(player);
                    }

                    return;
                }
                case eDialogCode.HousePayRent:
                {
                    if (response == 0x00)
                    {
                        if (player.TempProperties.GetProperty<long>(HousingConstants.MoneyForHouseRent, -1) != -1)
                            player.TempProperties.RemoveProperty(HousingConstants.MoneyForHouseRent);

                        if (player.TempProperties.GetProperty<long>(HousingConstants.BPsForHouseRent, -1) != -1)
                            player.TempProperties.RemoveProperty(HousingConstants.BPsForHouseRent);

                        player.TempProperties.RemoveProperty(HousingConstants.HouseForHouseRent);
                        return;
                    }

                    House house = player.TempProperties.GetProperty<House>(HousingConstants.HouseForHouseRent);
                    long moneyToAdd = player.TempProperties.GetProperty<long>(HousingConstants.MoneyForHouseRent, -1);
                    long bpsToMoney = player.TempProperties.GetProperty<long>(HousingConstants.BPsForHouseRent, -1);
                    bool usingBps = moneyToAdd == -1;
                    long currencyToUse = usingBps ? bpsToMoney : moneyToAdd;

                    // If we're giving money and already have some in the lockbox, make sure we don't take more than what would cover 4 weeks of rent.
                    if (currencyToUse + house.KeptMoney > HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS)
                        currencyToUse = HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS - house.KeptMoney;

                    if (usingBps)
                    {
                        if (!player.RemoveBountyPoints(currencyToUse))
                            return;
                    }
                    else if (!player.RemoveMoney(currencyToUse))
                        return;

                    InventoryLogging.LogInventoryAction(player, $"(HOUSE;{house.HouseNumber})", eInventoryActionType.Other, currencyToUse);
                    house.KeptMoney += currencyToUse;
                    house.SaveIntoDatabase();
                    player.SaveIntoDatabase();
                    player.Out.SendMessage($"You deposit {Money.GetString(currencyToUse)} in the lockbox.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage($"The lockbox now has {Money.GetString(house.KeptMoney)} in it. The weekly payment is {Money.GetString(HouseMgr.GetRentByModel(house.Model))}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage($"The house is now prepaid for the next {house.KeptMoney / HouseMgr.GetRentByModel(house.Model)} payments.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.TempProperties.RemoveProperty(HousingConstants.MoneyForHouseRent);
                    return;
                }
                case eDialogCode.MasterLevelWindow:
                {
                    player.Out.SendMasterLevelWindow(response);
                    return;
                }
            }
        }
    }
}
