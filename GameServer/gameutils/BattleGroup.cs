using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using static DOL.GS.IGameStaticItemOwner;

namespace DOL.GS
{
	/// <summary>
	/// Battlegroups
	/// </summary>
	public class BattleGroup : IGameStaticItemOwner
	{
		public const string BATTLEGROUP_PROPERTY="battlegroup";

		public readonly Lock Lock = new();

		/// <summary>
		/// This holds all players inside the battlegroup
		/// </summary>
		protected HybridDictionary m_battlegroupMembers = new HybridDictionary();
		protected readonly Lock _battlegroupMembersLock = new();
        protected GamePlayer m_battlegroupLeader;
        protected List<GamePlayer> m_battlegroupModerators = new List<GamePlayer>();

        protected Dictionary<GamePlayer, int> m_battlegroupRolls;
        protected readonly Lock _battlegroupRolls = new();
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

		private string password=string.Empty;
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
			lock (_battlegroupMembersLock)
			{
				if (m_battlegroupMembers.Contains(player))
					return false;
				player.TempProperties.SetProperty(BATTLEGROUP_PROPERTY, this);
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
            lock (_battlegroupMembersLock)
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
	        lock (_battlegroupRolls)
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

        public bool SetBGLeader(GamePlayer player)
        {
            if (player != null)
            {
                m_battlegroupLeader = player;
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
        }

        public void SetBGTreasurer(GamePlayer treasurer)
        {
            battlegroupTreasurer = treasurer;
            battlegroupLootType = treasurer != null;
        }

        public int PlayerCount
        {
            get { return m_battlegroupMembers.Count; }
        }

		public string Name => $"{(Leader == null || PlayerCount <= 0 ? "leaderless" : $"{Leader.Name}'s")} battlegroup (size: {PlayerCount})";

		public bool TryAutoPickUpMoney(GameMoney money)
		{
			return TryPickUpMoney(Leader as GamePlayer, money) is not TryPickUpResult.CANNOT_HANDLE;
		}

		public bool TryAutoPickUpItem(WorldInventoryItem worldItem)
		{
			// We don't care if players have auto loot enabled, or if they can see the item (the item isn't added to the world yet anyway), or who attacked last, etc.
			// This is especially important for battlegroups since can have an illimited amount of players.
			return TryPickUpItem(battlegroupTreasurer, worldItem) is not TryPickUpResult.CANNOT_HANDLE;
		}

		public TryPickUpResult TryPickUpMoney(GamePlayer source, GameMoney money)
		{
			// Splitting money in a battlegroup could cause performance issues. Let it fallback to group then solo logic.
			return TryPickUpResult.CANNOT_HANDLE;
		}

		public TryPickUpResult TryPickUpItem(GamePlayer source, WorldInventoryItem item)
		{
			// A battlegroup is only able to pick up items if it has a treasurer, otherwise it's supposed to fallback to group then solo logic.
			// There is no range check. If you're in a BG, every item goes to the treasurer or stay on the ground.
			// If his inventory is full, the item should simply stay on the ground until he makes some room, or another treasurer is appointed.
			if (!GetBGLootType() || battlegroupTreasurer == null)
				return TryPickUpResult.CANNOT_HANDLE;

			if (!GiveItem(battlegroupTreasurer, item.Item))
			{
				battlegroupTreasurer.Out.SendMessage(LanguageMgr.GetTranslation(battlegroupTreasurer.Client.Account.Language, "GamePlayer.PickupObject.BackpackFull"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return TryPickUpResult.FAILED;
			}

			battlegroupTreasurer.Out.SendMessage(LanguageMgr.GetTranslation(battlegroupTreasurer.Client.Account.Language, "GamePlayer.PickupObject.YouGet", item.Item.GetName(1, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			Message.SystemToOthers(source, LanguageMgr.GetTranslation(battlegroupTreasurer.Client.Account.Language, "GamePlayer.PickupObject.GroupMemberPicksUp", Name, item.Item.GetName(1, false)), eChatType.CT_System);
			item.RemoveFromWorld();
			return TryPickUpResult.SUCCESS;

			static bool GiveItem(GamePlayer player, DbInventoryItem item)
			{
				if (item.IsStackable)
					return player.Inventory.AddTemplate(item, item.Count, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

				return player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);
			}
		}

		/// <summary>
		/// Removes a player from the group
		/// </summary>
		/// <param name="player">GamePlayer to be removed</param>
		/// <returns>true if removed, false if not</returns>
		public virtual bool RemoveBattlePlayer(GamePlayer player)
		{
			if (player == null) return false;
			lock (_battlegroupMembersLock)
			{
				if (!m_battlegroupMembers.Contains(player))
					return false;
				var leader = IsBGLeader(player);
				m_battlegroupMembers.Remove(player);
				player.TempProperties.RemoveProperty(BATTLEGROUP_PROPERTY);
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
					m_battlegroupLeader = null;
				} else if (leader && m_battlegroupMembers.Count >= 2)
				{
					var bgPlayers = new ArrayList(m_battlegroupMembers.Count);
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
			return true;
		}
	}
}
