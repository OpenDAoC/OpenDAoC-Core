namespace Core.GS.Players.Friends;

public static class FriendsMgrExtensions
{
	/// <summary>
	/// Get This Player Friends List
	/// </summary>
	/// <param name="player">Player to retrieve Friends List from</param>
	/// <returns>String array of Player's friends</returns>
	public static string[] GetFriends(this GamePlayer player)
	{
		return GameServer.Instance.PlayerManager.Friends[player];
	}
	
	/// <summary>
	/// Remove a Friend from Player Friends List
	/// </summary>
	/// <param name="player">Player to remove Friend from</param>
	/// <param name="friendName">Friend's Name to be removed</param>
	/// <returns>True if friend removed successfully.</returns>
	public static bool RemoveFriend(this GamePlayer player, string friendName)
	{
		return GameServer.Instance.PlayerManager.Friends.RemoveFriendFromPlayerList(player, friendName);
	}
	
	/// <summary>
	/// Add a Friend to Player Friends List
	/// </summary>
	/// <param name="player">Player to add Friend to</param>
	/// <param name="friendName">Friend's Name to be added</param>
	/// <returns>True if friend added successfully</returns>
	public static bool AddFriend(this GamePlayer player, string friendName)
	{
		return GameServer.Instance.PlayerManager.Friends.AddFriendToPlayerList(player, friendName);
	}
	
	/// <summary>
	/// Send Player Friends List Snapshot
	/// </summary>
	/// <param name="player">Player to send Snapshot to</param>
	public static void SendFriendsListSnapshot(this GamePlayer player)
	{
		GameServer.Instance.PlayerManager.Friends.SendPlayerFriendsSnapshot(player);
	}

	/// <summary>
	/// Send Player Friends List Social Window
	/// </summary>
	/// <param name="player">Player to send Social Window to</param>
	public static void SendFriendsListSocial(this GamePlayer player)
	{
		GameServer.Instance.PlayerManager.Friends.SendPlayerFriendsSocial(player);
	}
}