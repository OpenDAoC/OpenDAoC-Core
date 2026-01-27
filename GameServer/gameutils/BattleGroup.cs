using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using static DOL.GS.IGameStaticItemOwner;
using DOL.GS;
using DOL.Timing;
using DOL.GS.Housing;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS
{
	public class BattleGroup : IGameStaticItemOwner
	{
		public const string BATTLEGROUP_PROPERTY = "battlegroup";
		public readonly Lock Lock = new();

		protected HybridDictionary m_battlegroupMembers = new();
		protected readonly Lock _battlegroupMembersLock = new();
		protected GamePlayer m_battlegroupLeader;
		private string m_bgID;
		public string BgID
		{
			get
			{
				if (string.IsNullOrEmpty(m_bgID))
					m_bgID = Guid.NewGuid().ToString().Substring(0, 8);
				return m_bgID;
			}
		}
		protected string m_purpose = "";
		protected int m_minutesToStart = -1;
		protected ECSGameTimer m_startTimer;
		protected string m_meetingPlace = "";

		protected bool m_lootChestEnabled = false;
		protected long m_lootChestMoney = 0;
		private BGLootInventory m_lootChestInventory;
		protected GameLootChest m_currentChestNPC;

		protected List<GamePlayer> m_battlegroupModerators = new();
		protected bool battlegroupLootType = false;
		protected GamePlayer battlegroupTreasurer = null;
		protected int battlegroupLootTypeThreshold = 0;

		public BattleGroup()
		{
			battlegroupLootType = false;
			battlegroupTreasurer = null;
		}

		#region Properties
		public GameLiving Leader => m_battlegroupLeader;
		public HybridDictionary Members { get => m_battlegroupMembers; set => m_battlegroupMembers = value; }
		public List<GamePlayer> Moderators { get => m_battlegroupModerators; set => m_battlegroupModerators = value; }
		protected Dictionary<string, int> m_savedBeams = new Dictionary<string, int>();

		public bool IsPublic { get; set; } = true;
		public string Password { get; set; } = string.Empty;
		public string Purpose { get => m_purpose; set => m_purpose = value; }
		public int MinutesToStart { get => m_minutesToStart; set => m_minutesToStart = value; }
		public string MeetingPlace { get => m_meetingPlace; set => m_meetingPlace = value; }

		private bool m_listen = false;
		public bool Listen { get => m_listen; set => m_listen = value; }

		public bool LootChestEnabled { get => m_lootChestEnabled; set => m_lootChestEnabled = value; }
		public long LootChestMoney { get => m_lootChestMoney; set => m_lootChestMoney = value; }
		protected Dictionary<GamePlayer, ushort> m_activeBeams = new Dictionary<GamePlayer, ushort>();

		public BGLootInventory LootChestInventory
		{
			get
			{
				if (m_lootChestInventory == null && Leader is GamePlayer leaderPlayer)
					m_lootChestInventory = new BGLootInventory(this);
				return m_lootChestInventory;
			}
		}

		public int PlayerCount => m_battlegroupMembers.Count;
		public string Name => $"{(Leader == null ? "Empty" : Leader.Name + "'s")} Battlegroup (Size: {PlayerCount})";
		public object GameStaticItemOwnerComparand => null;
		#endregion

		#region Member Management
		public virtual bool AddBattlePlayer(GamePlayer player, bool leader)
		{
			if (player == null) return false;
			lock (_battlegroupMembersLock)
			{
				if (m_battlegroupMembers.Contains(player)) return false;

				player.TempProperties.SetProperty(BATTLEGROUP_PROPERTY, this);
				player.isInBG = true;

				m_battlegroupMembers.Add(player, leader);
				if (leader) m_battlegroupLeader = player;

				player.Out.SendMessage("You join the battle group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				BroadcastNormalMessage($"{player.Name} joined the battle group.");
				RefreshBeamsForPlayer(player);
			}
			return true;
		}

		public virtual bool IsInTheBattleGroup(GamePlayer player)
		{
			if (player == null) return false;
			lock (_battlegroupMembersLock)
			{
				return m_battlegroupMembers.Contains(player);
			}
		}

		public virtual bool RemoveBattlePlayer(GamePlayer player)
		{
			if (player == null) return false;

			lock (_battlegroupMembersLock)
			{
				if (!m_battlegroupMembers.Contains(player))
					return false;

				// Remove Beam Logic
				if (m_battlegroupMembers.Count <= 2)
				{
					ArrayList currentMembers = new ArrayList(m_battlegroupMembers.Keys);
					foreach (GamePlayer p in currentMembers)
					{
						this.ApplyBeam("remove", p);
					}
				}
				else
				{
					this.ApplyBeam("remove", player);
				}

				var leader = IsBGLeader(player);
				m_battlegroupMembers.Remove(player);
				player.TempProperties.RemoveProperty(BATTLEGROUP_PROPERTY);
				player.isInBG = false;

				// Cleanup Treasurer/Moderators & Icons
				if (battlegroupTreasurer == player) SetBGTreasurer(null);
				if (m_battlegroupModerators.Contains(player)) m_battlegroupModerators.Remove(player);

				player.Out.SendMinotaurRelicMapRemove(9);
				player.Out.SendMinotaurRelicMapRemove(19);
				player.Out.SendMinotaurRelicMapRemove(29);

				player.Out.SendMessage("You leave the battle group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

				foreach (GamePlayer member in m_battlegroupMembers.Keys)
				{
					member.Out.SendMessage(player.Name + " has left the battle group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}

				if (m_battlegroupMembers.Count == 1)
				{
					ArrayList lastPlayers = new ArrayList(m_battlegroupMembers.Count);
					lastPlayers.AddRange(m_battlegroupMembers.Keys);
					foreach (GamePlayer plr in lastPlayers)
					{
						RemoveBattlePlayer(plr);
					}
					m_battlegroupLeader = null;

					// Loot Chest Cleanup
					if (LootChestEnabled)
					{
						if (m_currentChestNPC != null)
						{
							m_currentChestNPC.Delete();
							m_currentChestNPC = null;
						}
						m_lootChestInventory = null;
						LootChestEnabled = false;
					}
				}
				else if (leader && m_battlegroupMembers.Count >= 2)
				{
					var bgPlayers = new ArrayList(m_battlegroupMembers.Keys);
					var nextLeader = m_battlegroupMembers.Keys.Cast<GamePlayer>().FirstOrDefault(p => p != null);

					if (nextLeader != null)
					{
						SetBGLeader(nextLeader);
						m_battlegroupMembers[nextLeader] = true;

						if (LootChestEnabled) m_lootChestInventory = null;

						foreach (GamePlayer member in m_battlegroupMembers.Keys)
						{
							member.Out.SendMessage(nextLeader.Name + " is the new leader of the battle group.", eChatType.CT_BattleGroupLeader, eChatLoc.CL_SystemWindow);
						}
					}
				}
			}
			return true;
		}

		public bool IsBGLeader(GameLiving living) => m_battlegroupLeader != null && living != null && m_battlegroupLeader == living;
		public bool IsBGTreasurer(GameLiving living) => battlegroupTreasurer != null && living != null && battlegroupTreasurer == living;
		public bool IsBGModerator(GamePlayer player) => player != null && m_battlegroupModerators.Contains(player);

		public bool SetBGLeader(GamePlayer player)
		{
			if (player == null) return false;
			m_battlegroupLeader = player;
			return true;
		}
		#endregion

		#region Treasurer Logic (Personal Collection)
		public bool GetBGLootType() => battlegroupLootType;
		public GamePlayer GetBGTreasurer() => battlegroupTreasurer;
		public void SetBGTreasurer(GamePlayer treasurer)
		{
			battlegroupTreasurer = treasurer;
			battlegroupLootType = (treasurer != null);
		}

		public int GetBGLootTypeThreshold() => battlegroupLootTypeThreshold;
		public void SetBGLootTypeThreshold(int thresh) => battlegroupLootTypeThreshold = Math.Clamp(thresh, 0, 50);
		public void SetBGLootType(bool type)
		{
			battlegroupLootType = type;
		}


		/// <summary>
		/// Lootchest & Treasurer Money Management
		/// </summary>
		public TryPickUpResult TryAutoPickUpMoney(GameMoney money)
		{
			if (money == null || money.Value <= 0) return TryPickUpResult.DoesNotWant;

			// Lootchest Money
			if (LootChestEnabled)
			{
				lock (Lock)
				{
					m_lootChestMoney += money.Value;
				}
				// Money Message
				BroadcastNormalMessage($"{Money.GetString(money.Value)} were been added to the loot chest.");
				money.RemoveFromWorld();
				return TryPickUpResult.Success;
			}

			// Treasurer Money
			if (battlegroupLootType && battlegroupTreasurer != null)
			{
				battlegroupTreasurer.AddMoney(money.Value);
				battlegroupTreasurer.Out.SendMessage($"[Treasurer] You picked up {Money.GetString(money.Value)}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				money.RemoveFromWorld();
				return TryPickUpResult.Success;
			}

			return TryPickUpResult.DoesNotWant;
		}

		public TryPickUpResult TryPickUpMoney(GamePlayer source, GameMoney money) => TryAutoPickUpMoney(money);

		/// <summary>
		/// Lootchest & Treasurer Item Management
		/// </summary>
		public TryPickUpResult TryAutoPickUpItem(WorldInventoryItem worldItem)
			=> TryPickUpItem(battlegroupTreasurer, worldItem);

		public TryPickUpResult TryPickUpItem(GamePlayer source, WorldInventoryItem worldItem)
		{
			if (worldItem == null || worldItem.Item == null) return TryPickUpResult.DoesNotWant;
			worldItem.AssertLockAcquisition();

			if (worldItem.IsPlayerDiscarded) return TryPickUpResult.DoesNotWant;

			// Lootchest
			if (LootChestEnabled)
			{
				GameInventoryItem chestItem = new GameInventoryItem(worldItem.Item);
				string bgOwnerID = "BG_ID_" + this.BgID;
				chestItem.OwnerID = bgOwnerID;
				chestItem.ObjectId = null;

				var itemsInChest = GameServer.Database.SelectObjects<DbInventoryItem>(DB.Column("OwnerID").IsEqualTo(bgOwnerID));

				int freeSlot = -1;
				for (int i = 2500; i <= 2599; i++)
				{
					if (!itemsInChest.Any(it => it.SlotPosition == i))
					{
						freeSlot = i;
						break;
					}
				}

				if (freeSlot != -1)
				{
					chestItem.SlotPosition = freeSlot;
					GameServer.Database.AddObject(chestItem);
					m_lootChestInventory = null;
					// Item Message
					BroadcastNormalMessage($"{worldItem.Item.Name} has been stored in the loot chest.");
					worldItem.RemoveFromWorld();
					return TryPickUpResult.Success;
				}
				else
				{
					BroadcastMessage("[Lootchest] The chest is full (max 100 items)!"); // Send to battlegroup, shouldnt happen
					return TryPickUpResult.Blocked;
				}
			}

			// Treasurer
			if (battlegroupLootType && battlegroupTreasurer != null)
			{
				if (battlegroupTreasurer.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, worldItem.Item))
				{
					battlegroupTreasurer.Out.SendMessage($"[Treasurer] You picked up: {worldItem.Item.Name}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					worldItem.RemoveFromWorld();
					return TryPickUpResult.Success;
				}

				battlegroupTreasurer.Out.SendMessage("[Treasurer] Backpack full!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return TryPickUpResult.Blocked;
			}

			return TryPickUpResult.DoesNotWant;
		}
		#endregion

		#region Lootchest Logic
		public void AddToLootChest(DbInventoryItem dbItem)
		{
			if (dbItem == null || Leader == null) return;
			string bgOwnerID = "BG_ID_" + this.BgID;

			var itemsInChest = GameServer.Database.SelectObjects<DbInventoryItem>(DB.Column("OwnerID").IsEqualTo(bgOwnerID));
			if (itemsInChest.Count >= 100) return;

			int freeSlot = -1;
			for (int i = 0; i <= 99; i++)
			{
				if (!itemsInChest.Any(it => it.SlotPosition == i)) { freeSlot = i; break; }
			}

			if (freeSlot != -1)
			{
				dbItem.OwnerID = bgOwnerID;
				dbItem.SlotPosition = freeSlot;
				GameServer.Database.SaveObject(dbItem);
				m_lootChestInventory = null;
			}
		}

		public void SpawnChest(GamePlayer leader)
		{
			// Remove Lootchest from world, when already spawned
			if (m_currentChestNPC != null)
			{
				m_currentChestNPC.Delete();
				m_currentChestNPC = null;
				return;
			}

			m_currentChestNPC = new GameLootChest(this)
			{
				Model = 2255,
				Name = "Lootchest",
				Size = 30,
				GuildName = leader.Name + "'s Battelgroup",
				Realm = leader.Realm,
				CurrentRegionID = leader.CurrentRegionID,
				X = leader.X,
				Y = leader.Y,
				Z = leader.Z,
				Heading = leader.Heading
			};
			m_currentChestNPC.AddToWorld();
		}
		public void HandoutLoot(GamePlayer leader)
		{
			if (leader == null || !IsBGLeader(leader)) return;

			string bgOwnerID = "BG_ID_" + this.BgID;
			var dbItemsList = GameServer.Database.SelectObjects<DbInventoryItem>(DB.Column("OwnerID").IsEqualTo(bgOwnerID));

			lock (_battlegroupMembersLock)
			{
				var activeMembers = m_battlegroupMembers.Keys.Cast<GamePlayer>()
					.Where(p => p?.Client != null && p.ObjectState == GameObject.eObjectState.Active).ToList();

				if (activeMembers.Count == 0) return;

				if (dbItemsList != null && dbItemsList.Count > 0)
				{
					Random rnd = new();
					foreach (var dbItem in dbItemsList)
					{
						var winner = activeMembers[rnd.Next(activeMembers.Count)];

						if (winner.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, dbItem))
						{
							dbItem.OwnerID = winner.InternalID;
							GameServer.Database.SaveObject(dbItem);

							foreach (GamePlayer ply in m_battlegroupMembers.Keys)
							{
								ply.Out.SendMessage($"{winner.Name} received {dbItem.Name}.", eChatType.CT_BattleGroupLeader, eChatLoc.CL_SystemWindow);
							}
						}
						else
						{
							winner.Out.SendMessage($"[Battlegroup] Your backpack is full! {dbItem.Name} remains in the chest.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						}
					}
				}

				if (m_lootChestMoney > 0)
				{
					long share = m_lootChestMoney / activeMembers.Count;
					if (share > 0)
					{
						activeMembers.ForEach(p => p.AddMoney(share));
						foreach (GamePlayer ply in m_battlegroupMembers.Keys)
						{
							ply.Out.SendMessage($"[Battlegroup] The money was splitted, everyone received: {Money.GetString(share)}.", eChatType.CT_BattleGroupLeader, eChatLoc.CL_SystemWindow);
						}
					}
					m_lootChestMoney = 0;
				}

				m_lootChestInventory = null;
			}

			if (m_currentChestNPC != null)
			{
				m_currentChestNPC.Delete();
				m_currentChestNPC = null;
			}
			LootChestEnabled = false;
		}
		#endregion

		#region Utilities & Timer
		public void StartCountdown(int minutes)
		{
			if (m_startTimer != null) m_startTimer.Stop();
			m_minutesToStart = minutes;
			BroadcastMessage($"The Battlegroup starts in {m_minutesToStart} minute(s)!");
			m_minutesToStart--;

			m_startTimer = new ECSGameTimer(this.Leader, (timer) =>
			{
				if (m_minutesToStart <= 0)
				{
					BroadcastMessage("The Battlegroup is starting any moment!");
					m_minutesToStart = -1;
					return 0;
				}
				if (m_minutesToStart % 5 == 0 || m_minutesToStart <= 2)
					BroadcastMessage($"The Battlegroup starts in {m_minutesToStart} minute(s)!");

				m_minutesToStart--;
				return 60000;
			});
			m_startTimer.Start(60000);
		}

		public void BroadcastMessage(string msg)
		{
			lock (_battlegroupMembersLock)
			{
				foreach (GamePlayer ply in m_battlegroupMembers.Keys)
				{
					if (ply?.Client == null) continue;
					ply.Out.SendMessage(msg, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					ply.Out.SendMessage("[Battlegroup]: " + msg, eChatType.CT_BattleGroupLeader, eChatLoc.CL_SystemWindow);
				}
			}
		}

		public void BroadcastNormalMessage(string msg)
		{
			lock (_battlegroupMembersLock)
			{
				foreach (GamePlayer ply in m_battlegroupMembers.Keys)
				{
					if (ply?.Client == null) continue;
					ply.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
		}
		#endregion

		#region Inner Classes
		public class BGLootInventory : AccountVault
		{
			private readonly BattleGroup m_bg;
			public BGLootInventory(BattleGroup bg)
				: base(bg.Leader as GamePlayer, 0, AccountVaultKeeper.GetDummyVaultItem(bg.Leader as GamePlayer))
			{
				m_bg = bg;
			}
			public override string GetOwner(GamePlayer player)
			{
				if (m_bg != null)
				{
					return "BG_ID_" + m_bg.BgID;
				}
				if (player != null)
				{
					var tempBG = player.TempProperties.GetProperty<BattleGroup>(BATTLEGROUP_PROPERTY);
					if (tempBG != null) return "BG_ID_" + tempBG.BgID;
				}

				return "BG_Unknown";
			}
			public override bool CanView(GamePlayer player) => true;
			public override bool CanAddItems(GamePlayer player) => true;
			public override bool CanRemoveItems(GamePlayer player)
			{
				var bg = player.TempProperties.GetProperty<BattleGroup>(BATTLEGROUP_PROPERTY);
				return bg != null && bg.IsBGLeader(player);
			}

			public override bool OnAddItem(GamePlayer player, DbInventoryItem item, int previousSlot)
			{
				bool success = base.OnAddItem(player, item, previousSlot);

				if (success && m_bg != null && item != null && player != null)
				{
					foreach (GamePlayer ply in m_bg.m_battlegroupMembers.Keys)
					{
						ply.Out.SendMessage($"[Battlegroup] {player.Name} added {item.Name} to the loot chest.", eChatType.CT_BattleGroupLeader, eChatLoc.CL_SystemWindow);
					}
				}
				return success;
			}

			public override bool OnRemoveItem(GamePlayer player, DbInventoryItem item, int previousSlot)
			{
				bool success = base.OnRemoveItem(player, item, previousSlot);

				if (success && m_bg != null && item != null && player != null)
				{
					foreach (GamePlayer ply in m_bg.m_battlegroupMembers.Keys)
					{
						ply.Out.SendMessage($"[Battlegroup] {player.Name} removed {item.Name} from the loot chest.", eChatType.CT_BattleGroupLeader, eChatLoc.CL_SystemWindow);
					}
				}
				return success;
			}
		}

		// Loot Chest Interface
		public class GameLootChest : GameNPC
		{
			private BattleGroup m_bg;
			public GameLootChest(BattleGroup bg) => m_bg = bg;

			public override bool Interact(GamePlayer player)
			{
				if (!base.Interact(player)) return false;

				if (player == null || m_bg == null || !m_bg.IsInTheBattleGroup(player))
				{
					player.Out.SendMessage("You are not a member of this Battlegroup.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return false;
				}

				player.ActiveInventoryObject = m_bg.LootChestInventory;

				if (player.ActiveInventoryObject != null)
				{
					player.Out.SendInventoryItemsUpdate(player.ActiveInventoryObject.GetClientInventory(player), eInventoryWindowType.HouseVault);
				}

				if (m_bg.LootChestMoney > 0)
					player.Out.SendMessage($"The loot chest holds: {Money.GetString(m_bg.LootChestMoney)}", eChatType.CT_System, eChatLoc.CL_SystemWindow);

				return true;
			}
		}
		#endregion


		/// <summary>
		/// Applies or removes a visual relic beam effect on a target player.
		/// This implementation follows the MinotaurRelic script logic.
		/// </summary>
		public void ApplyBeam(string action, GamePlayer target)
		{
			if (target == null) return;

			int relicEffectID = 0;
			bool active = true;

			if (string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase))
			{
				relicEffectID = 0;
				active = false;
				m_savedBeams.Remove(target.InternalID);
			}
			else
			{
				switch (action.ToLower())
				{
					case "red": relicEffectID = 159; break;
					case "white": relicEffectID = 160; break;
					case "yellow":
					case "gold": relicEffectID = 161; break;
					default: return;
				}
				m_savedBeams[target.InternalID] = relicEffectID;
			}

			GamePlayer[] members;
			lock (_battlegroupMembersLock)
			{
				members = m_battlegroupMembers.Keys.Cast<GamePlayer>().ToArray();
			}

			ushort targetActiveId = (ushort)relicEffectID;

			foreach (GamePlayer member in members)
			{
				if (member?.Out == null || member.Realm != target.Realm)
					continue;

					member.Out.SendMinotaurRelicWindow(target, targetActiveId, active);
			}
		}

		// Still have beams enabled after release, teleport or whatever
		public void RefreshBeamsForPlayer(GamePlayer player)
		{
			if (player?.Out == null) return;

			lock (_battlegroupMembersLock)
			{
				foreach (var entry in m_savedBeams)
				{
					string ownerID = entry.Key;
					int effectID = entry.Value;

					foreach (GamePlayer member in m_battlegroupMembers.Keys)
					{
						if (member.InternalID == ownerID)
						{
							player.Out.SendMinotaurRelicWindow(member, effectID, true);
							break;
						}
					}
				}

				if (m_savedBeams.TryGetValue(player.InternalID, out int selfEffectID))
				{
					foreach (GamePlayer member in m_battlegroupMembers.Keys)
					{
						if (member?.Out != null)
						{
							member.Out.SendMinotaurRelicWindow(player, selfEffectID, true);
						}
					}
				}
			}
		}
	}
}