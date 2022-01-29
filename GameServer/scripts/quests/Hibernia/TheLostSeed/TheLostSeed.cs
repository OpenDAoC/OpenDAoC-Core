/*
 * Atlas SI Quest - Atlas 1.65v Classic Freeshard
 */
/*
*Author         : Kelt
*Editor			: Kelt
*Source         : SI Quest
*Date           : 29 January 2022
*Quest Name     : The Lost Seed
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
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests.Actions;
using DOL.GS.Quests.Triggers;
using log4net;

namespace DOL.GS.Quests.Hibernia
{
	public class TheLostSeed : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "The Lost Seed";
		protected const int minimumLevel = 48;
		protected const int maximumLevel = 50;

		private static GameNPC Terod = null; // Start NPC
		private static GameNPC Kedril = null;
		private static GameNPC Emolia = null;
		private static GameNPC Jandros = null;
		
		private static GameNPC Feairna_Athar = null; //Mob to Kill
		
		// Constructors
		public TheLostSeed() : base()
		{
		}

		public TheLostSeed(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public TheLostSeed(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public TheLostSeed(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
		{
		}


		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;

			#region defineNPCs
			
			#endregion

			#region defineItems

			#endregion

			#region defineObject

			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
			
			/* Now we bring to Terod the possibility to give this quest to players */
			Terod.AddQuestToGive(typeof (TheLostSeed));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Terod == null)
				return;
			
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
			
			/* Now we remove to Terod the possibility to give this quest to players */
			Terod.RemoveQuestToGive(typeof (TheLostSeed));
		}

		protected static void TalkToTerod(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Terod.CanGiveQuest(typeof (TheLostSeed), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			TheLostSeed quest = player.IsDoingQuest(typeof (TheLostSeed)) as TheLostSeed;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						
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
						case "lost seed":
							player.Out.SendQuestSubscribeCommand(Terod, QuestMgr.GetIDForQuestType(typeof(TheLostSeed)), "Will you help Terod [The Lost Seed]?");
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
			else if (e == GameLivingEvent.ReceiveItem)
			{
				ReceiveItemEventArgs rArgs = (ReceiveItemEventArgs) args;
				if (quest != null)
				{
					
				}
			}
		}
		
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (TheLostSeed)) != null)
				return true;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			TheLostSeed quest = player.IsDoingQuest(typeof (TheLostSeed)) as TheLostSeed;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(TheLostSeed)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Terod.CanGiveQuest(typeof (TheLostSeed), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (TheLostSeed)) != null)
				return;

			if (response == 0x00)
			{
				
			}
			else
			{
				//Check if we can add the quest!
				if (!Terod.GiveQuest(typeof (TheLostSeed), player, 1))
					return;
			}
			Terod.Interact(player);
		}

		//Set quest name
		public override string Name
		{
			get { return "The Lost Seed"; }
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

			if (player==null || player.IsDoingQuest(typeof (TheLostSeed)) == null)
				return;
			
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, 1768448, true);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,2,32,Util.Random(50)), "You receive {0} as a reward.");

			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
	
}
