using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;

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
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
		public FriendsManager(IObjectDatabase Database)
		{
			this.Database = Database;
			GameEventMgr.AddHandler(GameClientEvent.StateChanged, OnClientStateChanged);
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, OnPlayerGameEntered);
			GameEventMgr.AddHandler(GamePlayerEvent.Quit, OnPlayerQuit);
			GameEventMgr.AddHandler(GamePlayerEvent.ChangeAnonymous, OnPlayerChangeAnonymous);
		}

		/// <summary>
		/// Add Player to Friends Manager Cache
		/// </summary>
		/// <param name="Player">Gameplayer to Add</param>
		public void AddPlayerFriendsListToCache(GamePlayer Player)
		{
			if (Player == null)
				throw new ArgumentNullException(nameof(Player));

			string[] friends = Player.SerializedFriendsList;
			PlayersFriendsListsCache.TryAdd(Player, friends);
			FriendStatus[] offlineFriends = Array.Empty<FriendStatus>();

			if (friends.Any())
				offlineFriends = Database.SelectObjects<DOLCharacters>(DB.Column("Name").IsIn(friends)).Select(chr => new FriendStatus(chr.Name, chr.Level, chr.Class, chr.LastPlayed)).ToArray();

			PlayersFriendsStatusCache.TryAdd(Player, offlineFriends);
		}

		/// <summary>
		/// Remove Player from Friends Manager Cache
		/// </summary>
		/// <param name="Player">Gameplayer to Remove</param>
		public void RemovePlayerFriendsListFromCache(GamePlayer Player)
		{
			if (Player == null)
				throw new ArgumentNullException(nameof(Player));

			PlayersFriendsListsCache.TryRemove(Player, out _);
			PlayersFriendsStatusCache.TryRemove(Player, out _);
		}

		/// <summary>
		/// Add a Friend Entry to GamePlayer Friends List.
		/// </summary>
		/// <param name="Player">GamePlayer to Add Friend to.</param>
		/// <param name="Friend">Friend's Name to Add</param>
		/// <returns>True if friend was added successfully</returns>
		public bool AddFriendToPlayerList(GamePlayer Player, string Friend)
		{
			if (Player == null)
				throw new ArgumentNullException(nameof(Player));
			if (Friend == null)
				throw new ArgumentNullException(nameof(Friend));

			Friend = Friend.Trim();

			if (string.IsNullOrEmpty(Friend))
				throw new ArgumentException("Friend need to be a valid non-empty or white space string!", nameof(Friend));

			string[] currentFriendsList;

			while (PlayersFriendsListsCache.TryGetValue(Player, out currentFriendsList))
			{
				if (PlayersFriendsListsCache.TryUpdate(Player, currentFriendsList.Contains(Friend) ? currentFriendsList : currentFriendsList.Concat(new[] { Friend }).ToArray(), currentFriendsList))
					break;
			}

			if (currentFriendsList == null)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Cache while trying to Add a new Friend ({1})", Player, Friend);

				return false;
			}

			Player.Out.SendAddFriends(new[] { Friend });
			Player.SerializedFriendsList = this[Player];
			DOLCharacters offlineFriend = Database.SelectObjects<DOLCharacters>(DB.Column("Name").IsEqualTo(Friend)).FirstOrDefault();
			FriendStatus[] currentFriendsStatus;


			if (offlineFriend != null)
			{
				while (PlayersFriendsStatusCache.TryGetValue(Player, out currentFriendsStatus))
				{
					if (PlayersFriendsStatusCache.TryUpdate(Player, currentFriendsStatus.Where(frd => frd.Name != Friend)
																						.Concat(new[] { new FriendStatus(offlineFriend.Name, offlineFriend.Level, offlineFriend.Class, offlineFriend.LastPlayed) })
																						.ToArray(), currentFriendsStatus))
						break;
				}

				if (currentFriendsStatus == null)
				{
					if (log.IsWarnEnabled)
						log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Status Cache while trying to Add a new Friend ({1})", Player, Friend);
				}
			}

			return true;
		}

		/// <summary>
		/// Remove a Friend Entry from GamePlayer Friends List.
		/// </summary>
		/// <param name="Player">GamePlayer to Add Friend to.</param>
		/// <param name="Friend">Friend's Name to Add</param>
		/// <returns>True if friend was added successfully</returns>
		public bool RemoveFriendFromPlayerList(GamePlayer Player, string Friend)
		{
			if (Player == null)
				throw new ArgumentNullException(nameof(Player));
			if (Friend == null)
				throw new ArgumentNullException(nameof(Friend));

			Friend = Friend.Trim();

			if (string.IsNullOrEmpty(Friend))
				throw new ArgumentException("Friend need to be a valid non-empty or white space string!", nameof(Friend));

			string[] currentFriendsList;

			while (PlayersFriendsListsCache.TryGetValue(Player, out currentFriendsList))
			{
				if (PlayersFriendsListsCache.TryUpdate(Player, currentFriendsList.Except(new[] { Friend }, StringComparer.OrdinalIgnoreCase).ToArray(), currentFriendsList))
					break;
			}

			if (currentFriendsList == null)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Cache while trying to Remove a Friend ({1})", Player, Friend);

				return false;
			}

			Player.Out.SendRemoveFriends(new[] { Friend });
			Player.SerializedFriendsList = this[Player];
			FriendStatus[] currentFriendStatus;

			while (PlayersFriendsStatusCache.TryGetValue(Player, out currentFriendStatus))
			{
				if (PlayersFriendsStatusCache.TryUpdate(Player, currentFriendStatus.Where(frd => frd.Name != Friend).ToArray(), currentFriendStatus))
					break;
			}

			if (currentFriendStatus == null)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Status Cache while trying to Remove a Friend ({1})", Player, Friend);
			}

			return true;
		}

		/// <summary>
		/// Send Players Friends List Snapshot
		/// </summary>
		/// <param name="Player">GamePlayer to Send Friends snapshot to</param>
		public void SendPlayerFriendsSnapshot(GamePlayer Player)
		{
			if (Player == null)
				throw new ArgumentNullException(nameof(Player));

			Player.Out.SendCustomTextWindow("Friends (snapshot)", this[Player]);
		}

		/// <summary>
		/// Send Players Friends Social Windows
		/// </summary>
		/// <param name="Player">GamePlayer to Send Friends social window to</param>
		public void SendPlayerFriendsSocial(GamePlayer Player)
		{
			if (Player == null)
				throw new ArgumentNullException(nameof(Player));

			// "TF" - clear friend list in social
			Player.Out.SendMessage("TF", eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);

			var offlineFriends = this[Player].ToList();
			var index = 0;
			foreach (var friend in this[Player].Select(name => PlayersFriendsListsCache.FirstOrDefault(kv => kv.Key.Name == name))
					 .Where(kv => kv.Key != null && !kv.Key.IsAnonymous && kv.Key.Realm == Player.Realm).Select(kv => kv.Key))
			{
				offlineFriends.Remove(friend.Name);
				Player.Out.SendMessage(string.Format("F,{0},{1},{2},{3},\"{4}\"",
					index++,
					friend.Name,
					friend.Level,
					friend.CharacterClass.ID,
					friend.CurrentZone == null ? string.Empty : friend.CurrentZone.Description),
					eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
			}

			// Query Offline Characters
			FriendStatus[] offline;

			if (PlayersFriendsStatusCache.TryGetValue(Player, out offline))
			{
				foreach (var friend in offline.Where(frd => offlineFriends.Contains(frd.Name)))
				{
					Player.Out.SendMessage(string.Format("F,{0},{1},{2},{3},\"{4}\"",
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
		/// <param name="Player">GamePlayer to send the list to.</param>
		private void SendPlayerFriendsList(GamePlayer Player)
		{
			Player.Out.SendAddFriends(this[Player].Where(name => {
				var player = PlayersFriendsListsCache.FirstOrDefault(kv => kv.Key != null && kv.Key.Name == name);
				return player.Key != null && !player.Key.IsAnonymous;
			}).ToArray());
		}

		/// <summary>
		/// Notify Friends of this Player that he entered Game
		/// </summary>
		/// <param name="Player">GamePlayer to notify to friends</param>
		private void NotifyPlayerFriendsEnteringGame(GamePlayer Player)
		{
			var playerName = Player.Name;
			var playerUpdate = new[] { playerName };

			foreach (GamePlayer friend in PlayersFriendsListsCache.Where(kv => kv.Value.Contains(playerName)).Select(kv => kv.Key))
			{
				friend.Out.SendAddFriends(playerUpdate);
			}
		}

		/// <summary>
		/// Notify Friends of this Player that he exited Game
		/// </summary>
		/// <param name="Player">GamePlayer to notify to friends</param>
		private void NotifyPlayerFriendsExitingGame(GamePlayer Player)
		{
			var playerName = Player.Name;
			var playerUpdate = new[] { playerName };

			foreach (GamePlayer friend in PlayersFriendsListsCache.Where(kv => kv.Value.Contains(playerName)).Select(kv => kv.Key))
			{
				friend.Out.SendRemoveFriends(playerUpdate);
			}

			var offline = new FriendStatus(Player.Name, Player.Level, Player.CharacterClass.ID, DateTime.Now);

			foreach (var cache in PlayersFriendsStatusCache.Where(kv => kv.Value.Any(frd => frd.Name == Player.Name)).ToArray())
				PlayersFriendsStatusCache[cache.Key] = cache.Value.Where(frd => frd.Name != Player.Name).Concat(new[] { offline }).ToArray();
		}

		/// <summary>
		/// Trigger Player Friend List Update on World Enter
		/// </summary>
		private void OnClientStateChanged(DOLEvent e, object sender, EventArgs arguments)
		{
			var client = sender as GameClient;
			if (client == null)
				return;

			if (client.ClientState == GameClient.eClientState.WorldEnter && client.Player != null)
			{
				// Load Friend List
				AddPlayerFriendsListToCache(client.Player);
				SendPlayerFriendsList(client.Player);
			}
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