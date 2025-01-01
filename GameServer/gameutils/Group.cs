using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using static DOL.GS.GameObject;
using static DOL.GS.IGameStaticItemOwner;

namespace DOL.GS
{
	/// <summary>
	/// This class represents a Group inside the game
	/// </summary>
	public class Group : IGameStaticItemOwner
	{
		#region constructor and members
		/// <summary>
		/// Default Constructor with GamePlayer Leader.
		/// </summary>
		/// <param name="leader"></param>
		public Group(GamePlayer leader)
			: this((GameLiving)leader)
		{
		}
		
		/// <summary>
		/// Default Constructor with GameLiving Leader.
		/// </summary>
		/// <param name="leader"></param>
		public Group(GameLiving leader)
		{
			LivingLeader = leader;
			m_groupMembers = new ReaderWriterList<GameLiving>(ServerProperties.Properties.GROUP_MAX_MEMBER);
		}
		
		/// <summary>
		/// This holds all players inside the group
		/// </summary>
		protected readonly ReaderWriterList<GameLiving> m_groupMembers;
		protected readonly Lock _groupMembersLock = new();
		#endregion

		#region Leader / Member
		
		/// <summary>
		/// Gets/sets the group Player leader
		/// </summary>
		public GamePlayer Leader
		{
			get { return LivingLeader as GamePlayer; }
			private set { LivingLeader = value; }
		}

		/// <summary>
		/// Gets/sets the group Living leader
		/// </summary>
		public GameLiving LivingLeader { get; protected set; }
		
		/// <summary>
		/// Returns the number of players inside this group
		/// </summary>
		public byte MemberCount
		{
			get { return (byte)m_groupMembers.Count; }
		}

		#endregion

		#region mission

		/// <summary>
		/// This Group Mission.
		/// </summary>
		private Quests.AbstractMission m_mission = null;
		
