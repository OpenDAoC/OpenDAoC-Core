/*
 * Atlas Custom Quest - Atlas 1.65v Classic Freeshard
 */
/*
*Author         : Kelt
*Editor			: Kelt
*Source         : Custom
*Date           : 03 July 2022
*Quest Name     : [Memorial] Power of Nature
*Quest Classes  : all
*Quest Version  : v1.0
*
*Changes:
* 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using log4net;

namespace DOL.GS.Quests.Hibernia
{
	public class PowerOfNature : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Memorial] Power of Nature";
		protected const int minimumLevel = 1;
		protected const int maximumLevel = 50;

		private static GameNPC Theresa = null; // Start NPC
		private static GameNPC Karl = null; // Finish Quest

		// Constructors
		public PowerOfNature() : base()
		{
		}

		public PowerOfNature(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public PowerOfNature(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public PowerOfNature(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
		{
		}


		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			

			#region defineNPCs

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Theresa", eRealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 201 && npc.X == 31401 && npc.Y == 30076)
					{
						Theresa = npc;
						break;
					}

			if (Theresa == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Theresa, creating it ...");
				Theresa = new GameNPC();
				Theresa.Model = 310;
				Theresa.Name = "Theresa";
				Theresa.GuildName = "";
				Theresa.Realm = eRealm.Hibernia;
				Theresa.CurrentRegionID = 201;
				Theresa.LoadEquipmentTemplateFromDatabase("Theresa");
				Theresa.Size = 48;
				Theresa.Level = 50;
				Theresa.X = 31401;
				Theresa.Y = 30076;
				Theresa.Z = 8011;
				Theresa.Heading = 1505;
				Theresa.AddToWorld();
				if (SAVE_INTO_DATABASE)
					Theresa.SaveIntoDatabase();
			}
			
			npcs = WorldMgr.GetNPCsByName("Karl", eRealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 200 && npc.X == 328521 && npc.Y == 518534)
					{
						Karl = npc;
						break;
					}

			if (Karl == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Karl, creating it ...");
				Karl = new GameNPC();
				Karl.Model = 350;
				Karl.Name = "Karl";
				Karl.GuildName = "";
				Karl.Realm = eRealm.Hibernia;
				Karl.CurrentRegionID = 200;
				Karl.LoadEquipmentTemplateFromDatabase("Karl");
				Karl.Size = 50;
				Karl.Level = 50;
				Karl.X = 328521;
				Karl.Y = 518534;
				Karl.Z = 4285;
				Karl.Heading = 2612;
				Karl.AddToWorld();
				if (SAVE_INTO_DATABASE)
					Karl.SaveIntoDatabase();
			}
			// end npc
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Theresa, GameObjectEvent.Interact, new DOLEventHandler(TalkToTheresa));
			GameEventMgr.AddHandler(Theresa, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToTheresa));
			
			GameEventMgr.AddHandler(Karl, GameObjectEvent.Interact, new DOLEventHandler(TalkToKarl));
			GameEventMgr.AddHandler(Karl, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToKarl));
			
			Theresa.AddQuestToGive(typeof (PowerOfNature));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Theresa == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Theresa, GameObjectEvent.Interact, new DOLEventHandler(TalkToTheresa));
			GameEventMgr.RemoveHandler(Theresa, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToTheresa));
			
			GameEventMgr.RemoveHandler(Karl, GameObjectEvent.Interact, new DOLEventHandler(TalkToKarl));
			GameEventMgr.RemoveHandler(Karl, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToKarl));
			
			Theresa.RemoveQuestToGive(typeof (PowerOfNature));
		}

		protected static void TalkToTheresa(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Theresa.CanGiveQuest(typeof (PowerOfNature), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			PowerOfNature quest = player.IsDoingQuest(typeof (PowerOfNature)) as PowerOfNature;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							break;
						case 2:
							break;
						case 3:
							break;
						case 4:
							break;
					}
				}
				else
				{
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text)
					{
						case "":
							player.Out.SendQuestSubscribeCommand(Theresa, QuestMgr.GetIDForQuestType(typeof(PowerOfNature)), "Will you help Theresa to find her dad? [Memorial] Power of Nature");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "abort":
							player.Out.SendCustomDialog("Do you really want to abort this quest, \nall items gained during quest will be lost?", new CustomDialogResponse(CheckPlayerAbortQuest));
							break;
					}
				}
			}
		}
		
		protected static void TalkToKarl(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			//We also check if the player is already doing the quest
			PowerOfNature quest = player.IsDoingQuest(typeof (PowerOfNature)) as PowerOfNature;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							break;
						case 2:
							break;
						case 3:
							break;
						case 4:
							break;
					}
				}
				else
				{
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text)
					{
						
					}
				}
				else
				{
					switch (wArgs.Text)
					{
					}
				}
			}
		}

		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (PowerOfNature)) != null)
				return true;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			PowerOfNature quest = player.IsDoingQuest(typeof (PowerOfNature)) as PowerOfNature;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and finish your work!");
			}
			else
			{
				SendSystemMessage(player, "Aborting Quest " + questTitle + ". You can start over again if you want.");
				quest.AbortQuest();
			}
		}

		protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(PowerOfNature)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Theresa.CanGiveQuest(typeof (PowerOfNature), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (PowerOfNature)) != null)
				return;

			if (response == 0x00)
			{
				// send message, when decline dialog
			}
			else
			{
				//Check if we can add the quest!
				if (!Theresa.GiveQuest(typeof (PowerOfNature), player, 1))
					return;

				// add interaction here after accepting quest

			}
		}

		//Set quest name
		public override string Name
		{
			get { return questTitle; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "";
					case 2:
						return "";
					case 3:
						return "";
					case 4:
						return "";
					case 5:
						return "";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player==null || player.IsDoingQuest(typeof (PowerOfNature)) == null)
				return;

		}
		
		public class PowerOfNatureTitle : EventPlayerTitle 
    {
        /// <summary>
        /// The title description, shown in "Titles" window.
        /// </summary>
        /// <param name="player">The title owner.</param>
        /// <returns>The title description.</returns>
        public override string GetDescription(GamePlayer player)
        {
            return "Protected by Nature";
        }

        /// <summary>
        /// The title value, shown over player's head.
        /// </summary>
        /// <param name="source">The player looking.</param>
        /// <param name="player">The title owner.</param>
        /// <returns>The title value.</returns>
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Protected by Nature";
        }
		
        /// <summary>
        /// The event to hook.
        /// </summary>
        public override DOLEvent Event
        {
            get { return GamePlayerEvent.GameEntered; }
        }
		
        /// <summary>
        /// Verify whether the player is suitable for this title.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>true if the player is suitable for this title.</returns>
        public override bool IsSuitable(GamePlayer player)
        {
	        return player.HasFinishedQuest(typeof(PowerOfNature)) == 1;
        }
		
        /// <summary>
        /// The event callback.
        /// </summary>
        /// <param name="e">The event fired.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="arguments">The event arguments.</param>
        protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer p = sender as GamePlayer;
            if (p != null && p.Titles.Contains(this))
            {
                p.UpdateCurrentTitle();
                return;
            }
            base.EventCallback(e, sender, arguments);
        }
    }

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, 20, false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,1,32,Util.Random(50)), "You receive {0} as a reward.");

			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
