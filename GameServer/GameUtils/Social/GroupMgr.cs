using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Core.GS.GameUtils;

/// <summary>
/// The GroupMgr holds pointers to all groups and to players
/// looking for a group
/// </summary>
public static class GroupMgr
{
	static readonly ConcurrentDictionary<GroupUtil, bool> m_groups = new();
	static readonly ConcurrentDictionary<GamePlayer, bool> m_lfgPlayers = new();

	/// <summary>
	/// Adds a group to the list of groups
	/// </summary>
	/// <param name="group">The group to add</param>
	/// <returns>True if the function succeeded, otherwise false</returns>
	public static bool AddGroup(GroupUtil group)
	{
		return m_groups.TryAdd(group, true);
	}

	/// <summary>
	/// Removes a group from the manager
	/// </summary>
	/// <param name="group"></param>
	/// <returns></returns>
	public static bool RemoveGroup(GroupUtil group)
	{
		return m_groups.TryRemove(group, out _);
	}

	/// <summary>
	/// Adds a player to the looking for group list
	/// </summary>
	/// <param name="member">player to add to the list</param>
	public static void SetPlayerLooking(GamePlayer member)
	{
		if (member.LookingForGroup == false && m_lfgPlayers.TryAdd(member, true))
			member.LookingForGroup = true;
	}

	/// <summary>
	/// Removes a player from the looking for group list
	/// </summary>
	/// <param name="member">player to remove from the list</param>
	public static void RemovePlayerLooking(GamePlayer member)
	{
		member.LookingForGroup = false;
		bool dummy;
		m_lfgPlayers.TryRemove(member, out dummy);
	}

	/// <summary>
	/// Returns a list of groups by their status
	/// </summary>
	/// <param name="status">statusbyte</param>
	/// <returns>ArrayList of groups</returns>
	public static ICollection<GroupUtil> ListGroupByStatus(byte status)
	{
		return m_groups.Keys.Where(g => g.Status == 0x0B || g.Status == status).ToArray();
	}

	/// <summary>
	/// Returns an Arraylist of all players looking for a group
	/// </summary>
	/// <returns>ArrayList of all players looking for a group</returns>
	public static ICollection<GamePlayer> LookingForGroupPlayers()
	{
		return m_lfgPlayers.Keys.Where(p => p.Group == null).ToArray();
	}
}