		/// <summary>
		/// Group Mission
		/// </summary>
		public Quests.AbstractMission Mission
		{
			get { return m_mission; }
			set
			{
				m_mission = value;
				foreach (GamePlayer player in m_groupMembers.OfType<GamePlayer>())
				{
					player.Out.SendQuestListUpdate();
					if (value != null)
						player.Out.SendMessage(m_mission.Description, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
		}

		#endregion

		#region autosplit

		/// <summary>
		/// Gets or sets the group's autosplit loot flag
		/// </summary>
		protected bool m_autosplitLoot = true;
		
		/// <summary>
		/// Gets or sets the group's autosplit loot flag
		/// </summary>
		public bool AutosplitLoot
		{
			get { return m_autosplitLoot; }
			set { m_autosplitLoot = value; }
		}

		/// <summary>
		/// Gets or sets the group's autosplit coins flag
		/// </summary>
		protected bool m_autosplitCoins = true;

		/// <summary>
		/// Gets or sets the group's autosplit coins flag
		/// </summary>
		public bool AutosplitCoins
		{
			get { return m_autosplitCoins; }
			set { m_autosplitCoins = value; }
		}

		#endregion

		#region lfg status

		/// <summary>
		/// This holds the status of the group
		/// eg. looking for members etc ...
		/// </summary>
		protected byte m_status = 0x0A;

		/// <summary>
		/// Gets or sets the status of this group
		/// </summary>
		public byte Status
		{
			get { return m_status; }
			set { m_status = value; }
		}

        #endregion

        #region managing members

        /// <summary>
        /// Gets all members of the group
        /// </summary>
        /// <returns>Array of GameLiving in this group</returns>
        public ICollection<GameLiving> GetMembersInTheGroup()
		{
			return m_groupMembers.ToArray();
		}
		
		/// <summary>
		/// Gets all players of the group
		/// </summary>
		/// <returns>Array of GamePlayers in this group</returns>
		public ICollection<GamePlayer> GetPlayersInTheGroup()
		{
			return m_groupMembers.OfType<GamePlayer>().ToArray();
		}

		/// <summary>
		/// Adds a living to the group
		/// </summary>
		/// <param name="living">GameLiving to be added to the group</param>
		/// <returns>true if added successfully</returns>
		public virtual bool AddMember(GameLiving living) 
		{
			if (!m_groupMembers.FreezeWhile<bool>(l => { if (l.Count >= ServerProperties.Properties.GROUP_MAX_MEMBER || l.Count >= (byte.MaxValue - 1))
															return false;
													
														if (l.Contains(living))
															return false;
													
														l.Add(living);
														living.Group = this;
														living.GroupIndex = (byte)(l.Count - 1);
														return true; }))
				return false;

			GamePlayer player = living as GamePlayer;

			if (player?.Duel != null)
				player.Duel.Stop();
			
			UpdateGroupWindow();
			// update icons of joined player to everyone in the group
			UpdateMember(living, true, false);
			
			// update all icons for just joined player
			if (player != null)
				player.Out.SendGroupMembersUpdate(true, true);

			SendMessageToGroupMembers(string.Format("{0} has joined the group.", living.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			GameEventMgr.Notify(GroupEvent.MemberJoined, this, new MemberJoinedEventArgs(living));


			//use this to track completely solo characters
			const string customKey = "grouped_char";
			var hasGrouped = DOLDB<DbCoreCharacterXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));

			if (hasGrouped == null)
			{
				DbCoreCharacterXCustomParam groupedChar = new DbCoreCharacterXCustomParam();
				groupedChar.DOLCharactersObjectId = player.ObjectId;
				groupedChar.KeyName = customKey;
				groupedChar.Value = "1";
				GameServer.Database.AddObject(groupedChar);
			}

			// Part of the hack to make friendly pets untargetable (or targetable again) with TAB on a PvP server.
			// We could also check for non controlled pets (turrets for example) around the player, but it isn't very important.
			if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_PvP)
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
				foreach (GamePlayer groupMember in m_groupMembers.OfType<GamePlayer>().Where(x => x != player))
				{
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

			return true;
		}
		
		/// <summary>
		/// Removes a living from the group
		/// </summary>
		/// <param name="living">GameLiving to be removed</param>
		/// <returns>true if removed, false if not</returns>
		public virtual bool RemoveMember(GameLiving living)
		{
			if (!m_groupMembers.TryRemove(living))
				return false;

			if (MemberCount < 1)
				DisbandGroup();
			
			living.Group = null;
			living.GroupIndex = 0xFF;
			GamePlayer player = living as GamePlayer;

			// Update Player.
			if (player != null)
			{
				player.Out.SendGroupWindowUpdate();
				player.Out.SendQuestListUpdate();

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
									EffectService.RequestCancelEffect(guard);
							}

							continue;
						}
						case eEffect.Protect:
						{
							if (abilityEffect is ProtectECSGameEffect protect)
							{
								if (protect.Source is GamePlayer && protect.Target is GamePlayer)
									EffectService.RequestCancelEffect(protect);
							}

							continue;
						}
						case eEffect.Intercept:
						{
							if (abilityEffect is InterceptECSGameEffect intercept)
							{
								if (intercept.Source is GamePlayer && intercept.Target is GamePlayer)
									EffectService.RequestCancelEffect(intercept);
							}

							continue;
						}
					}
				}

				// Part of the hack to make friendly pets untargetable (or targetable again) with TAB on a PvP server.
				// We could also check for non controlled pets (turrets for example) around the player, but it isn't very important.
				if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_PvP)
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

