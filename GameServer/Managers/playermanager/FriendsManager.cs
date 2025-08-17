using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Friends
{
	/// <summary>
	/// Game Player Friends List Manager
	/// </summary>
	public sealed class FriendsManager
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Players Indexed Friends Lists Cache.
		/// </summary>
		private ConcurrentDictionary<GamePlayer, string[]> PlayersFriendsListsCache { get; set; } = new();

		/// <summary>
		/// Players Indexed Friends Offline Status Cache.
		/// </summary>
		private ConcurrentDictionary<GamePlayer, FriendStatus[]> PlayersFriendsStatusCache { get; set; } = new();

		/// <summary>
		/// Server Database Reference.
		/// </summary>
		private IObjectDatabase Database { get; set; }

		/// <summary>
		/// Get this Player Friends List
		/// </summary>
		public string[] this[GamePlayer player]
		{
			get
			{
				if (player == null)
					return new string[0];

				string[] result;
				return PlayersFriendsListsCache.TryGetValue(player, out result) ? result : new string[0];
			}
		}

		/// <summary>
		/// Create a new Instance of <see cref="FriendsManager"/>
		/// </summary>
		public FriendsManager(IObjectDatabase database)
		{
			Database = database;
			GameEventMgr.AddHandler(GameClientEvent.StateChanged, OnClientStateChanged);
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, OnPlayerGameEntered);
			GameEventMgr.AddHandler(GamePlayerEvent.Quit, OnPlayerQuit);
			GameEventMgr.AddHandler(GamePlayerEvent.ChangeAnonymous, OnPlayerChangeAnonymous);
		}

		/// <summary>
		/// Add Player to Friends Manager Cache
		/// </summary>
		/// <param name="player">Gameplayer to Add</param>
		public async Task AddPlayerFriendsListToCache(GamePlayer player)
		{
			ArgumentNullException.ThrowIfNull(player);

			string[] friends = player.SerializedFriendsList;
			PlayersFriendsListsCache.TryAdd(player, friends);

			if (friends.Length == 0)
				return;

			IList<DbCoreCharacter> offlineFriends = await DOLDB<DbCoreCharacter>.SelectObjectsAsync(DB.Column("Name").IsIn(friends));
			FriendStatus[] offlineFriendStatus = offlineFriends.Select(chr => new FriendStatus(chr.Name, chr.Level, chr.Class, chr.LastPlayed)).ToArray();
			PlayersFriendsStatusCache.TryAdd(player, offlineFriendStatus);
		}

		/// <summary>
		/// Remove Player from Friends Manager Cache
		/// </summary>
		/// <param name="player">Gameplayer to Remove</param>
		public void RemovePlayerFriendsListFromCache(GamePlayer player)
		{
			if (player == null)
				throw new ArgumentNullException(nameof(player));

			PlayersFriendsListsCache.TryRemove(player, out _);
			PlayersFriendsStatusCache.TryRemove(player, out _);
		}

		/// <summary>
		/// Add a Friend Entry to GamePlayer Friends List.
		/// </summary>
		/// <param name="player">GamePlayer to Add Friend to.</param>
		/// <param name="friend">Friend's Name to Add</param>
		/// <returns>True if friend was added successfully</returns>
		public bool AddFriendToPlayerList(GamePlayer player, string friend)
		{
			if (player == null)
				throw new ArgumentNullException(nameof(player));
			if (friend == null)
				throw new ArgumentNullException(nameof(friend));

			friend = friend.Trim();

			if (string.IsNullOrEmpty(friend))
				throw new ArgumentException("Friend need to be a valid non-empty or white space string!", nameof(friend));

			string[] currentFriendsList;

			while (PlayersFriendsListsCache.TryGetValue(player, out currentFriendsList))
			{
				if (PlayersFriendsListsCache.TryUpdate(player, currentFriendsList.Contains(friend) ? currentFriendsList : currentFriendsList.Concat(new[] { friend }).ToArray(), currentFriendsList))
					break;
			}

			if (currentFriendsList == null)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Cache while trying to Add a new Friend ({1})", player, friend);

				return false;
			}

			player.Out.SendAddFriends(new[] { friend });
			player.SerializedFriendsList = this[player];
			DbCoreCharacter offlineFriend = Database.SelectObjects<DbCoreCharacter>(DB.Column("Name").IsEqualTo(friend)).FirstOrDefault();
			FriendStatus[] currentFriendsStatus;

			if (offlineFriend != null)
			{
				while (PlayersFriendsStatusCache.TryGetValue(player, out currentFriendsStatus))
				{
					if (PlayersFriendsStatusCache.TryUpdate(player, currentFriendsStatus.Where(frd => frd.Name != friend)
																						.Concat(new[] { new FriendStatus(offlineFriend.Name, offlineFriend.Level, offlineFriend.Class, offlineFriend.LastPlayed) })
																						.ToArray(), currentFriendsStatus))
						break;
				}

				if (currentFriendsStatus == null)
				{
					if (log.IsWarnEnabled)
						log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Status Cache while trying to Add a new Friend ({1})", player, friend);
				}
			}

			return true;
		}

		/// <summary>
		/// Remove a Friend Entry from GamePlayer Friends List.
		/// </summary>
		/// <param name="player">GamePlayer to Add Friend to.</param>
		/// <param name="friend">Friend's Name to Add</param>
		/// <returns>True if friend was added successfully</returns>
		public bool RemoveFriendFromPlayerList(GamePlayer player, string friend)
		{
			if (player == null)
				throw new ArgumentNullException(nameof(player));
			if (friend == null)
				throw new ArgumentNullException(nameof(friend));

			friend = friend.Trim();

			if (string.IsNullOrEmpty(friend))
				throw new ArgumentException("Friend need to be a valid non-empty or white space string!", nameof(friend));

			string[] currentFriendsList;

			while (PlayersFriendsListsCache.TryGetValue(player, out currentFriendsList))
			{
				if (PlayersFriendsListsCache.TryUpdate(player, currentFriendsList.Except(new[] { friend }, StringComparer.OrdinalIgnoreCase).ToArray(), currentFriendsList))
					break;
			}

			if (currentFriendsList == null)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Cache while trying to Remove a Friend ({1})", player, friend);

				return false;
			}

			player.Out.SendRemoveFriends(new[] { friend });
			player.SerializedFriendsList = this[player];
			FriendStatus[] currentFriendStatus;

			while (PlayersFriendsStatusCache.TryGetValue(player, out currentFriendStatus))
			{
				if (PlayersFriendsStatusCache.TryUpdate(player, currentFriendStatus.Where(frd => frd.Name != friend).ToArray(), currentFriendStatus))
					break;
			}

			if (currentFriendStatus == null)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Status Cache while trying to Remove a Friend ({1})", player, friend);
			}

			return true;
		}

		/// <summary>
		/// Send Players Friends List Snapshot
		/// </summary>
		/// <param name="player">GamePlayer to Send Friends snapshot to</param>
		public void SendPlayerFriendsSnapshot(GamePlayer player)
		{
			if (player == null)
				throw new ArgumentNullException(nameof(player));

			player.Out.SendCustomTextWindow("Friends (snapshot)", this[player]);
		}

		/// <summary>
		/// Send Players Friends Social Windows
		/// </summary>
		/// <param name="player">GamePlayer to Send Friends social window to</param>
		public void SendPlayerFriendsSocial(GamePlayer player)
		{
			if (player == null)
				throw new ArgumentNullException(nameof(player));

			// "TF" - clear friend list in social
			player.Out.SendMessage("TF", eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);

			var offlineFriends = this[player].ToList();
			var index = 0;
			foreach (var friend in this[player].Select(name => PlayersFriendsListsCache.FirstOrDefault(kv => kv.Key.Name == name))
					 .Where(kv => kv.Key != null && !kv.Key.IsAnonymous && kv.Key.Realm == player.Realm).Select(kv => kv.Key))
			{
				offlineFriends.Remove(friend.Name);
				player.Out.SendMessage(string.Format("F,{0},{1},{2},{3},\"{4}\"",
					index++,
					friend.Name,
					friend.Level,
					friend.CharacterClass.ID,
					friend.CurrentZone == null ? string.Empty : friend.CurrentZone.Description),
					eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
			}

			// Query Offline Characters
			FriendStatus[] offline;

			if (PlayersFriendsStatusCache.TryGetValue(player, out offline))
			{
				foreach (var friend in offline.Where(frd => offlineFriends.Contains(frd.Name)))
				{
					player.Out.SendMessage(string.Format("F,{0},{1},{2},{3},\"{4}\"",
						index++,
						friend.Name,
						friend.Level,
						friend.ClassID,
						friend.LastPlayed),
						eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
				}
			}
		}

		/// <summary>
		/// Send Initial Player Friends List to Client
		/// </summary>
		/// <param name="player">GamePlayer to send the list to.</param>
		private bool SendPlayerFriendsList(GamePlayer player)
		{
			player.Out.SendAddFriends(this[player].Where(name =>
			{
				var pair = PlayersFriendsListsCache.FirstOrDefault(kv => kv.Key != null && kv.Key.Name == name);
				return pair.Key != null && !pair.Key.IsAnonymous;
			}).ToArray());
			return true;
		}

		/// <summary>
		/// Notify Friends of this Player that he entered Game
		/// </summary>
		/// <param name="player">GamePlayer to notify to friends</param>
		private void NotifyPlayerFriendsEnteringGame(GamePlayer player)
		{
			var playerName = player.Name;
			var playerUpdate = new[] { playerName };

			foreach (GamePlayer friend in PlayersFriendsListsCache.Where(kv => kv.Value.Contains(playerName)).Select(kv => kv.Key))
			{
				friend.Out.SendAddFriends(playerUpdate);
			}
		}

		/// <summary>
		/// Notify Friends of this Player that he exited Game
		/// </summary>
		/// <param name="player">GamePlayer to notify to friends</param>
		private void NotifyPlayerFriendsExitingGame(GamePlayer player)
		{
			var playerName = player.Name;
			var playerUpdate = new[] { playerName };

			foreach (GamePlayer friend in PlayersFriendsListsCache.Where(kv => kv.Value.Contains(playerName)).Select(kv => kv.Key))
			{
				friend.Out.SendRemoveFriends(playerUpdate);
			}

			var offline = new FriendStatus(player.Name, player.Level, player.CharacterClass.ID, DateTime.Now);

			foreach (var cache in PlayersFriendsStatusCache.Where(kv => kv.Value.Any(frd => frd.Name == player.Name)).ToArray())
				PlayersFriendsStatusCache[cache.Key] = cache.Value.Where(frd => frd.Name != player.Name).Concat(new[] { offline }).ToArray();
		}

		/// <summary>
		/// Trigger Player Friend List Update on World Enter
		/// </summary>
		private async void OnClientStateChanged(DOLEvent e, object sender, EventArgs arguments)
		{
			if (sender is not GameClient client || client.ClientState is not GameClient.eClientState.Playing || client.Player == null)
				return;

			await AddPlayerFriendsListToCache(client.Player);
			SendPlayerFriendsList(client.Player);
		}

		/// <summary>
		/// Trigger Player's Friends Notice on Game Enter
		/// </summary>
		private void OnPlayerGameEntered(DOLEvent e, object sender, EventArgs arguments)
		{
			var player = sender as GamePlayer;
			if (player == null)
				return;

			if (!player.IsAnonymous)
				NotifyPlayerFriendsEnteringGame(player);
		}

		/// <summary>
		/// Trigger Player's Friends Notice on Game Leave, And Cleanup Player Friend List
		/// </summary>
		private void OnPlayerQuit(DOLEvent e, object sender, EventArgs arguments)
		{
			var player = sender as GamePlayer;
			if (player == null)
				return;

			RemovePlayerFriendsListFromCache(player);
			if (!player.IsAnonymous)
				NotifyPlayerFriendsExitingGame(player);
		}

		/// <summary>
		/// Trigger Player's Friends Notice on Anonymous State Change
		/// </summary>
		private void OnPlayerChangeAnonymous(DOLEvent e, object sender, EventArgs arguments)
		{
			var player = sender as GamePlayer;
			if (player == null)
				return;

			if (player.IsAnonymous)
				NotifyPlayerFriendsExitingGame(player);
			else
				NotifyPlayerFriendsEnteringGame(player);
		}
	}
}