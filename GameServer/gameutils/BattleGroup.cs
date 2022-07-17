/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DOL.GS.PacketHandler;
namespace DOL.GS
{
	/// <summary>
	/// Battlegroups
	/// </summary>
	public class BattleGroup
	{
		public const string BATTLEGROUP_PROPERTY="battlegroup";
		/// <summary>
		/// This holds all players inside the battlegroup
		/// </summary>
		protected HybridDictionary m_battlegroupMembers = new HybridDictionary();
        protected GameLiving m_battlegroupLeader;
        protected List<GamePlayer> m_battlegroupModerators = new List<GamePlayer>();

        protected Dictionary<GamePlayer, int> m_battlegroupRolls;
        protected bool recordingRolls;
        protected int rollRecordThreshold;

        bool battlegroupLootType = false;
        GamePlayer battlegroupTreasurer = null;
        int battlegroupLootTypeThreshold = 0;

		/// <summary>
		/// constructor of battlegroup
		/// </summary>
		public BattleGroup()
		{
            battlegroupLootType = false;
            battlegroupTreasurer = null;
            m_battlegroupLeader = null;
		}

        public GameLiving Leader
        {
            get { return m_battlegroupLeader; }
        }

		public HybridDictionary Members
		{
			get{return m_battlegroupMembers;}
			set{m_battlegroupMembers=value;}
		}
		