					foreach (GamePlayer groupMember in m_groupMembers.OfType<GamePlayer>())
					{
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
								SendControlledBodyGuildID(player, groupMember.Guild, groupMemberControlledBrain.Body);
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

			UpdateGroupWindow();
			SendMessageToGroupMembers(string.Format("{0} has left the group.", living.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			// only one member left?
			if (MemberCount == 1)
			{
				// RR4: Group is disbanded, ending mission group if any
				RemoveMember(m_groupMembers.First());
			}

			// Update all members
			if (MemberCount > 1 && LivingLeader == living)
			{
				var newLeader = m_groupMembers.OfType<GamePlayer>().First();

				if (newLeader != null)
				{
					LivingLeader = newLeader;
					SendMessageToGroupMembers(string.Format("{0} is the new group leader.", Leader.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				else
				{
					// Set aother Living Leader.
					LivingLeader = m_groupMembers.First();
				}
			}

			UpdateGroupIndexes();
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

		/// <summary>
		/// Clear this group
		/// </summary>
		public void DisbandGroup()
		{
			GroupMgr.RemoveGroup(this);
			
			if (Mission != null)
				Mission.ExpireMission();
			
			LivingLeader = null;
			m_groupMembers.Clear();
		}
		
		/// <summary>
		/// Updates player indexes
		/// </summary>
		private void UpdateGroupIndexes()
		{
			m_groupMembers.FreezeWhile(l => {
										for (byte ind = 0; ind < l.Count; ind++)
											l[ind].GroupIndex = ind;
			});
		}
		
		/// <summary>
		/// Makes living current leader of the group
		/// </summary>
		/// <param name="living"></param>
		/// <returns></returns>
		public bool MakeLeader(GameLiving living)
		{
			bool allOk = m_groupMembers.FreezeWhile<bool>(l => {
														if (!l.Contains(living))
															return false;
														
														byte ind = living.GroupIndex;
														var oldLeader = l[0];
														l[ind] = oldLeader;
														l[0] = living;
														LivingLeader = living;
														living.GroupIndex = 0;
														oldLeader.GroupIndex = ind;
														
														return true;
													});
			if (allOk)
			{
				// all went ok
				UpdateGroupWindow();
				SendMessageToGroupMembers(string.Format("{0} is the new group leader.", Leader.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}

			return allOk;
		}

		/// <summary>
		/// Makes living current leader of the group
		/// </summary>
		/// <param name="living"></param>
		/// <returns></returns>
		public bool SwitchPlayers(GameLiving source, GameLiving target)
		{
			bool allOk = m_groupMembers.FreezeWhile<bool>(l => {
				if (!l.Contains(source))
					return false;
				if (!l.Contains(target))
					return false;
														
				byte sourceInd = source.GroupIndex;
				byte targetInd = target.GroupIndex;
				
				source.GroupIndex = targetInd;
				l[targetInd] = source;
				target.GroupIndex = sourceInd;
				l[sourceInd] = target;

				return true;
			});
			if (allOk)
			{
				// all went ok
				UpdateGroupWindow();
				SendMessageToGroupMembers(string.Format("Switched group member {0} with {1}", source.Name, target.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}

			return allOk;
		}

		public GamePlayer GetMemberByIndex(byte index)
		{
			GamePlayer player = m_groupMembers.FreezeWhile<GamePlayer>(l =>
			{

				GamePlayer player = l[index] as GamePlayer;
				
				if (player == null)
					return null;

				return player;
			});
			if (player != null)
			{
				// all went ok
				UpdateGroupWindow(); 
			}

			return player;
		}

		#endregion

		#region messaging

		/// <summary>
		/// Sends a message to all group members with an object from
		/// </summary>
		/// <param name="from">GameLiving source of the message</param>
		/// <param name="msg">message string</param>
		/// <param name="type">message type</param>
		/// <param name="loc">message location</param>
		public virtual void SendMessageToGroupMembers(GameLiving from, string msg, eChatType type, eChatLoc loc)
		{
			string message;
			if (from != null)
			{
				message = string.Format("[Party] {0}: \"{1}\"", from.GetName(0, true), msg);
			}
			else
			{
				message = string.Format("[Party] {0}", msg);
			}

			SendMessageToGroupMembers(message, type, loc);
		}

		/// <summary>
		/// Send Raw Message to all group members.
		/// </summary>
		/// <param name="msg">message string</param>
		/// <param name="type">message type</param>
		/// <param name="loc">message location</param>
		public virtual void SendMessageToGroupMembers(string msg, eChatType type, eChatLoc loc)
		{
			foreach (GamePlayer player in GetPlayersInTheGroup())
				player.Out.SendMessage(msg, type, loc);
		}

		#endregion

		#region update group

		/// <summary>
		/// Updates a group member to all other living in the group
		/// </summary>
		/// <param name="living">living to update</param>
		/// <param name="updateIcons">Do icons need an update</param>
		/// <param name="updateOtherRegions">Should updates be sent to players in other regions</param>
		public void UpdateMember(GameLiving living, bool updateIcons, bool updateOtherRegions)
		{
			if (living.Group != this)
				return;
			
			foreach (var player in GetPlayersInTheGroup())
			{
				if (updateOtherRegions || player.CurrentRegion == living.CurrentRegion)
					player.Out.SendGroupMemberUpdate(updateIcons, true, living);
			}
		}

		/// <summary>
		/// Updates all group members to one member
		/// </summary>
		/// <param name="player">The player that should receive updates</param>
		/// <param name="updateIcons">Do icons need an update</param>
		/// <param name="updateOtherRegions">Should updates be sent to players in other regions</param>
		public void UpdateAllToMember(GamePlayer player, bool updateIcons, bool updateOtherRegions)
		{
			if (player.Group != this)
				return;
			
			foreach (GameLiving living in m_groupMembers)
			{
				if (updateOtherRegions || living.CurrentRegion == player.CurrentRegion)
				{
					player.Out.SendGroupMemberUpdate(updateIcons, true, living);
				}
			}
		}

		/// <summary>
		/// Updates the group window to all players
		/// </summary>
		public void UpdateGroupWindow()
		{
			foreach (GamePlayer player in GetPlayersInTheGroup())
				player.Out.SendGroupWindowUpdate();
		}

		#endregion

		#region utils

		public string Name => $"{(Leader == null || MemberCount <= 0 ? "leaderless" : $"{Leader.Name}'s")} group (size: {MemberCount})";

		public bool TryAutoPickUpMoney(GameMoney money)
		{
			return TryPickUpMoney(Leader, money) is not TryPickUpResult.CANNOT_HANDLE;
		}

		public bool TryAutoPickUpItem(WorldInventoryItem inventoryItem)
		{
			// We don't care if players have auto loot enabled, or if they can see the item (the item isn't added to the world yet anyway), or who attacked last, etc.
			return TryPickUpItem(Leader, inventoryItem) is not TryPickUpResult.CANNOT_HANDLE;
		}

		public TryPickUpResult TryPickUpMoney(GamePlayer source, GameMoney money)
		{
			if (!AutosplitCoins)
				return TryPickUpResult.CANNOT_HANDLE;

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
				return TryPickUpResult.FAILED;
			}

			SplitMoneyBetweenEligibleMembers(eligibleMembers, money);
			money.RemoveFromWorld();
			return TryPickUpResult.SUCCESS;

			static void SplitMoneyBetweenEligibleMembers(List<GamePlayer> eligibleMembers, GameMoney money)
			{
				long splitMoney = (long) Math.Ceiling((double) money.TotalCopper / eligibleMembers.Count);
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
			// A group is only able to pick up items if auto split is enabled. Otherwise, solo logic should apply.
			// Group members are filtered to exclude far away players or players with auto split solo enabled.
			// A player with enough room in his inventory is chosen randomly.
			// If there is none, the item should simply stays on the ground.
			if (!AutosplitLoot)
				return TryPickUpResult.CANNOT_HANDLE;

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
				return TryPickUpResult.FAILED;
			}

			if (!GiveItemToRandomEligibleMember(eligibleMembers, item.Item, out GamePlayer eligibleMember))
				return TryPickUpResult.FAILED;

			Message.SystemToOthers(source, LanguageMgr.GetTranslation(source.Client.Account.Language, "GamePlayer.PickupObject.GroupMemberPicksUp", Name, item.Item.GetName(1, false)), eChatType.CT_System);
			SendMessageToGroupMembers(LanguageMgr.GetTranslation(source.Client.Account.Language, "GamePlayer.PickupObject.Autosplit", item.Item.GetName(1, true), eligibleMember.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			InventoryLogging.LogInventoryAction("(ground)", source, eInventoryActionType.Loot, item.Item.Template, item.Item.IsStackable ? item.Item.Count : 1);
			item.RemoveFromWorld();
			return TryPickUpResult.SUCCESS;

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
					if (item.IsStackable)
						return player.Inventory.AddTemplate(item, item.Count, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

					return player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);
				}
			}
		}

		/// <summary>
		/// If at least one player is in combat group is in combat
		/// </summary>
		/// <returns>true if group in combat</returns>
		public bool IsGroupInCombat()
		{
			return m_groupMembers.Any(m => m.InCombat);
		}

		/// <summary>
		/// Checks if a living is inside the group
		/// </summary>
		/// <param name="living">GameLiving to check</param>
		/// <returns>true if the player is in the group</returns>
		public virtual bool IsInTheGroup(GameLiving living)
		{
			return m_groupMembers.Contains(living);
		}

		/// <summary>
		///  This is NOT to be used outside of Battelgroup code.
		/// </summary>
		/// <param name="player">Input from battlegroups</param>
		/// <returns>A string of group members</returns>
		public string GroupMemberString(GamePlayer player)
		{
			lock (_groupMembersLock)
			{
				StringBuilder text = new StringBuilder(64); //create the string builder
				text.Length = 0;
				BattleGroup mybattlegroup = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
				foreach (GamePlayer plr in m_groupMembers)
				{
					if (mybattlegroup.IsInTheBattleGroup(plr))
					{
						if ((bool)mybattlegroup.Members[plr] == true)
						{
							text.Append("<Leader> ");
						}
						text.Append("(I)");
					}
					text.Append(plr.Name + " ");
				}
				return text.ToString();
			}
		}

		/// <summary>
		///  This is NOT to be used outside of Battelgroup code.
		/// </summary>
		/// <param name="player">Input from battlegroups</param>
		/// <returns>A string of group members</returns>
		public string GroupMemberClassString(GamePlayer player)
		{
			lock (_groupMembersLock)
			{
				StringBuilder text = new StringBuilder(64); //create the string builder
				text.Length = 0;
				BattleGroup mybattlegroup = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
				foreach (GamePlayer plr in m_groupMembers)
				{
					text.Append($"{plr.Name} ({plr.CharacterClass.Name}) ");
				}
				return text.ToString();
			}
		}

		#endregion
	}
}
