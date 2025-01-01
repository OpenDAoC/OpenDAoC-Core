using System;
using System.Collections;
using System.Threading;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// Alliance are the alliance between guild in game
	/// </summary>
	public class Alliance
	{
		protected ArrayList m_guilds;
		protected DbGuildAlliance m_dballiance;
		public Alliance()
		{
			m_dballiance = null;
			m_guilds = new ArrayList(2);
		}
		public ArrayList Guilds
		{
			get
			{
				return m_guilds;
			}
			set
			{
				m_guilds = value;
			}
		}
		public readonly Lock GuildsLock = new();
		public DbGuildAlliance Dballiance
		{
			get
			{
				return m_dballiance;
			}
			set
			{
				m_dballiance = value;
			}
		}

		#region IList
		public void AddGuild(Guild myguild)
		{
			lock (GuildsLock)
			{
				myguild.alliance = this;
				Guilds.Add(myguild);
				myguild.AllianceId = m_dballiance.ObjectId;
				m_dballiance.DBguilds = null;
				//sirru 23.12.06 Add the new object instead of trying to save it
				GameServer.Database.AddObject(m_dballiance);
				GameServer.Database.FillObjectRelations(m_dballiance);
				//sirru 23.12.06 save changes to db for each guild
				SaveIntoDatabase();
				SendMessageToAllianceMembers(myguild.Name + " has joined the alliance of " + m_dballiance.AllianceName, PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
			}
		}
		public void RemoveGuild(Guild myguild)
		{
			lock (GuildsLock)
			{
				myguild.alliance = null;
				myguild.AllianceId = string.Empty;
                Guilds.Remove(myguild);
                if (myguild.GuildID == m_dballiance.DBguildleader.GuildID)
                {
                    SendMessageToAllianceMembers(myguild.Name + " has disbanded the alliance of " + m_dballiance.AllianceName, PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                    ArrayList mgl = new ArrayList(Guilds);
                    foreach (Guild mg in mgl)
                    {
                        try
                        {
                            RemoveGuild(mg);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    GameServer.Database.DeleteObject(m_dballiance);
                }
                else
                {
                    m_dballiance.DBguilds = null;
                    GameServer.Database.SaveObject(m_dballiance);
                    GameServer.Database.FillObjectRelations(m_dballiance);
                }
				//sirru 23.12.06 save changes to db for each guild
				myguild.SaveIntoDatabase();
                myguild.SendMessageToGuildMembers(myguild.Name + " has left the alliance of " + m_dballiance.AllianceName, PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                SendMessageToAllianceMembers(myguild.Name + " has left the alliance of " + m_dballiance.AllianceName, PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
			}
		}
		
		public void PromoteGuild(Guild myguild)
		{
			lock (GuildsLock)
			{
				m_dballiance.DBguildleader.GuildID = myguild.GuildID;
				m_dballiance.DBguildleader.GuildName = myguild.Name;
				
				// m_dballiance.AllianceName = myguild.Name;
				// m_dballiance.LeaderGuildID = myguild.GuildID;
				GameServer.Database.SaveObject(m_dballiance);
				// GameServer.Database.FillObjectRelations(m_dballiance);
				
				SendMessageToAllianceMembers(myguild.Name + " is the new leader of the alliance", PacketHandler.eChatType.CT_Alliance, PacketHandler.eChatLoc.CL_SystemWindow);
			}
		}
		
		public void Clear()
		{
			lock (GuildsLock)
			{
				foreach (Guild guild in Guilds)
				{
					guild.alliance = null;
					guild.AllianceId = string.Empty;
					//sirru 23.12.06 save changes to db
					guild.SaveIntoDatabase();
				}
				Guilds.Clear();
			}
		}
		public bool Contains(Guild myguild)
		{
			lock (GuildsLock)
			{
				return Guilds.Contains(myguild);
			}
		}

		#endregion

		/// <summary>
		/// send message to all member of alliance
		/// </summary>
		public void SendMessageToAllianceMembers(string msg, PacketHandler.eChatType type, PacketHandler.eChatLoc loc)
		{
			lock (GuildsLock)
			{
				foreach (Guild guild in Guilds)
				{
					guild.SendMessageToGuildMembers(msg, type, loc);
				}
			}
		}

		/// <summary>
		/// Loads this alliance from an alliance table
		/// </summary>
		/// <param name="obj"></param>
		public void LoadFromDatabase(DataObject obj)
		{
			if (!(obj is DbGuildAlliance))
				return;

			m_dballiance = (DbGuildAlliance)obj;
		}

		/// <summary>
		/// Saves this alliance to database
		/// </summary>
		public void SaveIntoDatabase()
		{
			GameServer.Database.SaveObject(m_dballiance);
			lock (GuildsLock)
			{
				foreach (Guild guild in Guilds)
				{
					guild.SaveIntoDatabase();
				}
			}
		}
	}
}
