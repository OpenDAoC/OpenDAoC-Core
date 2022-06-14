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
			paidrean_necklace = GameServer.Database.FindObjectByKey<ItemTemplate>("paidrean_necklace");
	        if (paidrean_necklace == null)
	        {
	            if (log.IsWarnEnabled)
	                log.Warn("Could not find Paidrean Necklace, creating it ...");
	            paidrean_necklace = new ItemTemplate();
	            paidrean_necklace.Id_nb = "paidrean_necklace";
	            paidrean_necklace.Name = "Paidrean Necklace";
	            paidrean_necklace.Level = 51;
	            paidrean_necklace.Durability = 50000;
	            paidrean_necklace.MaxDurability = 50000;
	            paidrean_necklace.Condition = 50000;
	            paidrean_necklace.MaxCondition = 50000;
	            paidrean_necklace.Item_Type = 29;
	            paidrean_necklace.Object_Type = (int) eObjectType.Magical;
	            paidrean_necklace.Model = 101;
	            paidrean_necklace.Bonus = 35;
	            paidrean_necklace.IsDropable = true;
	            paidrean_necklace.IsTradable = true;
	            paidrean_necklace.IsIndestructible = false;
	            paidrean_necklace.IsPickable = true;
	            paidrean_necklace.Bonus1 = 10;
	            paidrean_necklace.Bonus2 = 10;
	            paidrean_necklace.Bonus3 = 10;
	            paidrean_necklace.Bonus4 = 10;
	            paidrean_necklace.Bonus1Type = 11;
	            paidrean_necklace.Bonus2Type = 19;
	            paidrean_necklace.Bonus3Type = 18;
	            paidrean_necklace.Bonus4Type = 13;
	            paidrean_necklace.Price = 0;
	            paidrean_necklace.Realm = (int) eRealm.Hibernia;
	            paidrean_necklace.DPS_AF = 0;
	            paidrean_necklace.SPD_ABS = 0;
	            paidrean_necklace.Hand = 0;
	            paidrean_necklace.Type_Damage = 0;
	            paidrean_necklace.Quality = 100;
	            paidrean_necklace.Weight = 10;
	            paidrean_necklace.LevelRequirement = 50;
	            paidrean_necklace.BonusLevel = 30;
	            paidrean_necklace.Description = "";
	            if (SAVE_INTO_DATABASE) GameServer.Database.AddObject(paidrean_necklace);
	        }
	        glowing_red_jewel = GameServer.Database.FindObjectByKey<ItemTemplate>("glowing_red_jewel");
	        if (glowing_red_jewel == null)
	        {
		        if (log.IsWarnEnabled)
			        log.Warn("Could not find Glowing Red Jewel, creating it ...");
		        glowing_red_jewel = new ItemTemplate();
		        glowing_red_jewel.Id_nb = "glowing_red_jewel";
		        glowing_red_jewel.Name = "Glowing Red Jewel";
		        glowing_red_jewel.Level = 55;
		        glowing_red_jewel.Item_Type = 0;
		        glowing_red_jewel.Model = 110;
		        glowing_red_jewel.IsDropable = true;
		        glowing_red_jewel.IsTradable = false;
		        glowing_red_jewel.IsIndestructible = true;
		        glowing_red_jewel.IsPickable = true;
		        glowing_red_jewel.DPS_AF = 0;
		        glowing_red_jewel.SPD_ABS = 0;
		        glowing_red_jewel.Object_Type = 0;
		        glowing_red_jewel.Hand = 0;
		        glowing_red_jewel.Type_Damage = 0;
		        glowing_red_jewel.Quality = 100;
		        glowing_red_jewel.Weight = 1;
		        glowing_red_jewel.Description = "A jewel of unnatural power.";
		        if (SAVE_INTO_DATABASE) GameServer.Database.AddObject(glowing_red_jewel);
	        }
			#endregion

			const int radius = 1500;
			var region = WorldMgr.GetRegion(treantLocation.RegionID);
			treantArea = region.AddArea(new Area.Circle("accursed piece of forest", treantLocation.X, treantLocation.Y, treantLocation.Z,
				radius));
			treantArea.RegisterPlayerEnter(PlayerEnterTreantArea);
			
			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
			
			GameEventMgr.AddHandler(Terod, GameObjectEvent.Interact, TalkToTerod);
			GameEventMgr.AddHandler(Terod, GameLivingEvent.WhisperReceive, TalkToTerod);

			GameEventMgr.AddHandler(Kredril, GameObjectEvent.Interact, TalkToKredril);
			GameEventMgr.AddHandler(Kredril, GameLivingEvent.WhisperReceive, TalkToKredril);

			GameEventMgr.AddHandler(Emolia, GameObjectEvent.Interact, TalkToEmolia);
			GameEventMgr.AddHandler(Emolia, GameLivingEvent.WhisperReceive, TalkToEmolia);
			
			GameEventMgr.AddHandler(Jandros, GameObjectEvent.Interact, TalkToJandros);
			GameEventMgr.AddHandler(Jandros, GameLivingEvent.WhisperReceive, TalkToJandros);
			
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
			
			GameEventMgr.RemoveHandler(Terod, GameObjectEvent.Interact, TalkToTerod);
			GameEventMgr.RemoveHandler(Terod, GameLivingEvent.WhisperReceive, TalkToTerod);

			GameEventMgr.RemoveHandler(Kredril, GameObjectEvent.Interact, TalkToKredril);
			GameEventMgr.RemoveHandler(Kredril, GameLivingEvent.WhisperReceive, TalkToKredril);

			GameEventMgr.RemoveHandler(Emolia, GameObjectEvent.Interact, TalkToEmolia);
			GameEventMgr.RemoveHandler(Emolia, GameLivingEvent.WhisperReceive, TalkToEmolia);
			
			GameEventMgr.RemoveHandler(Jandros, GameObjectEvent.Interact, TalkToJandros);
			GameEventMgr.RemoveHandler(Jandros, GameLivingEvent.WhisperReceive, TalkToJandros);
			
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
			Feairna_Athar.Size = 100;
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
						case 1:
							Terod.SayTo(player, "I am very glad that you decided to help us! The [treant] in Cothrom Gorge is brutal and very aggressive. I heard it likes to torture everything that stands in its way.");
							break;
						case 2:
							Terod.SayTo(player, "Kredril has studied this treant for years, he will know much more about it. Seek for him in the outskirts of Droighaid, he will be able to help you.");
							break;
						case 3:
							Terod.SayTo(player, "Greetings "+player.CharacterClass.Name+", Kredril told me that you are on your way to visit Jandros, is that right? " +
							                    "Emolia can be found next to Droighaid's Bindstone, she will be able to help with your journey to Aalid Feie.");
							break;
						case 4:
							Terod.SayTo(player, "Hello "+player.Name+", have you visited Jandros yet? " +
							                    "You can find Jandros in one of those big trees in Aalid Feie.");
							break;
						case 5:
							Terod.SayTo(player, "Jandros has told me that you defeated the treant Feairna-Athar. We all stand behind and thank you for your courage!");
							break;
						case 6:
							Terod.SayTo(player, "I dont know what to say, you are outstanding! Bring this Glowing Red Jewel to Jandros, he knows what to do.");
							break;
						case 7:
							Terod.SayTo(player, "Welcome back hero of Hibernia, I think it's time for your [reward]!");
							break;
					}
				}
				else
				{
					Terod.SayTo(player, "Hello " + player.Name +
					                       ", do you have a moment to listen to my story?\n" +
					                       "A cursed treant rages in Cothrom Gorge and is threatening our realm. I have a good feeling ..could you be the one finally able to [help us]?");
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
						case "help us":
							Terod.SayTo(player, "Some friends and I have been looking for strong fighters and magicians to help us for a few days now. " +
							                    "It's about the [Lost Seed].");
							break;
						
						case "Lost Seed":
							player.Out.SendQuestSubscribeCommand(Terod, QuestMgr.GetIDForQuestType(typeof(TheLostSeed)), "Will you help Terod find [The Lost Seed]?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "treant":
							if (quest.Step == 1)
							{
								Terod.SayTo(player, "Kredril knows much more about this treant, go find him outside of Droighaid.");
								quest.Step = 2;
							}
							break;
						case "reward":
							if (quest.Step == 7)
							{
								quest.FinishQuest();
							}
							break;
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
						case 1:
							Kredril.SayTo(player, "Hello Adventurer, have you heard about the Lost Seed? " +
							                      "Find Terod in Droighaid, he will tell you more about it.");
							break;
						case 2:
							Kredril.SayTo(player, "Hey "+player.CharacterClass.Name+", has Terod told you about [the Lost Seed]?");
							break;
						case 3:
							Kredril.SayTo(player, "Hey "+player.Name+", you can find Emolia around Droighaid's Bindstone. She will teleport you to Aalid Feie.");
							break;
						case 4:
							Kredril.SayTo(player, player.Name+" have you visited Jandros yet? You can find him in one of those big trees.");
							break;
						case 5:
							Kredril.SayTo(player, "Jandros has told Terod and I that you faced the treant Feairna-Athar. Thank you for your help and courage!");
							break;
						case 6:
							Kredril.SayTo(player, "Outstanding! Now everyone can sleep well again! Bring this Glowing Red Jewel to Jandros, he will tell you what to do next.");
							break;
						case 7:
							Kredril.SayTo(player, "Hey "+player.Name+", go tell Terod about it. I think he will want to thank you in a special way.");
							break;
					}
				}
				else
				{
					Kredril.SayTo(player, "Hey "+player.Name+", today is a beautiful day, I hope for you too.");
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
						case "the Lost Seed":
							Kredril.SayTo(player, "Farmers of Hibernia found seeds in Cothrom Gorge, which were cursed. A Few seeds got destroyed, but one was lost. " +
							                      "Please visit [Jandros] in Aalid Feie, he might know where the farmers found those seeds.");
							break;
						case "Jandros":
							if (quest.Step == 2)
							{
								Kredril.SayTo(player, "Go to Emolia in Droighaid, she will teleport you to Aalid Feie. " +
								                      "You can find Jandros in one of those big trees. Tell him that I sent you, he will understand.");
								quest.Step = 3;
							}
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
		
		protected static void TalkToEmolia(DOLEvent e, object sender, EventArgs args)
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
						case 1:
							break;
						case 2:
							break;
						case 3:
							Emolia.Say("Hello Adventurer, Jandros awaits you.");
							break;
						case 4:
							Emolia.Say("Hello Adventurer, you can find Jandros in a big tree in Aalid Feie.");
							break;
						case 5:
							Emolia.Say("Hello Adventurer, I wish you good luck finding the treant in Cothrom Gorge.");
							break;
						case 6:
							Emolia.Say("Hello Adventurer, I can send you whenever you need.");
							break;
						case 7:
							Emolia.Say("Hello Adventurer, thank you for your help in Cothrom Gorge.");
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
						case "Aalid Feie":
							if (quest.Step == 3)
							{
								quest.Step = 4;
							}
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
						case 1:
							Jandros.SayTo(player, "Hello "+player.CharacterClass.Name+", I saw you in Aalid Feie for a few times. Have you visited Droighaid yet? " +
							                      "It's a beautiful place, my friends Kredril and Terod live there.");
							break;
						case 2:
							Jandros.SayTo(player, "Hey "+player.CharacterClass.Name+", I am sorry for seeming so distracted. We found a track for the Lost Seed.");
							break;
						case 3:
							Jandros.SayTo(player, "Hello "+player.Name+", has Emolia sent you? I wasn't expecting you anytime soon.");
							break;
						case 4:
							Jandros.SayTo(player, "Greetings "+player.Name+", I am glad that you are here, has [Kredril] sent you?");
							break;
						case 5:
							Jandros.SayTo(player, "Hey "+player.Name+", follow the path north towards Cothrom Gorge. " +
							                      "Once in the forest, head West. There, you will find the Treant Feairna-Athar. Kill him and bring me proof.");
							break;
						case 6:
							Jandros.SayTo(player, player.Name+" you are crazy. I know that you will do it! Please hand me [the Jewel].");
							break;
						case 7:
							Jandros.SayTo(player, "Hey "+player.Name+", have you visited Terod in Droighaid yet? Please do it, he needs to know about it.");
							break;
					}
				}
				else
				{
					Jandros.SayTo(player, "Hey "+player.Name+", I wish it rained way more often in Aalid Feie. I love the sound and feel.");
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
						case "Kredril":
							Jandros.SayTo(player, "Thank you for your courage and help, indeed we need fighter and magicians to help us finding [the Lost Seed].");
							break;
						case "the Lost Seed":
							Jandros.SayTo(player, "I think Kredril has already told you about the farmers that found some cursed magical seeds. Many [died] because of them.");
							break;
						case "died":
							Jandros.Emote(eEmote.Cry);
							Jandros.SayTo(player, "Some rangers went near the location where the farmers died and reported that something strange " +
							                      "is happening at this location. They saw a treant and named it [Feairna-Athar]. " +
							                      "I think it has to do with the Lost Seed!");
							break;
						case "Feairna-Athar":
							if (quest.Step == 4)
							{
								Jandros.SayTo(player, "Follow the path Morth towards Cothrom Gorge. " +
								                      "Once in the forest, head West. There, you will find the Treant Feairna-Athar. Kill him and bring me proof.");
								quest.Step = 5;
							}
							break;
						case "the Jewel":
							if (quest.Step == 6)
							{
								RemoveItem(player, glowing_red_jewel);
								Jandros.SayTo(player, "Please go back to Droighaid and tell Terod about it!");
								quest.Step = 7;
							}
							break;
					}
				}
			}
			else if (e == GameLivingEvent.ReceiveItem)
			{
				ReceiveItemEventArgs rArgs = (ReceiveItemEventArgs) args;
				if (quest != null)
					if (rArgs.Item.Id_nb == glowing_red_jewel.Id_nb)
					{
						if (quest.Step == 6)
						{
							Jandros.SayTo(player,
								"Thanks " + player.Name + ", please go back to Droighaid and tell Terod about it!");
							Jandros.Emote(eEmote.Smile);
							quest.Step = 7;
						}
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
				Terod.SayTo(player, "Please come back, if you changed your mind!");
			}
			else
			{
				//Check if we can add the quest!
				if (!Terod.GiveQuest(typeof (TheLostSeed), player, 1))
					return;
			}
			Terod.SayTo(player, "Thanks, let's talk more about the Lost Seed!");
			Terod.SayTo(player, "I am very glad that you decided to help us! The [treant] in Cothrom Gorge is brutal and very aggressive. I heard it likes to torture everything that stands in its way.");
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
						       "Once in the forest, head West. There, you will find the Treant Feairna-Athar. Kill him and bring me the jewel.";
					case 6:
						return "Return the Glowing Red Jewel to Jandros in Aalid Feie.";
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
			RemoveItem(m_questPlayer, glowing_red_jewel);
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
