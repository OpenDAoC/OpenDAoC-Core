using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.GS.ServerProperties;
using DOL.Language;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    /// <summary>
    /// This class represents a Group inside the game
    /// </summary>
    public class Group : IGameStaticItemOwner
    {
        private readonly List<GameLiving> _groupMembers;
        private readonly Lock _groupMembersLock = new();
        private GroupMemberPositionUpdateTimer _memberPositionUpdateTimer;

        public byte MemberCount
        {
            get
            {
                lock (_groupMembersLock)
                {
                    return (byte) _groupMembers.Count;
                }
            }
        }

        public GameLiving LivingLeader { get; private set; }

        public GamePlayer Leader
        {
            get => LivingLeader as GamePlayer;
            private set => LivingLeader = value;
        }

        public bool AutosplitLoot { get; set; } = true;
        public bool AutosplitCoins { get; set; } = true;
        public byte Status { get; set; } = 0x0A;

        public AbstractMission Mission
        {
            get;
            set
            {
                field = value;

                foreach (GamePlayer groupMember in GetPlayersInTheGroup())
                {
                    groupMember.Out.SendQuestListUpdate();

                    if (value != null)
                        groupMember.Out.SendMessage(field.Description, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
        }

        public Group(GameLiving leader)
        {
            LivingLeader = leader;
            _groupMembers = new(Properties.GROUP_MAX_MEMBER);
            _memberPositionUpdateTimer = new(this);
        }

        public List<GameLiving> GetMembersInTheGroup()
        {
            List<GameLiving> temp = GameLoop.GetListForTick<GameLiving>();

            lock (_groupMembersLock)
            {
                temp.AddRange(_groupMembers);
            }

            return temp;
        }

        public List<GamePlayer> GetPlayersInTheGroup()
        {
            List<GamePlayer> temp = GameLoop.GetListForTick<GamePlayer>();

            lock (_groupMembersLock)
            {
                foreach (GameLiving groupMember in _groupMembers)
                {
                    if (groupMember is GamePlayer player)
                        temp.Add(player);
                }
            }

            return temp;
        }

        public bool AddMember(GameLiving living)
        {
            int memberCount;

            lock (_groupMembersLock)
            {
                if (_groupMembers.Count >= Properties.GROUP_MAX_MEMBER || living.Group != null)
                    return false;

                _groupMembers.Add(living);
                memberCount = _groupMembers.Count;
                living.Group = this;
                living.GroupIndex = (byte) (memberCount - 1);
            }

            SendMessageToGroupMembers($"{living.Name} has joined the group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            if (living is GamePlayer player)
            {
                player.Duel?.Stop();
                player.Out.SendGroupMembersUpdate(true, false, CollectionsMarshal.AsSpan(GetMembersInTheGroup()));
                AbstractMission groupMission = Mission;

                if (groupMission != null)
                {
                    // Quest list updates also carry task/mission text for newer clients.
                    player.Out.SendQuestListUpdate();
                    player.Out.SendMessage(groupMission.Description, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                // Part of the hack to make friendly pets untargetable (or targetable again) with TAB on a PvP server.
                // We could also check for non controlled pets (turrets for example) around the player, but it isn't very important.
                if (GameServer.Instance.Configuration.ServerType is EGameServerType.GST_PvP)
                {
                    IControlledBrain controlledBrain = player.ControlledBrain;
                    Guild playerGuild = player.Guild;
                    bool updateOneself = false;

                    // Update how the added player sees their pet and themself.
                    if (controlledBrain != null)
                    {
                        SendControlledBodyGuildID(player, playerGuild, controlledBrain.Body);
                        updateOneself = true;
                    }

                    // Let's all be friends.
                    foreach (GamePlayer groupMember in GetPlayersInTheGroup())
                    {
                        if (groupMember == living)
                            continue;

                        Guild groupMemberGuild = groupMember.Guild;

                        if (controlledBrain != null)
                        {
                            // Update how the group member sees the added player's pet and themself.
                            SendControlledBodyGuildID(groupMember, groupMemberGuild, controlledBrain.Body);
                            groupMember.Out.SendObjectGuildID(groupMember, groupMemberGuild ?? Guild.DummyGuild);
                        }

                        IControlledBrain groupMemberControlledBrain = groupMember.ControlledBrain;

                        if (groupMemberControlledBrain != null)
                        {
                            // Update how the added player sees the group member's pet and themself.
                            SendControlledBodyGuildID(player, playerGuild, groupMemberControlledBrain.Body);
                            updateOneself = true;
                        }
                    }

                    if (updateOneself)
                        player.Out.SendObjectGuildID(player, playerGuild ?? Guild.DummyGuild);
                }
            }

            UpdateMember(living, true, true);
            UpdateGroupWindow();
            GameEventMgr.Notify(GroupEvent.MemberJoined, this, new MemberJoinedEventArgs(living));
            return true;
        }

        public bool RemoveMember(GameLiving living)
        {
            AbstractMission groupMission;

            lock (_groupMembersLock)
            {
                if (!_groupMembers.Remove(living))
                    return false;

                groupMission = Mission;
                living.Group = null;
                living.GroupIndex = 0xFF;

                if (_groupMembers.Count < 1)
                    DisbandGroup();
            }

            SendMessageToGroupMembers($"{living.Name} has left the group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            // Update Player.
            if (living is GamePlayer player)
            {
                player.Out.SendGroupWindowUpdate();

                if (groupMission != null)
                {
                    // Quest list updates also carry task/mission text for newer clients.
                    player.Out.SendQuestListUpdate();
                }

                List<ECSGameAbilityEffect> abilityEffects = player.effectListComponent.GetAbilityEffects();

                // Cancel ability effects.
                foreach (ECSGameAbilityEffect abilityEffect in abilityEffects)
                {
                    switch (abilityEffect.EffectType)
                    {
                        case eEffect.Guard:
                        {
                            if (abilityEffect is GuardECSGameEffect guard)
                            {
                                if (guard.Source is GamePlayer && guard.Target is GamePlayer)
                                    _ = guard.End();
                            }

                            continue;
                        }
                        case eEffect.Protect:
                        {
                            if (abilityEffect is ProtectECSGameEffect protect)
                            {
                                if (protect.Source is GamePlayer && protect.Target is GamePlayer)
                                    _ = protect.End();
                            }

                            continue;
                        }
                        case eEffect.Intercept:
                        {
                            if (abilityEffect is InterceptECSGameEffect intercept)
                            {
                                if (intercept.Source is GamePlayer && intercept.Target is GamePlayer)
                                    _ = intercept.End();
                            }

                            continue;
                        }
                    }
                }

                // Part of the hack to make friendly pets untargetable (or targetable again) with TAB on a PvP server.
                // We could also check for non controlled pets (turrets for example) around the player, but it isn't very important.
                if (GameServer.Instance.Configuration.ServerType is EGameServerType.GST_PvP)
                {
                    IControlledBrain controlledBrain = player.ControlledBrain;
                    Guild playerGuild = player.Guild;
                    bool updateOneself = false;

                    // Update how the removed player sees their pet and themself.
                    if (controlledBrain != null)
                    {
                        SendControlledBodyGuildID(player, playerGuild, controlledBrain.Body);
                        updateOneself = true;
                    }

                    foreach (GamePlayer groupMember in GetPlayersInTheGroup())
                    {
                        if (groupMember == living)
                            continue;

                        Guild groupMemberGuild = groupMember.Guild;

                        if (playerGuild == null || groupMemberGuild == null || playerGuild != groupMemberGuild)
                        {
                            // Update how the group member sees the removed player's pet.
                            // There shouldn't be any need to update them.
                            if (controlledBrain != null)
                                SendControlledBodyGuildID(groupMember, playerGuild, controlledBrain.Body);

                            IControlledBrain groupMemberControlledBrain = groupMember.ControlledBrain;

                            // Update how the removed player sees the group member's pet and themself.
                            if (groupMemberControlledBrain != null)
                            {
                                SendControlledBodyGuildID(player, groupMemberGuild, groupMemberControlledBrain.Body);
                                updateOneself = true;
                            }
                        }
                    }

                    if (updateOneself)
                        player.Out.SendObjectGuildID(player, playerGuild ?? Guild.DummyGuild);
                }

                player.Out.SendMessage("You leave your group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.Notify(GamePlayerEvent.LeaveGroup, player);
            }

            lock (_groupMembersLock)
            {
                int memberCount = _groupMembers.Count;

                if (memberCount == 1)
                {
                    // Disband the group.
                    _ = RemoveMember(_groupMembers[0]);
                }
                else if (memberCount > 1 && LivingLeader == living)
                {
                    // Assign a new leader.
                    LivingLeader = _groupMembers.OfType<GamePlayer>().First() ?? _groupMembers[0];
                    SendMessageToGroupMembers($"{Leader.Name} is the new group leader.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }

            UpdateGroupIndexes();
            UpdateGroupWindow();
            GameEventMgr.Notify(GroupEvent.MemberDisbanded, this, new MemberDisbandedEventArgs(living));
            return true;
        }

        // Part of the hack to make friendly pets untargetable (or targetable again) with TAB on a PvP server.
        // Calls 'SendObjectGuildID' on the player and looks for all controlled NPCs controlled by 'controlledBody' recursively.
        private static void SendControlledBodyGuildID(GamePlayer player, Guild playerGuild, GameNPC controlledBody)
        {
            IControlledBrain[] npcControlledBrains = controlledBody.ControlledNpcList;

            if (npcControlledBrains != null)
            {
                foreach (IControlledBrain npcControlledBrain in npcControlledBrains.Where(x => x != null))
                    SendControlledBodyGuildID(player, playerGuild, npcControlledBrain.Body);
            }

            player.Out.SendObjectGuildID(controlledBody, playerGuild ?? Guild.DummyGuild);
        }

        public void DisbandGroup()
        {
            _ = GroupMgr.RemoveGroup(this);
            Mission?.ExpireMission();

            if (_memberPositionUpdateTimer != null)
            {
                _memberPositionUpdateTimer.Stop();
                _memberPositionUpdateTimer = null;
            }

            lock (_groupMembersLock)
            {
                LivingLeader = null;
                _groupMembers.Clear();
            }
        }

        private void UpdateGroupIndexes()
        {
            var shiftedMembers = GameLoop.GetListForTick<GameLiving>();

            lock (_groupMembersLock)
            {
                for (int i = 0; i < _groupMembers.Count; i++)
                {
                    if (_groupMembers[i].GroupIndex == (byte) i)
                        continue;

                    _groupMembers[i].GroupIndex = (byte) i;
                    shiftedMembers.Add(_groupMembers[i]);
                }
            }

            if (shiftedMembers.Count > 0)
                UpdateMembers(CollectionsMarshal.AsSpan(shiftedMembers), true, true);
        }

        public bool MakeLeader(GameLiving living)
        {
            GameLiving oldLeader;

            lock (_groupMembersLock)
            {
                if (LivingLeader == living || living == null || living.Group != this)
                    return false;

                byte index = living.GroupIndex;

                oldLeader = _groupMembers[0];
                _groupMembers[index] = oldLeader;
                _groupMembers[0] = living;
                LivingLeader = living;
                living.GroupIndex = 0;
                oldLeader.GroupIndex = index;
            }

            UpdateMembers([oldLeader, living], true, true);
            UpdateGroupWindow();
            SendMessageToGroupMembers($"{Leader.Name} is the new group leader.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return true;
        }

        public bool SwitchPlayers(GameLiving source, GameLiving target)
        {
            lock (_groupMembersLock)
            {
                if (source == target || !_groupMembers.Contains(source) || !_groupMembers.Contains(target))
                    return false;

                byte sourceIndex = source.GroupIndex;
                byte targetIndex = target.GroupIndex;

                source.GroupIndex = targetIndex;
                _groupMembers[targetIndex] = source;
                target.GroupIndex = sourceIndex;
                _groupMembers[sourceIndex] = target;
            }

            UpdateMembers([source, target], true, true);
            UpdateGroupWindow();
            SendMessageToGroupMembers($"Switched group member {source.Name} with {target.Name}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return true;
        }

        public GamePlayer GetMemberByIndex(byte index)
        {
            GamePlayer player = null;

            lock (_groupMembersLock)
            {
                player = _groupMembers.Count > index ? _groupMembers[index] as GamePlayer : null;
            }

            return player;
        }

        public virtual void SendMessageToGroupMembers(GameLiving from, string msg, eChatType type, eChatLoc loc)
        {
            string message = from != null ? $"[Party] {from.GetName(0, true)}: \"{msg}\"" : $"[Party] {msg}";
            SendMessageToGroupMembers(message, type, loc);
        }

        public virtual void SendMessageToGroupMembers(string msg, eChatType type, eChatLoc loc)
        {
            foreach (GamePlayer player in GetPlayersInTheGroup())
                player.Out.SendMessage(msg, type, loc);
        }

        public void UpdateMember(GameLiving living, bool updateIcons, bool updateOtherRegions)
        {
            UpdateMembers([living], updateIcons, updateOtherRegions);
        }

        public void UpdateMembers(ReadOnlySpan<GameLiving> livings, bool updateIcons, bool updateOtherRegions)
        {
            foreach (GamePlayer player in GetPlayersInTheGroup())
            {
                List<GameLiving> updatesForPlayer = GameLoop.GetListForTick<GameLiving>();

                foreach (GameLiving living in livings)
                {
                    if (living.Group != this)
                        continue;

                    if (updateOtherRegions || player.CurrentRegion == living.CurrentRegion)
                        updatesForPlayer.Add(living);
                }

                if (updatesForPlayer.Count > 0)
                    player.Out.SendGroupMembersUpdate(updateIcons, false, CollectionsMarshal.AsSpan(updatesForPlayer));
            }
        }

        public void UpdateAllToMember(GamePlayer player, bool updateIcons, bool updateOtherRegions)
        {
            if (player.Group != this)
                return;

            List<GameLiving> updatesForPlayer = GameLoop.GetListForTick<GameLiving>();

            foreach (GameLiving living in GetMembersInTheGroup())
            {
                if (updateOtherRegions || living.CurrentRegion == player.CurrentRegion)
                    updatesForPlayer.Add(living);
            }

            if (updatesForPlayer.Count > 0)
                player.Out.SendGroupMembersUpdate(updateIcons, false, CollectionsMarshal.AsSpan(updatesForPlayer));
        }

        public void UpdateGroupWindow()
        {
            foreach (GamePlayer player in GetPlayersInTheGroup())
                player.Out.SendGroupWindowUpdate();
        }

        public string Name => $"{(Leader == null || MemberCount <= 0 ? "leaderless" : $"{Leader.Name}'s")} group (size: {MemberCount})";

        public object GameStaticItemOwnerComparand => null;

        public TryPickUpResult TryAutoPickUpMoney(GameMoney money)
        {
            return TryPickUpMoney(Leader, money);
        }

        public TryPickUpResult TryAutoPickUpItem(WorldInventoryItem inventoryItem)
        {
            // We don't care if players have auto loot enabled, or if they can see the item (the item isn't added to the world yet anyway), or who attacked last, etc.
            return TryPickUpItem(Leader, inventoryItem);
        }

        public TryPickUpResult TryPickUpMoney(GamePlayer source, GameMoney money)
        {
            money.AssertLockAcquisition();

            if (!AutosplitCoins)
                return TryPickUpResult.DoesNotWant;

            List<GamePlayer> eligibleMembers = new(8);

            // Members must be in visible range.
            foreach (GamePlayer member in GetPlayersInTheGroup())
            {
                // Ignores `GamePlayer.AutoSplitLoot`.
                if (member.ObjectState is eObjectState.Active && member.CanSeeObject(money))
                    eligibleMembers.Add(member);
            }

            if (eligibleMembers.Count == 0)
            {
                source.Out.SendMessage(LanguageMgr.GetTranslation(source.Client.Account.Language, "GamePlayer.PickupObject.NoOneGroupWantsMoney"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return TryPickUpResult.Blocked;
            }

            SplitMoneyBetweenEligibleMembers(eligibleMembers, money);
            _ = money.RemoveFromWorld();
            return TryPickUpResult.Success;

            static void SplitMoneyBetweenEligibleMembers(List<GamePlayer> eligibleMembers, GameMoney money)
            {
                long splitMoney = (long) Math.Ceiling((double) money.Value / eligibleMembers.Count);
                long moneyToPlayer;

                foreach (GamePlayer eligibleMember in eligibleMembers)
                {
                    moneyToPlayer = eligibleMember.ApplyGuildDues(splitMoney);

                    if (moneyToPlayer > 0)
                    {
                        eligibleMember.AddMoney(moneyToPlayer, LanguageMgr.GetTranslation(eligibleMember.Client.Account.Language, eligibleMembers.Count > 1 ? "GamePlayer.PickupObject.YourLootShare" : "GamePlayer.PickupObject.YouPickUp", Money.GetString(splitMoney)));
                        InventoryLogging.LogInventoryAction("(ground)", eligibleMember, eInventoryActionType.Loot, splitMoney);
                    }
                }
            }
        }

        public TryPickUpResult TryPickUpItem(GamePlayer source, WorldInventoryItem item)
        {
            item.AssertLockAcquisition();

            // A group is only able to pick up items if auto split is enabled. Otherwise, solo logic should apply.
            // Group members are filtered to exclude far away players or players with auto split solo enabled.
            // A player with enough room in his inventory is chosen randomly.
            // If there is none, the item should simply stays on the ground.
            // Items discarded by players can only be picked up by those same players.
            if (!AutosplitLoot || item.IsPlayerDiscarded)
                return TryPickUpResult.DoesNotWant;

            List<GamePlayer> eligibleMembers = new(8);

            // Members must be in visible range.
            foreach (GamePlayer member in GetPlayersInTheGroup())
            {
                if (member.ObjectState is eObjectState.Active && member.AutoSplitLoot && member.CanSeeObject(item))
                    eligibleMembers.Add(member);
            }

            if (eligibleMembers.Count == 0)
            {
                source.Out.SendMessage(LanguageMgr.GetTranslation(source.Client.Account.Language, "GamePlayer.PickupObject.NoOneWantsThis", item.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return TryPickUpResult.Blocked;
            }

            if (!GiveItemToRandomEligibleMember(eligibleMembers, item.Item, out GamePlayer eligibleMember))
                return TryPickUpResult.Blocked;

            Message.SystemToOthers(source, LanguageMgr.GetTranslation(source.Client.Account.Language, "GamePlayer.PickupObject.GroupMemberPicksUp", Name, item.Item.GetName(1, false)), eChatType.CT_System);
            SendMessageToGroupMembers(LanguageMgr.GetTranslation(source.Client.Account.Language, "GamePlayer.PickupObject.Autosplit", item.Item.GetName(1, true), eligibleMember.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            InventoryLogging.LogInventoryAction("(ground)", eligibleMember, eInventoryActionType.Loot, item.Item.Template, item.Item.IsStackable ? item.Item.Count : 1);
            _ = item.RemoveFromWorld();
            return TryPickUpResult.Success;

            static bool GiveItemToRandomEligibleMember(List<GamePlayer> eligibleMembers, DbInventoryItem item, out GamePlayer eligibleMember)
            {
                int randomIndex;
                int lastIndex;

                do
                {
                    lastIndex = eligibleMembers.Count - 1;
                    randomIndex = Util.Random(0, lastIndex);
                    eligibleMember = eligibleMembers[randomIndex];

                    if (GiveItem(eligibleMember, item))
                        return true;

                    eligibleMember.Out.SendMessage(LanguageMgr.GetTranslation(eligibleMember.Client.Account.Language, "GamePlayer.PickupObject.BackpackFull"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    eligibleMembers.SwapRemoveAt(randomIndex);
                } while (eligibleMembers.Count > 0);

                return false;

                static bool GiveItem(GamePlayer player, DbInventoryItem item)
                {
                    return item.IsStackable ?
                        player.Inventory.AddTemplate(item, item.Count, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) :
                        player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);
                }
            }
        }

        public bool IsGroupInCombat()
        {
            lock (_groupMembers)
            {
                return _groupMembers.Any(static m => m.InCombat);
            }
        }

        public bool IsInTheGroup(GameLiving living)
        {
            lock (_groupMembers)
            {
                return _groupMembers.Contains(living);
            }
        }

        public string GroupMemberString(GamePlayer player)
        {
            StringBuilder text = new(64);
            BattleGroup battlegroup = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);

            foreach (GamePlayer groupMember in GetPlayersInTheGroup())
            {
                if (battlegroup.IsInTheBattleGroup(groupMember))
                {
                    if (battlegroup.Members.Contains(groupMember))
                        _ = text.Append("<Leader> ");

                    _ = text.Append("(I)");
                }

                _ = text.Append($"{groupMember.Name} ");
            }

            return text.ToString();
        }

        public string GroupMemberClassString()
        {
            StringBuilder text = new(64);

            foreach (GamePlayer groupMember in GetPlayersInTheGroup())
                _ = text.Append($"{groupMember.Name} ({groupMember.CharacterClass.Name}) ");

            return text.ToString();
        }

        private class GroupMemberPositionUpdateTimer : ECSGameTimerWrapperBase
        {
            public const int POSITION_UPDATE_INTERVAL = 1000;

            private readonly Group _group;

            public GroupMemberPositionUpdateTimer(Group group) : base(null)
            {
                _group = group;
                Start(POSITION_UPDATE_INTERVAL);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (_group.LivingLeader == null)
                    return 0;

                var playersInGroup = _group.GetPlayersInTheGroup();

                foreach (GamePlayer player in playersInGroup)
                    player.Out.SendGroupMembersUpdate(false, true, CollectionsMarshal.AsSpan(playersInGroup));

                return POSITION_UPDATE_INTERVAL;
            }
        }
    }
}
