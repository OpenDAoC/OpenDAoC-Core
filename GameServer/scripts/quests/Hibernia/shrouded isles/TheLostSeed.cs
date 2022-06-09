/*
 * Atlas SI Quest - Atlas 1.65v Classic Freeshard
 */
/*
*Author         : Kelt
*Editor			: Kelt
*Source         : SI Quest
*Date           : 09 June 2022
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
using DOL.AI.Brain;
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

		private static GameNPC Terod = null; // Start NPC + Finish NPC
		private static GameNPC Kredril = null; // step 2
		private static HiberniaSITeleporter Emolia = null; // step 3
		private static GameNPC Jandros = null; // step 4 + 6
		
		private static GameNPC Feairna_Athar = null; //Mob to Kill
		
		private static readonly GameLocation treantLocation = new("Feairna-Athar", 181, 292515, 319526, 2238);
		
		private static IArea treantArea;

		private static ItemTemplate paidrean_necklace;
		private static ItemTemplate glowing_red_jewel;
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

		public override int Level =>
			// Quest Level
			minimumLevel;

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;

			#region defineNPCs
			
			 var npcs = WorldMgr.GetNPCsByName("Terod", eRealm.Hibernia);

        if (npcs.Length > 0)
            foreach (var npc in npcs)
                if (npc.CurrentRegionID == 181 && npc.X == 382809 && npc.Y == 421409)
                {
	                Terod = npc;
                    break;
                }

        if (Terod == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Terod, creating it ...");
            Terod = new GameNPC();
            Terod.Model = 382;
            Terod.Name = "Terod";
            Terod.GuildName = "";
            Terod.Realm = eRealm.Hibernia;
            Terod.CurrentRegionID = 181;
            Terod.LoadEquipmentTemplateFromDatabase("Terod");
            Terod.Size = 50;
            Terod.Level = 50;
            Terod.X = 382809;
            Terod.Y = 421409;
            Terod.Z = 5604;
            Terod.Heading = 1044;
            Terod.AddToWorld();
            if (SAVE_INTO_DATABASE) Terod.SaveIntoDatabase();
        }

        npcs = WorldMgr.GetNPCsByName("Kredril", eRealm.Hibernia);

        if (npcs.Length > 0)
            foreach (var npc in npcs)
                if (npc.CurrentRegionID == 181 && npc.X == 380514 && npc.Y == 421419)
                {
	                Kredril = npc;
                    break;
                }

        if (Kredril == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Kredril , creating it ...");
            Kredril = new GameNPC();
            Kredril.Model = 352;
            Kredril.Name = "Kredril";
            Kredril.GuildName = "";
            Kredril.Realm = eRealm.Hibernia;
            Kredril.CurrentRegionID = 181;
            Kredril.LoadEquipmentTemplateFromDatabase("Kredril");
            Kredril.Size = 51;
            Kredril.Level = 52;
            Kredril.X = 380514;
            Kredril.Y = 421419;
            Kredril.Z = 5520;
            Kredril.Heading = 1420;
            Kredril.AddToWorld();
            if (SAVE_INTO_DATABASE) Kredril.SaveIntoDatabase();
        }
        // end npc

        npcs = WorldMgr.GetNPCsByName("Emolia", eRealm.Hibernia);
        if (npcs.Length > 0)
            foreach (var npc in npcs)
                if (npc.CurrentRegionID == 181 && npc.X == 404696 && npc.Y == 503469)
                {
	                Emolia = (HiberniaSITeleporter)npc;
                    break;
                }

        if (Emolia == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Emolia , creating it ...");
            Emolia = new HiberniaSITeleporter();
            //should load equipment from script
            //Emolia.LoadEquipmentTemplateFromDatabase("Emolia");
            Emolia.Model = 714;
            Emolia.Name = "Emolia";
            Emolia.GuildName = "";
            Emolia.Realm = eRealm.Hibernia;
            Emolia.CurrentRegionID = 181;
            Emolia.Size = 50;
            Emolia.Level = 50;
            Emolia.X = 378864;
            Emolia.Y = 421086;
            Emolia.Z = 5528;
            Emolia.Heading = 3054;
            Emolia.VisibleActiveWeaponSlots = 34;
            Emolia.MaxSpeedBase = 200;
            Emolia.AddToWorld();
            if (SAVE_INTO_DATABASE) Emolia.SaveIntoDatabase();
        }
        // end npc

        npcs = WorldMgr.GetNPCsByName("Jandros", eRealm.Hibernia);
        if (npcs.Length > 0)
	        foreach (var npc in npcs)
		        if (npc.CurrentRegionID == 181 && npc.X == 404696 && npc.Y == 503469)
		        {
			        Jandros = npc;
			        break;
		        }

        if (Jandros == null)
        {
	        if (log.IsWarnEnabled)
		        log.Warn("Could not find Jandros , creating it ...");
	        Jandros = new GameNPC();
	        Jandros.LoadEquipmentTemplateFromDatabase("d26b8dab-dbdd-4d82-b265-9376cab4deb7");
	        Jandros.Model = 734;
	        Jandros.Name = "Jandros";
	        Jandros.GuildName = "";
	        Jandros.Realm = eRealm.Hibernia;
	        Jandros.CurrentRegionID = 181;
	        Jandros.Size = 53;
	        Jandros.Level = 54;
	        Jandros.X = 310873;
	        Jandros.Y = 349961;
	        Jandros.Z = 3571;
	        Jandros.Heading = 1459;
	        Jandros.VisibleActiveWeaponSlots = 34;
	        Jandros.MaxSpeedBase = 200;
	        Jandros.AddToWorld();
	        if (SAVE_INTO_DATABASE) Jandros.SaveIntoDatabase();
        }
        // end npc
			#endregion

			#region defineItems

			#endregion

			const int radius = 1500;
			var region = WorldMgr.GetRegion(treantLocation.RegionID);
			treantArea = region.AddArea(new Area.Circle("accursed piece of forest", treantLocation.X, treantLocation.Y, treantLocation.Z,
				radius));
			treantArea.RegisterPlayerEnter(PlayerEnterTreantArea);
			
			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
			
			/* Now we bring to Terod the possibility to give this quest to players */
			Terod?.AddQuestToGive(typeof (TheLostSeed));

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
			treantArea.UnRegisterPlayerEnter(PlayerEnterTreantArea);
			WorldMgr.GetRegion(treantLocation.RegionID).RemoveArea(treantArea);
			
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
			
			/* Now we remove to Terod the possibility to give this quest to players */
			Terod.RemoveQuestToGive(typeof (TheLostSeed));
		}

		protected virtual void CreateFeairnaAthar(GamePlayer player)
		{
			Feairna_Athar = new GameNPC();
			Feairna_Athar.Model = 767;
			Feairna_Athar.Name = "Feairna-Athar";
			Feairna_Athar.GuildName = "";
			Feairna_Athar.Realm = eRealm.None;
			Feairna_Athar.Race = 2007;
			Feairna_Athar.BodyType = (ushort) NpcTemplateMgr.eBodyType.Plant;
			Feairna_Athar.CurrentRegionID = 181;
			Feairna_Athar.Size = 150;
			Feairna_Athar.Level = 65;
			Feairna_Athar.ScalingFactor = 60;
			Feairna_Athar.X = 292515;
			Feairna_Athar.Y = 319526;
			Feairna_Athar.Z = 2238;
			Feairna_Athar.MaxSpeedBase = 250;
			Feairna_Athar.AddToWorld();

			var brain = new StandardMobBrain();
			brain.AggroLevel = 200;
			brain.AggroRange = 500;
			Feairna_Athar.SetOwnBrain(brain);

			Feairna_Athar.AddToWorld();

			Feairna_Athar.StartAttack(player);
		}
		
		private static void PlayerEnterTreantArea(DOLEvent e, object sender, EventArgs args)
		{
			var aargs = args as AreaEventArgs;
			var player = aargs?.GameObject as GamePlayer;

			if (player == null)
				return;

			var quest = player.IsDoingQuest(typeof(TheLostSeed)) as TheLostSeed;

			if (quest is not {Step: 5}) return;

			if (player.Group != null)
				if (player.Group.Leader != player)
					return;

			var existingCopy = WorldMgr.GetNPCsByName("Feairna-Athar", eRealm.None);

			if (existingCopy.Length > 0) return;

			// player near treant           
			SendSystemMessage(player,
				"You feel a quiet rustling in the leaves overhead.");
			player.Out.SendMessage("Feairna-Athar ambushes you!", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
			quest.CreateFeairnaAthar(player);
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
		
		protected static void TalkToKredril(DOLEvent e, object sender, EventArgs args)
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
					}
				}
				else
				{
					switch (wArgs.Text)
					{
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
		
		protected static void TalkToJandros(DOLEvent e, object sender, EventArgs args)
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
						return "Speak with Terod in Droighaid.";
					case 2:
						return "Speak with Kredril in Droighaid and tell him more about the Lost Seed.";
					case 3:
						return "Go to Emolia and port yourself to Aalid Feie.";
					case 4:
						return "Speak with Jandros in Aalid Feie and ask him about the Lost Seed.";
					case 5:
						return "Follow the path north towards Cothrom Gorge. " +
						       "Once in the forest, head west. You will find the Treant Feairna-Athar, kill him and bring me the jewel.";
					case 6:
						return "Return the jewel to Jandros in Aalid Feie.";
					case 7:
						return "Speak with Terod in Droighaid for your reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			var player = sender as GamePlayer;

			if (sender != m_questPlayer)
				return;

			if (player == null || player.IsDoingQuest(typeof(TheLostSeed)) == null)
				return;

			if (e == GameLivingEvent.EnemyKilled && Step == 5 && player.TargetObject.Name == Feairna_Athar.Name)
			{
				if (!m_questPlayer.Inventory.IsSlotsFree(1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
					player.Out.SendMessage(
						"You dont have enough room for " + glowing_red_jewel.Name + " and drops on the ground.",
						eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				GiveItem(player, glowing_red_jewel);
				Step = 6;
			}
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				if (m_questPlayer.Level >= 49)
					m_questPlayer.GainExperience(eXPSource.Quest,
						(m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel) / 3, false);
				else
					m_questPlayer.GainExperience(eXPSource.Quest,
						(m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel) / 2, false);
				GiveItem(m_questPlayer, paidrean_necklace);
				m_questPlayer.AddMoney(Money.GetMoney(0, 0, 121, 41, Util.Random(50)), "You receive {0} as a reward.");


				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			} 
			else
			{
				m_questPlayer.Out.SendMessage("You do not have enough free space in your inventory!",
					eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			}
		}
	}
}
