using System.Collections;
using System.Collections.Specialized;
using Core.GS.Enums;

namespace Core.GS.GameUtils;

public class ChatGroupUtil
{
	public const string CHATGROUP_PROPERTY="chatgroup";
	/// <summary>
	/// This holds all players inside the chatgroup
	/// </summary>
	protected HybridDictionary m_chatgroupMembers = new HybridDictionary();

	/// <summary>
	/// constructor of chat group
	/// </summary>
	public ChatGroupUtil()
	{
	}
	public HybridDictionary Members
	{
		get{return m_chatgroupMembers;}
		set{m_chatgroupMembers=value;}
	}
	private bool listen=false;
	public bool Listen
	{
		get{return listen;}
		set{listen = value;}
	}
	private bool ispublic=true;
	public bool IsPublic
	{
		get{return ispublic;}
		set{ispublic = value;}
	}
	private string password="";
	public string Password
	{
		get{return password;}
		set{password = value;}
	}

	/// <summary>
    /// Adds a player to the chatgroup
	/// </summary>
	/// <param name="player"></param>
	/// <param name="leader"></param>
	/// <returns></returns>
	public virtual bool AddPlayer(GamePlayer player,bool leader) 
	{
		if (player == null) return false;
		lock (m_chatgroupMembers)
		{
			if (m_chatgroupMembers.Contains(player))
				return false;
			player.TempProperties.SetProperty(CHATGROUP_PROPERTY, this);
			player.Out.SendMessage("You join the chat group.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			foreach(GamePlayer member in Members.Keys)
			{
				member.Out.SendMessage(player.Name+" has joined the chat group.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			m_chatgroupMembers.Add(player,leader);
		}
		return true;
	}

	/// <summary>
	/// Removes a player from the group
	/// </summary>
	/// <param name="player">GamePlayer to be removed</param>
	/// <returns>true if removed, false if not</returns>
	public virtual bool RemovePlayer(GamePlayer player)
	{
		if (player == null) return false;
		lock (m_chatgroupMembers)
		{
			if (!m_chatgroupMembers.Contains(player))
				return false;
			m_chatgroupMembers.Remove(player);
			player.TempProperties.RemoveProperty(CHATGROUP_PROPERTY);
			player.Out.SendMessage("You leave the chat group.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			foreach(GamePlayer member in Members.Keys)
			{
				member.Out.SendMessage(player.Name+" has left the chat group.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			if (m_chatgroupMembers.Count == 1)
			{
				ArrayList lastPlayers = new ArrayList(m_chatgroupMembers.Count);
				lastPlayers.AddRange(m_chatgroupMembers.Keys);
				foreach (GamePlayer plr in lastPlayers)
				{
					RemovePlayer(plr);
				}
			}
		}
		return true;
	}
}