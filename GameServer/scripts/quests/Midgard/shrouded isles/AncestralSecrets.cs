/*
 * Atlas SI Quest - Atlas 1.65v Classic Freeshard
 */
/*
*Author         : Kelt
*Editor			: Kelt
*Source         : SI Quest
*Date           : 09 June 2022
*Quest Name     : Ancestral Secrets
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
	public class AncestralSecrets : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "Ancestral Secrets";
		protected const int minimumLevel = 48;
		protected const int maximumLevel = 50;

		private static GameNPC OtaYrling = null; // Start NPC + Finish NPC
		private static GameNPC Jaklyr = null; // 
		private static GameNPC Longbeard = null; // 
		private static GameNPC Styr = null; // 
		
		private static GameNPC AncestralKeeper = null; //Mob to Kill
		
		private static readonly GameLocation keeperLocation = new("Ancestral Keeper", 151, 363016, 310849, 3933);
		
		private static IArea keeperArea;

		private static ItemTemplate beaded_resisting_stone;
		private static ItemTemplate stone_pendant;
		private static ItemTemplate quest_pendant;
		
		// Constructors
		public AncestralSecrets() : base()
		{
		}

		public AncestralSecrets(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public AncestralSecrets(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public AncestralSecrets(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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
			
			 var npcs = WorldMgr.GetNPCsByName("Ota Yrling", eRealm.Midgard);

        if (npcs.Length > 0)
            foreach (var npc in npcs)
                if (npc.CurrentRegionID == 151 && npc.X == 291615 && npc.Y == 354310)
                {
	                OtaYrling = npc;
                    break;
                }

        if (OtaYrling == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Ota Yrling, creating it ...");
            OtaYrling = new GameNPC();
            OtaYrling.Model = 230;
            OtaYrling.Name = "Ota Yrling";
            OtaYrling.GuildName = "";
            OtaYrling.Realm = eRealm.Midgard;
            OtaYrling.CurrentRegionID = 151;
            OtaYrling.LoadEquipmentTemplateFromDatabase("95ff9192-4787-4dca-bcbb-7a081d801074");
            OtaYrling.Size = 49;
            OtaYrling.Level = 50;
            OtaYrling.X = 291615;
            OtaYrling.Y = 354310;
            OtaYrling.Z = 3866;
            OtaYrling.Heading = 739;
            OtaYrling.AddToWorld();
            if (SAVE_INTO_DATABASE) OtaYrling.SaveIntoDatabase();
        }

        npcs = WorldMgr.GetNPCsByName("Jaklyr", eRealm.Midgard);

        if (npcs.Length > 0)
            foreach (var npc in npcs)
                if (npc.CurrentRegionID == 151 && npc.X == 289376 && npc.Y == 304521)
                {
	                Jaklyr = npc;
                    break;
                }

        if (Jaklyr == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Jaklyr , creating it ...");
            Jaklyr = new GameNPC();
            Jaklyr.Model = 203;
            Jaklyr.Name = "Jaklyr";
            Jaklyr.GuildName = "";
            Jaklyr.Realm = eRealm.Midgard;
            Jaklyr.CurrentRegionID = 151;
            Jaklyr.LoadEquipmentTemplateFromDatabase("MidTownsperson4");
            Jaklyr.Size = 52;
            Jaklyr.Level = 60;
            Jaklyr.X = 289376;
            Jaklyr.Y = 304521;
            Jaklyr.Z = 4253;
            Jaklyr.Heading = 1841;
            Jaklyr.AddToWorld();
            if (SAVE_INTO_DATABASE) Jaklyr.SaveIntoDatabase();
        }
        // end npc

        npcs = WorldMgr.GetNPCsByName("Longbeard", eRealm.Midgard);
        if (npcs.Length > 0)
            foreach (var npc in npcs)
                if (npc.CurrentRegionID == 151 && npc.X == 290742 && npc.Y == 355471)
                {
	                Longbeard = npc;
                    break;
                }

        if (Longbeard == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Longbeard, creating it ...");
            Longbeard = new GameNPC();
            Longbeard.LoadEquipmentTemplateFromDatabase("be600079-ca29-4093-953a-3ee3aa1552e8");
            Longbeard.Model = 232;
            Longbeard.Name = "Longbeard";
            Longbeard.GuildName = "";
            Longbeard.Realm = eRealm.Midgard;
            Longbeard.CurrentRegionID = 151;
            Longbeard.Size = 53;
            Longbeard.Level = 50;
            Longbeard.X = 290742;
            Longbeard.Y = 355471;
            Longbeard.Z = 3867;
            Longbeard.Heading = 1695;
            Longbeard.VisibleActiveWeaponSlots = 34;
            Longbeard.MaxSpeedBase = 200;
            Longbeard.AddToWorld();
            if (SAVE_INTO_DATABASE) Longbeard.SaveIntoDatabase();
        }
        // end npc

        npcs = WorldMgr.GetNPCsByName("Styr", eRealm.Midgard);
        if (npcs.Length > 0)
	        foreach (var npc in npcs)
		        if (npc.CurrentRegionID == 151 && npc.X == 290643 && npc.Y == 355275)
		        {
			        Styr = npc;
			        break;
		        }

        if (Styr == null)
        {
	        if (log.IsWarnEnabled)
		        log.Warn("Could not find Styr, creating it ...");
	        Styr = new GameNPC();
	        Styr.LoadEquipmentTemplateFromDatabase("dbdb0127-cbbe-42b5-b60a-3cdc27256ae9");
	        Styr.Model = 235;
	        Styr.Name = "Styr";
	        Styr.GuildName = "";
	        Styr.Realm = eRealm.Midgard;
	        Styr.CurrentRegionID = 151;
	        Styr.Size = 51;
	        Styr.Level = 50;
	        Styr.X = 290643;
	        Styr.Y = 355275;
	        Styr.Z = 3867;
	        Styr.Heading = 3725;
	        Styr.VisibleActiveWeaponSlots = 34;
	        Styr.MaxSpeedBase = 200;
	        Styr.AddToWorld();
	        if (SAVE_INTO_DATABASE) Styr.SaveIntoDatabase();
        }
        // end npc
			#endregion

			#region defineItems
			beaded_resisting_stone = GameServer.Database.FindObjectByKey<ItemTemplate>("beaded_resisting_stone");
	        if (beaded_resisting_stone == null)
	        {
	            if (log.IsWarnEnabled)
	                log.Warn("Could not find Beaded Resisting Stone, creating it ...");
	            beaded_resisting_stone = new ItemTemplate();
	            beaded_resisting_stone.Id_nb = "beaded_resisting_stone";
	            beaded_resisting_stone.Name = "Beaded Resisting Stone";
	            beaded_resisting_stone.Level = 51;
	            beaded_resisting_stone.Durability = 50000;
	            beaded_resisting_stone.MaxDurability = 50000;
	            beaded_resisting_stone.Condition = 50000;
	            beaded_resisting_stone.MaxCondition = 50000;
	            beaded_resisting_stone.Item_Type = 29;
	            beaded_resisting_stone.Object_Type = (int) eObjectType.Magical;
	            beaded_resisting_stone.Model = 101;
	            beaded_resisting_stone.Bonus = 35;
	            beaded_resisting_stone.IsDropable = true;
	            beaded_resisting_stone.IsTradable = true;
	            beaded_resisting_stone.IsIndestructible = false;
	            beaded_resisting_stone.IsPickable = true;
	            beaded_resisting_stone.Bonus1 = 10;
	            beaded_resisting_stone.Bonus2 = 10;
	            beaded_resisting_stone.Bonus3 = 10;
	            beaded_resisting_stone.Bonus4 = 10;
	            beaded_resisting_stone.Bonus1Type = 11;
	            beaded_resisting_stone.Bonus2Type = 19;
	            beaded_resisting_stone.Bonus3Type = 18;
	            beaded_resisting_stone.Bonus4Type = 13;
	            beaded_resisting_stone.Price = 0;
	            beaded_resisting_stone.Realm = (int) eRealm.Midgard;
	            beaded_resisting_stone.DPS_AF = 0;
	            beaded_resisting_stone.SPD_ABS = 0;
	            beaded_resisting_stone.Hand = 0;
	            beaded_resisting_stone.Type_Damage = 0;
	            beaded_resisting_stone.Quality = 100;
	            beaded_resisting_stone.Weight = 10;
	            beaded_resisting_stone.LevelRequirement = 50;
	            beaded_resisting_stone.BonusLevel = 30;
	            beaded_resisting_stone.Description = "";
	            if (SAVE_INTO_DATABASE) GameServer.Database.AddObject(beaded_resisting_stone);
	        }
	        
	        quest_pendant = GameServer.Database.FindObjectByKey<ItemTemplate>("quest_pendant");
	        if (quest_pendant == null)
	        {
		        if (log.IsWarnEnabled)
			        log.Warn("Could not find Lightly Decorated Pendant, creating it ...");
		        quest_pendant = new ItemTemplate();
		        quest_pendant.Id_nb = "quest_pendant";
		        quest_pendant.Name = "Lightly Decorated Pendant";
		        quest_pendant.Level = 50;
		        quest_pendant.Item_Type = 0;
		        quest_pendant.Model = 101;
		        quest_pendant.IsDropable = true;
		        quest_pendant.IsTradable = false;
		        quest_pendant.IsIndestructible = true;
		        quest_pendant.IsPickable = true;
		        quest_pendant.DPS_AF = 0;
		        quest_pendant.SPD_ABS = 0;
		        quest_pendant.Object_Type = 0;
		        quest_pendant.Hand = 0;
		        quest_pendant.Type_Damage = 0;
		        quest_pendant.Quality = 100;
		        quest_pendant.Weight = 1;
		        quest_pendant.Description = "A lightly decorated pendant with slight rusted spots.";
		        if (SAVE_INTO_DATABASE) GameServer.Database.AddObject(quest_pendant);
	        }
	        
	        stone_pendant = GameServer.Database.FindObjectByKey<ItemTemplate>("stone_pendant");
	        if (stone_pendant == null)
	        {
		        if (log.IsWarnEnabled)
			        log.Warn("Could not find Stone Pendant, creating it ...");
		        stone_pendant = new ItemTemplate();
		        stone_pendant.Id_nb = "stone_pendant";
		        stone_pendant.Name = "Stone Pendant";
		        stone_pendant.Level = 50;
		        stone_pendant.Item_Type = 0;
		        stone_pendant.Model = 624;
		        stone_pendant.IsDropable = true;
		        stone_pendant.IsTradable = false;
		        stone_pendant.IsIndestructible = true;
		        stone_pendant.IsPickable = true;
		        stone_pendant.DPS_AF = 0;
		        stone_pendant.SPD_ABS = 0;
		        stone_pendant.Object_Type = 0;
		        stone_pendant.Hand = 0;
		        stone_pendant.Type_Damage = 0;
		        stone_pendant.Quality = 100;
		        stone_pendant.Weight = 1;
		        stone_pendant.Description = "A stone pendant with magical decorative writings.";
		        if (SAVE_INTO_DATABASE) GameServer.Database.AddObject(stone_pendant);
	        }
			#endregion

			const int radius = 1000;
			var region = WorldMgr.GetRegion(keeperLocation.RegionID);
			keeperArea = region.AddArea(new Area.Circle("cursed crystals", keeperLocation.X, keeperLocation.Y, keeperLocation.Z,
				radius));
			keeperArea.RegisterPlayerEnter(PlayerEnterKeeperArea);
			
			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
			
			GameEventMgr.AddHandler(OtaYrling, GameObjectEvent.Interact, TalkToOtaYrling);
			GameEventMgr.AddHandler(OtaYrling, GameLivingEvent.WhisperReceive, TalkToOtaYrling);

			GameEventMgr.AddHandler(Jaklyr, GameObjectEvent.Interact, TalkToJaklyr);
			GameEventMgr.AddHandler(Jaklyr, GameLivingEvent.WhisperReceive, TalkToJaklyr);

			GameEventMgr.AddHandler(Longbeard, GameObjectEvent.Interact, TalkToLongbeard);
			GameEventMgr.AddHandler(Longbeard, GameLivingEvent.WhisperReceive, TalkToLongbeard);
			
			GameEventMgr.AddHandler(Styr, GameObjectEvent.Interact, TalkToStyr);
			GameEventMgr.AddHandler(Styr, GameLivingEvent.WhisperReceive, TalkToStyr);
			
			/* Now we bring to Ota Yrling the possibility to give this quest to players */
			OtaYrling?.AddQuestToGive(typeof (AncestralSecrets));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (OtaYrling == null)
				return;
			
			// remove handlers
			keeperArea.UnRegisterPlayerEnter(PlayerEnterKeeperArea);
			WorldMgr.GetRegion(keeperLocation.RegionID).RemoveArea(keeperArea);
			
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
			
			GameEventMgr.RemoveHandler(OtaYrling, GameObjectEvent.Interact, TalkToOtaYrling);
			GameEventMgr.RemoveHandler(OtaYrling, GameLivingEvent.WhisperReceive, TalkToOtaYrling);

			GameEventMgr.RemoveHandler(Jaklyr, GameObjectEvent.Interact, TalkToJaklyr);
			GameEventMgr.RemoveHandler(Jaklyr, GameLivingEvent.WhisperReceive, TalkToJaklyr);

			GameEventMgr.RemoveHandler(Longbeard, GameObjectEvent.Interact, TalkToLongbeard);
			GameEventMgr.RemoveHandler(Longbeard, GameLivingEvent.WhisperReceive, TalkToLongbeard);
			
			GameEventMgr.RemoveHandler(Styr, GameObjectEvent.Interact, TalkToStyr);
			GameEventMgr.RemoveHandler(Styr, GameLivingEvent.WhisperReceive, TalkToStyr);
			
			/* Now we remove to Ota Yrling the possibility to give this quest to players */
			OtaYrling.RemoveQuestToGive(typeof (AncestralSecrets));
		}

		protected virtual void CreateAncestralKeeper(GamePlayer player)
		{
		
			AncestralKeeper = new GameNPC();
			AncestralKeeper.Model = 951;
			AncestralKeeper.Name = "Ancestral Keeper";
			AncestralKeeper.GuildName = "";
			AncestralKeeper.Realm = eRealm.None;
			AncestralKeeper.Race = 2003;
			AncestralKeeper.BodyType = (ushort) NpcTemplateMgr.eBodyType.Elemental;
			AncestralKeeper.CurrentRegionID = 151;
			AncestralKeeper.Size = 140;
			AncestralKeeper.Level = 65;
			AncestralKeeper.ScalingFactor = 60;
			AncestralKeeper.X = player.X;
			AncestralKeeper.Y = player.Y;
			AncestralKeeper.Z = player.Z;
			AncestralKeeper.MaxSpeedBase = 250;
			AncestralKeeper.AddToWorld();

			var brain = new StandardMobBrain();
			brain.AggroLevel = 200;
			brain.AggroRange = 500;
			AncestralKeeper.SetOwnBrain(brain);

			AncestralKeeper.AddToWorld();

			AncestralKeeper.StartAttack(player);
		}
		
		private static void PlayerEnterKeeperArea(DOLEvent e, object sender, EventArgs args)
		{
			var aargs = args as AreaEventArgs;
			var player = aargs?.GameObject as GamePlayer;

			if (player == null)
				return;

			var quest = player.IsDoingQuest(typeof(AncestralSecrets)) as AncestralSecrets;

			if (quest is not {Step: 4}) return;

			if (player.Group != null)
				if (player.Group.Leader != player)
					return;
			
			var existingCopy = WorldMgr.GetNPCsByName("Ancestral Keeper", eRealm.None);

			if (existingCopy.Length > 0) return;

			// player near ancestral keeper           
			SendSystemMessage(player,
				"The Crystal Breaks and The Ancestral Keeper comes alive!");
			player.Out.SendMessage("Ancestral Keeper ambushes you!", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
			quest.CreateAncestralKeeper(player);
		}
		
		protected static void TalkToOtaYrling(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(OtaYrling.CanGiveQuest(typeof (AncestralSecrets), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			AncestralSecrets quest = player.IsDoingQuest(typeof (AncestralSecrets)) as AncestralSecrets;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							OtaYrling.SayTo(player, "Once densely populated by dwarves, this changed with the [impact] of a meteorite. " +
							                        "Wide areas were devastated and forests were set on fire. Even today, the wounds of this event are still clearly visible.");
							break;
						case 2:
							OtaYrling.SayTo(player, "Hey "+player.Name+", don't listen to Longbeard and his friend Styr, they came from Dellingstad and fled.");
							
							int random = Util.Random(0, 3);
							var message = "";
							switch (random)
							{
								case 0:
									Longbeard.Emote(eEmote.Laugh);
									Longbeard.TurnTo(player);
									Styr.Emote(eEmote.Laugh);
									message = "Longbeard yells, \"Haha, another idiot trying to help Ota Yrling\".";
									break;
								case 1: 
									Longbeard.Emote(eEmote.Rofl);
									Longbeard.TurnTo(player);
									Styr.Emote(eEmote.Laugh);
									message = $"Longbeard yells, \"Haha, Styr look at this \"{player.CharacterClass.Name}\"";
									break;
								case 2: 
									Longbeard.Emote(eEmote.Laugh);
									Styr.TurnTo(player);
									Styr.Emote(eEmote.Rofl);
									message = $"Styr yells, \"Haha, Longbeard look at this \"{player.CharacterClass.Name}\"";
									break;
								case 3: 
									Longbeard.Emote(eEmote.Laugh);
									Styr.TurnTo(player);
									Styr.Emote(eEmote.Laugh);
									message = "Styr yells, \"Haha, another idiot trying to help Ota Yrling\".";
									break;
							}
							SendMessage(player, message, 0,eChatType.CT_Say, eChatLoc.CL_ChatWindow);
							break;
						case 3:
							OtaYrling.SayTo(player, "Hey "+player.Name+", please visit Jaklyr in Bjarken and tell him that I sent you, he will understand.");
							break;
						case 4:
							OtaYrling.SayTo(player, "Greetings, thank you for your courage, I am with you mentally. Jaklyr might told you how you find the Delling Crater, right?");
							break;
						case 5:
							OtaYrling.SayTo(player, "God dag my friend, I am happy that you did it, please bring this magical pendant to Jaklyr in Bjarken.");
							break;
						case 6:
							OtaYrling.SayTo(player, "Many Years have passed and you made it, not only me but all of Midgard thanks you! You deserved your [reward]!");
							break;
					}
				}
				else
				{
					OtaYrling.SayTo(player, "Hello " + player.Name +
					                        ", I need to talk to you, do you have a moment?\n" +
					                        "Many Dwarfs can't work in the Delling Crater anymore. " +
					                        "I heard that there is a creature which kills everything that comes close to it. It is like a [Curse].");
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
						case "Curse":
							player.Out.SendQuestSubscribeCommand(OtaYrling, QuestMgr.GetIDForQuestType(typeof(AncestralSecrets)), "Will you help Ota Yrling find [Ancestral Secrets]?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "impact":
							OtaYrling.SayTo(player, player.Name+", we need your help finding [secrets] in the Delling Crater.");
							break;
						case "secrets":
							if (quest.Step == 1)
							{
								OtaYrling.SayTo(player, "Please visit Jaklyr in Bjarken and tell him that I se... Oh no Longbeard and his friend Styr...");
								Longbeard.Yell("Haha, you need help from this "+player.CharacterClass.Name+" Ota Yrling?");
								Longbeard.Emote(eEmote.Laugh);
								Styr.Emote(eEmote.Laugh);
								quest.Step = 2;
							}
							break;
						case "reward":
							if (quest.Step == 6)
							{
								Longbeard.Yell("Hey "+player.Name+", thank you for your help in Delling Crater!");
								Longbeard.Emote(eEmote.Clap);
								Styr.Emote(eEmote.Cheer);
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
					if (rArgs.Item.Id_nb == stone_pendant.Id_nb)
					{
						if (quest.Step == 6)
						{
							OtaYrling.SayTo(player, "Many Years have passed and you made it, not only me but all of Midgard thanks you! You have deserved your [reward]!");
						}
					}
			}
		}
		
		protected static void TalkToJaklyr(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(OtaYrling.CanGiveQuest(typeof (AncestralSecrets), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			AncestralSecrets quest = player.IsDoingQuest(typeof (AncestralSecrets)) as AncestralSecrets;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Jaklyr.SayTo(player, "Hello Adventurer, great to see more people in our town. Can I help you?");
							break;
						case 2:
							Jaklyr.SayTo(player, "Hey "+player.CharacterClass.Name+", did you hear about two dwarfs who fled from Dellingstad and living now in Aegirhamn? " +
							                     "Ota Yrling said that they are annoying.");
							break;
						case 3:
							Jaklyr.SayTo(player, "Hey "+player.CharacterClass.Name+", how can I help you? Did someone [sent] you?");
							break;
						case 4:
							Jaklyr.SayTo(player, "I wish you all the strength you need for your adventure!");
							Jaklyr.SayTo(player,
								"Please head to the Caldera in Delling Crater. Follow the road west and at the crossroads go north towards Delling Crater. " +
								"Search for Ancestral Keeper in the crater and kill it.");
							break;
						case 5:
							Jaklyr.SayTo(player, "You did it! Outstanding my friend! Please hand me the [pendant]!");
							break;
						case 6:
							Jaklyr.SayTo(player, "");
							break;
					}
				}
				else
				{
					Jaklyr.SayTo(player, "");
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
						case "sent":
							Jaklyr.SayTo(player, "Oh, Ota Yrling sent you, I know why...\n" +
							                     "After the meteorite impact, complications arose that did not exist before. The dwarves of Dellingstad were once friendly and helpful. " +
							                     "Now you have to kill elemental creatures to be accepted. I heard of a creature named Ancestral Keeper. Times are dark at [Delling Crater]. " +
							                     "I want you to set out and pursue this.");
							break;
						case "Delling Crater":
							if (quest.Step == 3)
							{
								if (player.Inventory.IsSlotsFree(1, eInventorySlot.FirstBackpack,
									    eInventorySlot.LastBackpack))
								{
									Jaklyr.SayTo(player, "Head to the Caldera in Delling Crater. Follow the road west and at the crossroads go north towards Delling Crater. " +
									                     "Search for Ancestral Keeper in the crater and kill it. " +
									                     "Take this pendant with you as lucky charm!");
									GiveItem(player, quest_pendant);
									quest.Step = 4;
								}
								else
								{
									Jaklyr.SayTo(player, "Please make room in your inventory for a lucky charm!");
								}
							}
							break;
						case "pendant":
							RemoveItem(player, stone_pendant);
							Jaklyr.SayTo(player, "I knew it, the Ancestral Keeper has lost its magic and is now [trapped] in this pendant.");
							Jaklyr.Emote(eEmote.Cheer);
							break;
						case "trapped":
							if (quest.Step == 5)
							{
								Jaklyr.SayTo(player, "Take it back and return to Ota Yrling in Aegirhamn. Bring her the pendant as a gift!");
								GiveItem(player, stone_pendant);
								quest.Step = 6;
							}
							break;
					}
				}
			}
			else if (e == GameLivingEvent.ReceiveItem)
			{
				ReceiveItemEventArgs rArgs = (ReceiveItemEventArgs) args;
				if (quest != null)
					if (rArgs.Item.Id_nb == stone_pendant.Id_nb)
					{
						if (quest.Step == 5)
						{
							Jaklyr.SayTo(player, "I knew it, the Ancestral Keeper has lost its magic and is now [trapped] in this pendant.");
							Jaklyr.Emote(eEmote.Cheer);
						}
					}
			}
		}
		
		protected static void TalkToLongbeard(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(OtaYrling.CanGiveQuest(typeof (AncestralSecrets), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			AncestralSecrets quest = player.IsDoingQuest(typeof (AncestralSecrets)) as AncestralSecrets;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Longbeard.SayTo(player, "Yes, I am a Dwarf from Dellingstad, do you have a problem with that?");
							break;
						case 2:
							Longbeard.SayTo(player, "You are kidding me right? Nobody came back, just [don't try] it kid.");
							break;
						case 3:
							Longbeard.SayTo(player, "Hey "+player.CharacterClass.Name+", have you visited Jaklyr in Bjarken yet? I thought you want to go to Delling Crater.");
							break;
						case 4:
							Longbeard.SayTo(player, "I wish you good luck my friend! It's not an easy mission.");
							break;
						case 5:
							Longbeard.SayTo(player, "Wow, I really never thought that you will do it. That's great my friend! Does Jaklyr know about it yet?");
							break;
						case 6:
							Longbeard.SayTo(player, "Congratulations "+player.Name+", you will get your recognition!");
							break;
					}
				}
				else
				{
					Longbeard.SayTo(player, "Hey, do you have a boar pelt? I would buy it.");
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
						case "don't try":
							Longbeard.SayTo(player, "You'll never come back alive. " +
							                        "There has been a curse since [the crater] has formed.");
							break;
						case "the crater":
							Longbeard.SayTo(player, "Yeah a crater, many years ago a meteorite fell from the sky. It's right next to Dellingstad, that's why its called Delling Crater." +
							                        "Everyone in Dellingstad started to get weird. They hardly ate anymore and they began hunting certain creatures. Styr and I fled." +
							                        "If you are intelligent enough, then you shouldn't accept this [challenge].");
							Longbeard.Emote(eEmote.Induct);
							break;
						case "challenge":
							if (quest.Step == 2)
							{
								Longbeard.SayTo(player, "Okay Adventurer, I warned you, but if you need help, then visit Jaklyr in Bjarken, he knows as much as I do about this event.\nHa det!");
								Longbeard.Emote(eEmote.Wave);
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
		
		protected static void TalkToStyr(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(OtaYrling.CanGiveQuest(typeof (AncestralSecrets), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			AncestralSecrets quest = player.IsDoingQuest(typeof (AncestralSecrets)) as AncestralSecrets;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Styr.SayTo(player, "Hey Adventurer, I am Styr and you?");
							break;
						case 2:
							Styr.SayTo(player, "Do you really think that this mission is easy?");
							break;
						case 3:
							Styr.SayTo(player, "Jaklyr will indeed help you, but it will be difficult!");
							break;
						case 4:
							Styr.SayTo(player, "Good luck my friend, you will need it for this mission.");
							break;
						case 5:
							Styr.SayTo(player, "Wait, you did it? Does Jaklyr knows about it already?");
							break;
						case 6:
							Styr.SayTo(player, "I'm sorry for my laughter, you are great!");
							break;
						
					}
				}
				else
				{
					Styr.SayTo(player, "Greetings, sometimes I need my walk at the port.");
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
				if (quest != null){}

			}
		}
		
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (AncestralSecrets)) != null)
				return true;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			AncestralSecrets quest = player.IsDoingQuest(typeof (AncestralSecrets)) as AncestralSecrets;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(AncestralSecrets)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(OtaYrling.CanGiveQuest(typeof (AncestralSecrets), player)  <= 0)
				return;

			if(player.IsDoingQuest(typeof (AncestralSecrets)) != null)
				return;

			if (response == 0x00)
			{
				OtaYrling.SayTo(player, "Please come back, if you want to help me!");
			}
			else
			{
				//Check if we can add the quest!
				if (!OtaYrling.GiveQuest(typeof (AncestralSecrets), player, 1))
					return;
			}
			OtaYrling.SayTo(player, "Thanks "+player.Name+", finally someone helps me!");
			OtaYrling.SayTo(player, "Once densely populated by dwarves, this changed with the [impact] of a meteorite. " +
			                        "Wide areas were devastated and forests were set on fire. Even today, the wounds of this event are clearly visible.");
			
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
						return "Speak to Ota Yrling in Aegirhamn.";
					case 2:
						return "Face the statements from Longbeard and Styr in Aegirhamn.";
					case 3:
						return "Speak to Jaklyr in Bjarken.";
					case 4:
						return "Head to the Caldera in Delling Crater. Follow the road west and at the crossroads go north towards Delling Crater." +
						       "Search for Ancestral Keeper in the crater and kill it.";
					case 5:
						return "Return the Stone Pendant to Jaklyr in Bjarken.";
					case 6:
						return "Return to Ota Yrling in Aegirhamn.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			var player = sender as GamePlayer;

			if (sender != m_questPlayer)
				return;

			if (player == null || player.IsDoingQuest(typeof(AncestralSecrets)) == null)
				return;

			if (e == GameLivingEvent.EnemyKilled && Step == 4 && player.TargetObject.Name == AncestralKeeper.Name)
			{
				RemoveItem(player, quest_pendant);
				SendSystemMessage("You feel the curse lift and the pendant turn into a powerful chain.");
				GiveItem(player, stone_pendant);
				Step = 5;
			}
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			RemoveItem(m_questPlayer, quest_pendant);
			RemoveItem(m_questPlayer, stone_pendant);
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
				RemoveItem(m_questPlayer, stone_pendant);
				GiveItem(m_questPlayer, beaded_resisting_stone);
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
