using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DOL.Database;

namespace DOL.GS
{
    /// <summary>
    /// Alliance are the alliance between guild in game
    /// </summary>
    public class Alliance
    {
        private readonly List<Guild> _guilds = new(2);
        private readonly Lock _lock = new();

        public List<Guild> Guilds
        {
            get
            {
                lock (_lock)
                {
                    return _guilds.ToList();
                }
            }
        }

        public DbGuildAlliance DbAlliance { get; set; }

        public void AddGuild(Guild guild)
        {
            lock (_lock)
            {
                guild.alliance = this;
                _guilds.Add(guild);
                guild.AllianceId = DbAlliance.ObjectId;
                DbAlliance.DBguilds = null; // Is there no other way?
                guild.SaveIntoDatabase();

                if (DbAlliance.IsPersisted)
                    GameServer.Database.SaveObject(DbAlliance);
                else
                    GameServer.Database.AddObject(DbAlliance);

                GameServer.Database.FillObjectRelations(DbAlliance);
                SendMessageToAllianceMembers(guild.Name + " has joined the alliance of " + DbAlliance.AllianceName, PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
            }
        }

        public void AddGuildOnLoad(Guild guild)
        {
            lock (_lock)
            {
                _guilds.Add(guild);
            }
        }

        public void RemoveGuild(Guild guild)
        {
            lock (_lock)
            {
                guild.alliance = null;
                guild.AllianceId = string.Empty;
                _guilds.Remove(guild);
                guild.SaveIntoDatabase();
                guild.SendMessageToGuildMembers(guild.Name + " has left the alliance of " + DbAlliance.AllianceName, PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                SendMessageToAllianceMembers(guild.Name + " has left the alliance of " + DbAlliance.AllianceName, PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);

                if (guild.GuildID == DbAlliance.DBguildleader.GuildID)
                {
                    SendMessageToAllianceMembers(guild.Name + " has disbanded the alliance of " + DbAlliance.AllianceName, PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                    ArrayList mgl = new ArrayList(_guilds);
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
                    GameServer.Database.DeleteObject(DbAlliance);
                }
                else
                {
                    DbAlliance.DBguilds = null;
                    GameServer.Database.SaveObject(DbAlliance);
                    GameServer.Database.FillObjectRelations(DbAlliance);
                }
            }
        }

        public bool Contains(Guild guild)
        {
            lock (_lock)
            {
                return _guilds.Contains(guild);
            }
        }

        public void SendMessageToAllianceMembers(string msg, PacketHandler.eChatType type, PacketHandler.eChatLoc loc)
        {
            lock (_lock)
            {
                foreach (Guild guild in _guilds)
                    guild.SendMessageToGuildMembers(msg, type, loc);
            }
        }

        public void LoadFromDatabase(DataObject obj)
        {
            DbAlliance = obj as DbGuildAlliance;
        }
    }
}