		public List<GamePlayer> Moderators
		{
			get{return m_battlegroupModerators;}
			set{m_battlegroupModerators=value;}
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
		/// <param name="player">GamePlayer to be added to the group</param>
		/// <param name="leader"></param>
		/// <returns>true if added successfully</returns>
		public virtual bool AddBattlePlayer(GamePlayer player,bool leader) 
		{
			if (player == null) return false;
			lock (m_battlegroupMembers)
			{
				if (m_battlegroupMembers.Contains(player))
					return false;
				player.TempProperties.setProperty(BATTLEGROUP_PROPERTY, this);
                player.Out.SendMessage("You join the battle group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				foreach(GamePlayer member in Members.Keys)
				{
                    member.Out.SendMessage(player.Name + " has joined the battle group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				m_battlegroupMembers.Add(player,leader);

                player.isInBG = true; //Xarik: Player is in BG
			}
			return true;
		}

        public virtual bool IsInTheBattleGroup(GamePlayer player)
        {
            lock (m_battlegroupMembers) // Mannen 10:56 PM 10/30/2006 - Fixing every lock(this)
            {
                return m_battlegroupMembers.Contains(player);
            }
        }

        public bool GetBGLootType()
        {
            return battlegroupLootType;
        }
        
        public bool IsRecordingRolls()
        {
	        return recordingRolls;
        }
        
        public int GetRecordingThreshold()
        {
	        return rollRecordThreshold;
        }
        
        public void StartRecordingRolls(int maxRoll = 1000)
		{
	        recordingRolls = true;
	        rollRecordThreshold = maxRoll;
	        m_battlegroupRolls = new Dictionary<GamePlayer, int>();
	        
	        foreach (GamePlayer ply in Members.Keys)
	        {
		        ply.Out.SendMessage($"{Leader.Name} has initiated the recording. Use /random {maxRoll} now to roll for this item.",eChatType.CT_BattleGroupLeader, eChatLoc.CL_ChatWindow);
	        }
		}

        public void StopRecordingRolls()
        {
	        recordingRolls = false;
	        foreach (GamePlayer ply in Members.Keys)
	        {
		        ply.Out.SendMessage($"{Leader.Name} stopped the recording. Use /bg showrolls to display the results.",eChatType.CT_BattleGroupLeader, eChatLoc.CL_ChatWindow);
	        }
        }
        
        public void AddRoll(GamePlayer player, int roll)
		{
	        if(!recordingRolls)
		        return;
	        if (roll > rollRecordThreshold)
		        return;
	        lock (m_battlegroupRolls)
	        {
		        if(m_battlegroupRolls.ContainsKey(player))
			        return;
		        m_battlegroupRolls.Add(player, roll);
	        }
		}
        public void ShowRollsWindow(GamePlayer player)
        {
	        if (recordingRolls)
	        {
		        player.Client.Out.SendMessage("Rolls are being recorded. Please wait.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		        return;
	        }

	        if (m_battlegroupRolls == null || m_battlegroupRolls.Count == 0)
	        {
		        player.Client.Out.SendMessage("No rolls have been recorded yet.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		        return;
	        }
	        
	        var output = new List<string>();

	        var sorted = new List<KeyValuePair<GamePlayer, int>>();

	        sorted = m_battlegroupRolls.ToList();
	        sorted.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
	        

	        var i = 1;
	        foreach (var value in sorted)
	        {
		        output.Add($"{i}) {value.Key.Name} rolled {value.Value}");
		        i++;
	        }
	        
	        player.Out.SendCustomTextWindow("LAST ROLL RESULTS", output);

        }

        public GamePlayer GetBGTreasurer()
        {
            return battlegroupTreasurer;
        }

        public GameLiving GetBGLeader()
        {
            return m_battlegroupLeader;
        }

        public bool SetBGLeader(GameLiving living)
        {
            if (living != null)
            {
                m_battlegroupLeader = living;
                return true;
            }

            return false;
        }
        public bool IsBGTreasurer(GameLiving living)
        {
            if (battlegroupTreasurer != null && living != null)
            {
                return battlegroupTreasurer == living;
            }

            return false;
        }
        public bool IsBGLeader(GameLiving living)
        {
            if (m_battlegroupLeader != null && living != null)
            {
                return m_battlegroupLeader == living;
            }

            return false;
        }
        
        public bool IsBGModerator(GamePlayer living)
        {
	        if (m_battlegroupModerators != null && living != null)
	        {
		        var ismod = m_battlegroupModerators.Contains(living);
		        return ismod;
	        }

	        return false;
        }

        public int GetBGLootTypeThreshold()
        {
            return battlegroupLootTypeThreshold;
        }

        public bool SetBGLootTypeThreshold(int thresh)
        {
            battlegroupLootTypeThreshold = thresh;

            if (thresh < 0 || thresh > 50)
            {
                battlegroupLootTypeThreshold = 0;
                return false;
            }
            else
            {
                return true;
            }
        }

        public void SetBGLootType(bool type)
        {
            battlegroupLootType = type;
            if (battlegroupLootType == false)
            {
                // Within bounds, continue
            }
            if (battlegroupLootType == true)
            {
                // Within bounds, continue
            }
            else
            {
                // Data has entered here that should not be, set to normal = 0
                battlegroupLootType = false;
            }
        }

        public bool SetBGTreasurer(GamePlayer treasurer)
        {
            battlegroupTreasurer = treasurer;
            if (battlegroupTreasurer == null)
            {
                // Do not set treasurer
                return false;
            }

            if (battlegroupTreasurer != null)
            {
                // Good input, got a treasurer, continue
                return true;
            }
            else
            {
                // Bad input, fix with null
                battlegroupTreasurer = null;
                return false;
            }
        }

        public virtual void SendMessageToBattleGroupMembers(string msg, eChatType type, eChatLoc loc)
        {
            lock (m_battlegroupMembers) // Mannen 10:56 PM 10/30/2006 - Fixing every lock(this)
            {
                foreach (GamePlayer player in m_battlegroupMembers.Keys)
                {
                    player.Out.SendMessage(msg, type, loc);
                }
            }
        }

        public int PlayerCount
        {
            get { return m_battlegroupMembers.Count; }
        }
        /// <summary>
		/// Removes a player from the group
		/// </summary>
		/// <param name="player">GamePlayer to be removed</param>
		/// <returns>true if removed, false if not</returns>
		public virtual bool RemoveBattlePlayer(GamePlayer player)
		{
			if (player == null) return false;
			lock (m_battlegroupMembers)
			{
				if (!m_battlegroupMembers.Contains(player))
					return false;
				var leader = IsBGLeader(player);
				m_battlegroupMembers.Remove(player);
				player.TempProperties.removeProperty(BATTLEGROUP_PROPERTY);
				player.isInBG = false; //Xarik: Player is no more in the BG
                player.Out.SendMessage("You leave the battle group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				foreach(GamePlayer member in Members.Keys)
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
				} else if (leader && m_battlegroupMembers.Count >= 2)
				{
					var bgPlayers = new ArrayList(m_battlegroupMembers.Count);
					lock (bgPlayers)
					{
						bgPlayers.AddRange(m_battlegroupMembers.Keys);
						var randomPlayer = bgPlayers[Util.Random(bgPlayers.Count - 1)] as GamePlayer;
						if (randomPlayer == null) return false;
						SetBGLeader(randomPlayer);
						m_battlegroupMembers[randomPlayer] = true;
						foreach(GamePlayer member in Members.Keys)
						{
							member.Out.SendMessage(randomPlayer.Name + " is the new leader of the battle group.", eChatType.CT_BattleGroupLeader, eChatLoc.CL_SystemWindow);
						}
					}
				}

			}
			return true;
		}
	}
}
