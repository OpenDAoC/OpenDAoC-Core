using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using Core.Events;
using Core.GS.PacketHandler;
using log4net;

namespace Core.GS.Quests
{
	public class AMission
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The temp property name for next check mission millisecond
		/// </summary>
		protected const string CHECK_MISSION_TICK = "checkMissionTick";

		/// <summary>
		/// Time player must wait after failed mission check to get new mission, in milliseconds
		/// "Once a player has a personal mission,
		/// a new Personal mission cannot be obtained for 30 minutes,
		/// or until the current Personal mission is complete
		/// - whichever occurs first."
		/// </summary>
		protected const int CHECK_MISSION_DELAY = 30 * 60 * 1000; // 30 minutes

		public EMissionType MissionType
		{
			get 
			{
				if (m_owner is GamePlayer)
					return EMissionType.Personal;
				else if (m_owner is GroupUtil)
					return EMissionType.Group;
				else if (m_owner is ERealm)
					return EMissionType.Realm;
				else return EMissionType.None;
			}
		}

		/// <summary>
		/// owner of the mission
		/// </summary>
		protected object m_owner = null;

		/// <summary>
		/// Constructs a new Mission
		/// </summary>
		/// <param name="owner">The owner of the mission</param>
		public AMission(object owner)
		{
			m_owner = owner;
		}

		public virtual long RewardXP
		{
			get { return 0; }
		}

		public virtual long RewardMoney
		{
			get 
			{
				return 50 * 100 * 100;
			}
		}

		public virtual long RewardRealmPoints
		{
			get 
			{
				return 1500;
			}
		}

		/// <summary>
		/// Retrieves the name of the mission
		/// </summary>
		public virtual string Name
		{
			get 
			{
				switch (MissionType)
				{
					case EMissionType.Personal: return "Personal Mission";
					case EMissionType.Group: return "Group Mission";
					case EMissionType.Realm: return "Realm Mission";
					case EMissionType.Task: return "Task";
					case EMissionType.None: return "Unknown Mission";
					default: return "MISSION NAME UNDEFINED!";
				}
			}
		}

		/// <summary>
		/// Retrieves the description for the mission
		/// </summary>
		public virtual string Description
		{
			get { return "MISSION DESCRIPTION UNDEFINED!"; }
		}

		/// <summary>
		/// This HybridDictionary holds all the custom properties of this quest
		/// </summary>
		protected HybridDictionary m_customProperties = new HybridDictionary();

		/// <summary>
		/// This method sets a custom Property to a specific value
		/// </summary>
		/// <param name="key">The name of the property</param>
		/// <param name="value">The value of the property</param>
		public void SetCustomProperty(string key, string value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (value == null)
				throw new ArgumentNullException("value");

			//Make the string safe
			key = key.Replace(';', ',');
			key = key.Replace('=', '-');
			value = value.Replace(';', ',');
			value = value.Replace('=', '-');
			lock (m_customProperties)
			{
				m_customProperties[key] = value;
			}
		}

		/// <summary>
		/// Removes a custom property from the database
		/// </summary>
		/// <param name="key">The key name of the property</param>
		public void RemoveCustomProperty(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			lock (m_customProperties)
			{
				m_customProperties.Remove(key);
			}
		}

		/// <summary>
		/// This method retrieves a custom property from the database
		/// </summary>
		/// <param name="key">The property key</param>
		/// <returns>The property value</returns>
		public string GetCustomProperty(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return (string)m_customProperties[key];
		}

		/// <summary>
		/// Called to finish the mission
		/// </summary>
		public virtual void FinishMission()
		{
			foreach (GamePlayer player in Targets)
			{
				if (m_owner is GroupUtil)
				{
					if (!player.IsWithinRadius((m_owner as GroupUtil).Leader, WorldMgr.MAX_EXPFORKILL_DISTANCE))
						continue;
				}
				if (RewardXP > 0)
					player.GainExperience(EXpSource.Mission, RewardXP);

                if (RewardMoney > 0)
                {
                    player.AddMoney(RewardMoney, "You receive {0} for completing your task.");
                    InventoryLogging.LogInventoryAction("(MISSION;" + MissionType + ")", player, EInventoryActionType.Quest, RewardMoney);
                }

			    if (RewardRealmPoints > 0)
					player.GainRealmPoints(RewardRealmPoints);

				player.Out.SendMessage("You finish the " + Name + "!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}

			switch (MissionType)
			{
				case EMissionType.Personal: (m_owner as GamePlayer).Mission = null; break;
				case EMissionType.Group: (m_owner as GroupUtil).Mission = null; break;
				//case eMissionType.Realm: (m_owner.RealmMission = null; break;
			}

			m_customProperties.Clear();
		}

		private List<GamePlayer> Targets
		{
			get
			{
				switch (MissionType)
				{
					case EMissionType.Personal:
						{
							GamePlayer player = m_owner as GamePlayer;
							List<GamePlayer> list = new List<GamePlayer>();
							list.Add(player);
							return list;
						}
					case EMissionType.Group:
						{
							GroupUtil group = m_owner as GroupUtil;
							return new List<GamePlayer>(group.GetPlayersInTheGroup());
						}
					case EMissionType.Realm:
					case EMissionType.None:
					default: return new List<GamePlayer>();
				}
			}
		}

		/// <summary>
		/// A mission runs out of time
		/// </summary>
		public virtual void ExpireMission()
		{
			foreach (GamePlayer player in Targets)
			{
				player.Out.SendMessage("Your " + Name + " has expired!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}

			switch (MissionType)
			{
				case EMissionType.Personal: (m_owner as GamePlayer).Mission = null; break;
				case EMissionType.Group: (m_owner as GroupUtil).Mission = null; break;
				//case eMissionType.Realm: m_owner.RealmMission = null; break;
			}
			m_customProperties.Clear();
		}

		public virtual void UpdateMission()
		{
			foreach (GamePlayer player in Targets)
			{
				player.Out.SendQuestListUpdate();
			}
		}

		/// <summary>
		/// This method needs to be implemented in each quest.
		/// It is the core of the quest. The global event hook of the GamePlayer.
		/// This method will be called whenever a GamePlayer with this quest
		/// fires ANY event!
		/// </summary>
		/// <param name="e">The event type</param>
		/// <param name="sender">The sender of the event</param>
		/// <param name="args">The event arguments</param>
		public virtual void Notify(CoreEvent e, object sender, EventArgs args)
		{
		}
	}
}