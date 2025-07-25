using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using DOL.GS.Scripts;

namespace DOL.GS.AtlasQuest.Albion
{
	public class BeetlePvEQuestAlb : Quests.AtlasQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Beetle] A peace offering from a beetle";
		private const int minimumLevel = 50;
		private const int maximumLevel = 50;
		
		// Kill Goal
		private const int MAX_KILLED = 1;
		// Quest Counter
		private int _dragonKilled = 0;
		private int _legionKilled = 0;
		private int _grandSummonerKilled = 0;

		private static GameNPC Laura = null; // Start NPC
		private static GameNPC Beetle = null;
		private static GameNPC MobEffect = null;
		private static String ALB_DRAGON_NAME = "Golestandt";
		private static String LEGION_NAME = "Legion";
		private static String GRAND_SUMMONER_NAME = "Grand Summoner Govannon";
		
		private static DbItemTemplate beetle_egg = null;
		private static DbItemTemplate beetle_bone = null;
		
		// Constructors
		public BeetlePvEQuestAlb() : base()
		{
		}

		public BeetlePvEQuestAlb(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public BeetlePvEQuestAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public BeetlePvEQuestAlb(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
		{
		}

		public override int Level
		{
			get
			{
				// Quest Level
				return minimumLevel;
			}
		}
		
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			
			#region defineNPCs

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Laura", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 10 && npc.X == 36450 && npc.Y == 30958)
					{
						Laura = npc;
						break;
					}

			if (Laura == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Laura , creating it ...");
				Laura = new GameNPC();
				Laura.Model = 261;
				Laura.Name = "Laura";
				Laura.GuildName = "Protector of Beetles";
				Laura.Realm = eRealm.Albion;
				Laura.CurrentRegionID = 10;
				Laura.Size = 50;
				Laura.Level = 59;
				//Camelot Location
				Laura.X = 36450;
				Laura.Y = 30958;
				Laura.Z = 8010;
				Laura.Heading = 1012;
				GameNpcInventoryTemplate templateAlb = new GameNpcInventoryTemplate();
				templateAlb.AddNPCEquipment(eInventorySlot.Cloak, 559, 43);
				templateAlb.AddNPCEquipment(eInventorySlot.TorsoArmor, 1005, 23);
				templateAlb.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 43);
				templateAlb.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 43);
				Laura.Inventory = templateAlb.CloseTemplate();
				Laura.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Laura.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			beetle_egg = GameServer.Database.FindObjectByKey<DbItemTemplate>("beetle_egg");
			if (beetle_egg == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Beetle Egg, creating it ...");
				beetle_egg = new DbItemTemplate();
				beetle_egg.Id_nb = "beetle_egg";
				beetle_egg.Name = "Beetle Egg";
				beetle_egg.Level = 50;
				beetle_egg.Durability = 50000;
				beetle_egg.MaxDurability = 50000;
				beetle_egg.Condition = 50000;
				beetle_egg.MaxCondition = 50000;
				beetle_egg.Item_Type = 40;
				beetle_egg.Object_Type = 41;
				beetle_egg.Model = 587;
				beetle_egg.CanUseEvery = 300;
				beetle_egg.SpellID = 96000;
				beetle_egg.Charges = 10;
				beetle_egg.MaxCharges = 10;
				beetle_egg.IsDropable = true;
				beetle_egg.IsTradable = true;
				beetle_egg.IsIndestructible = false;
				beetle_egg.IsPickable = true;
				beetle_egg.Price = 100000;
				beetle_egg.MaxCount = 1;
				beetle_egg.BonusLevel = 49;
				beetle_egg.DPS_AF = 0;
				beetle_egg.SPD_ABS = 0;
				beetle_egg.Hand = 0;
				beetle_egg.PackSize = 1;
				beetle_egg.Type_Damage = 0;
				beetle_egg.Quality = 85;
				beetle_egg.Weight = 4;
				beetle_egg.Description = "Another growing Beetle. Breed it to get 50% Power.";
				if (SAVE_INTO_DATABASE)
					GameServer.Database.AddObject(beetle_egg);
			}
			
			beetle_bone = GameServer.Database.FindObjectByKey<DbItemTemplate>("beetle_bone");
			if (beetle_bone == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Beetle Bone, creating it ...");
				beetle_bone = new DbItemTemplate();
				beetle_bone.Id_nb = "beetle_bone";
				beetle_bone.Name = "Beetle Bone";
				beetle_bone.Level = 50;
				beetle_bone.Durability = 50000;
				beetle_bone.MaxDurability = 50000;
				beetle_bone.Condition = 50000;
				beetle_bone.MaxCondition = 50000;
				beetle_bone.Item_Type = 40;
				beetle_bone.Object_Type = 41;
				beetle_bone.Model = 105;
				beetle_bone.CanUseEvery = 300;
				beetle_bone.SpellID = 96001;
				beetle_bone.Charges = 10;
				beetle_bone.MaxCharges = 10;
				beetle_bone.IsDropable = true;
				beetle_bone.IsTradable = true;
				beetle_bone.IsIndestructible = false;
				beetle_bone.IsPickable = true;
				beetle_bone.Price = 100000;
				beetle_bone.MaxCount = 1;
				beetle_bone.DPS_AF = 0;
				beetle_bone.SPD_ABS = 0;
				beetle_bone.Hand = 0;
				beetle_bone.PackSize = 1;
				beetle_bone.Type_Damage = 0;
				beetle_bone.Quality = 85;
				beetle_bone.Weight = 4;
				beetle_bone.Description = "In memory of an honorable Beetle. Get 20% life back.";
				if (SAVE_INTO_DATABASE)
					GameServer.Database.AddObject(beetle_bone);
			}
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Laura, GameObjectEvent.Interact, new DOLEventHandler(TalkToLaura));
			GameEventMgr.AddHandler(Laura, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToLaura));
			
			Laura.AddQuestToGive(typeof (BeetlePvEQuestAlb));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Laura == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Laura, GameObjectEvent.Interact, new DOLEventHandler(TalkToLaura));
			GameEventMgr.RemoveHandler(Laura, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToLaura));

			Laura.RemoveQuestToGive(typeof (BeetlePvEQuestAlb));
		}

		private static void TalkToLaura(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Laura.CanGiveQuest(typeof (BeetlePvEQuestAlb), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			BeetlePvEQuestAlb quest = player.IsDoingQuest(typeof (BeetlePvEQuestAlb)) as BeetlePvEQuestAlb;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Laura.SayTo(player, player.Name + ", please find the monstrous creatures in Dartmoor, Darkness Falls and Summoner's Hall, erase them and return for your reward.");
							break;
						case 2:
							Laura.SayTo(player, "Hello " + player.Name + ", I am glad that you are back, the [beetles] will be very happy about this news!");
							break;
						case 3:
							Laura.SayTo(player, "The friendly beetle gave me two rewards for you. You can [choose], which one you need the most!");
							break;
					}
				}
				else
				{
					Laura.SayTo(player, "Hello "+ player.Name +", I am Laura. The mission is very dangerous and i'm not as strong as i used to be. " +
					                    "Creatures reign in Darkness Falls, Dartmoor and Summoner's Hall, " +
					                    "which have scared and exterminated the beetles that once lived there. Can you [help the beetles]?");
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
						case "help the beetles":
							player.Out.SendQuestSubscribeCommand(Laura, QuestMgr.GetIDForQuestType(typeof(BeetlePvEQuestAlb)), "Will you help Laura "+questTitle+"?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "beetles":
							if (quest.Step == 2)
							{
								Laura.SayTo(player, "Francis is a [friendly beetle] which will be here soon!");
							}
							break;
						case "friendly beetle":
							if (quest.Step == 2)
							{
								new ECSGameTimer(Laura, new ECSGameTimer.ECSTimerCallback(CreateEffect), 1000);
								quest.Step = 3;
								Laura.SayTo(player, "The friendly beetle gave me two rewards for you. You can [choose], which one you need the most!");
							}
							break;
						case "choose":
							if (quest.Step == 3)
							{
								Laura.SayTo(player,
									"You can choose your reward:\n\n" +
									"[Beetle Egg] - An item with 50% Power Heal and 10 Charges.\n" +
									"[Beetle Bone] - An item with 20% Life Heal and 10 Charges.\n\n" +
									"Choose wisely!");
							}
							break;
						case "Beetle Egg":
							player.Out.SendCustomDialog("Do you really want to have the Beetle Egg?", new CustomDialogResponse(QuestRewardEgg));
							break;
						case "Beetle Bone":
							player.Out.SendCustomDialog("Do you really want to have the Beetle Bone?", new CustomDialogResponse(QuestRewardBone));
							break;
						case "abort":
							player.Out.SendCustomDialog("Do you really want to abort this quest, \nall items gained during quest will be lost?", new CustomDialogResponse(CheckPlayerAbortQuest));
							break;
					}
				}
			}
		}
		
		private static int CreateBeetle(ECSGameTimer timer)
        {
	        Beetle = new GameNPC();
            Beetle.Model = 669;
            Beetle.Name = "Francis";
            Beetle.GuildName = string.Empty;
            Beetle.Realm = eRealm.Albion;
            Beetle.Race = 2007;
            Beetle.BodyType = (ushort) NpcTemplateMgr.eBodyType.Magical;
            Beetle.Size = 40;
            Beetle.Level = 55;
            Beetle.Flags ^= GameNPC.eFlags.PEACE;
            Beetle.CurrentRegionID = 10;
            Beetle.X = 36434;
            Beetle.Y = 31031;
            Beetle.Z = 8010;
            Beetle.Heading = 1428;
            
            Beetle.AddToWorld();
            return 0;
        }
		
		private static int CreateEffect(ECSGameTimer timer)
		{
			MobEffect = new GameNPC();
			MobEffect.Model = 1822;
			MobEffect.Name = "power of the beetle";
			MobEffect.GuildName = string.Empty;
			MobEffect.Realm = eRealm.Hibernia;
			MobEffect.Race = 2007;
			MobEffect.BodyType = (ushort) NpcTemplateMgr.eBodyType.Magical;
			MobEffect.Size = 25;
			MobEffect.Level = 65;
			MobEffect.Flags ^= GameNPC.eFlags.CANTTARGET;
			MobEffect.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
			MobEffect.Flags ^= GameNPC.eFlags.PEACE;
			
			MobEffect.CurrentRegionID = 10;
			MobEffect.X = 36434;
			MobEffect.Y = 31031;
			MobEffect.Z = 8010;
			MobEffect.Heading = 1428;
			
			MobEffect.AddToWorld();
			
			new ECSGameTimer(Laura, new ECSGameTimer.ECSTimerCallback(RemoveEffectMob), 1000);
			new ECSGameTimer(Laura, new ECSGameTimer.ECSTimerCallback(CreateBeetle), 1000);
			new ECSGameTimer(Laura, new ECSGameTimer.ECSTimerCallback(RemoveBeetle), 2000);
			return 0;
		}
		
		private static int RemoveEffectMob(ECSGameTimer timer)
		{
			foreach (GameNPC effect in Laura.GetNPCsInRadius(600))
			{
				if (effect.Name.ToLower() == "power of the beetle")
					effect.RemoveFromWorld();
			}

			return 0;
		}
		
		private static int RemoveBeetle(ECSGameTimer timer)
		{
			foreach (GameNPC effect in Laura.GetNPCsInRadius(600))
			{
				if (effect.Name.ToLower() == "francis")
					effect.RemoveFromWorld();
			}
			return 0;
		}
		
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (BeetlePvEQuestAlb)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			//if (player.HasFinishedQuest(typeof(Academy_47)) == 0) return false;

			//if (!CheckPartAccessible(player,typeof(CityOfCamelot)))
			//	return false;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			BeetlePvEQuestAlb quest = player.IsDoingQuest(typeof (BeetlePvEQuestAlb)) as BeetlePvEQuestAlb;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and slay those creatures!");
			}
			else
			{
				SendSystemMessage(player, "Aborting Quest " + questTitle + ". You can start over again if you want.");
				quest.AbortQuest();
			}
		}
		
		private static void QuestRewardEgg(GamePlayer player, byte response)
		{
			BeetlePvEQuestAlb quest = player.IsDoingQuest(typeof (BeetlePvEQuestAlb)) as BeetlePvEQuestAlb;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Choose your reward wisely!");
			}
			else
			{
				if (player.Inventory.IsSlotsFree(2, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
				{
					SendSystemMessage(player, "Thank you for helping Francis and his beetle family.");
					GiveItem(player, beetle_egg);
					quest.FinishQuest();
				}
				else
				{
					player.Out.SendMessage("Clear two slots of your inventory for your reward!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
		}
		
		private static void QuestRewardBone(GamePlayer player, byte response)
		{
			BeetlePvEQuestAlb quest = player.IsDoingQuest(typeof (BeetlePvEQuestAlb)) as BeetlePvEQuestAlb;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Choose your reward wisely!");
			}
			else
			{
				if (player.Inventory.IsSlotsFree(2, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
				{
					SendSystemMessage(player, "Thank you for helping Francis and his beetle family.");
					GiveItem(player, beetle_bone);
					quest.FinishQuest();
				}
				else
				{
					player.Out.SendMessage("Clear two slots of your inventory for your reward!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
		}

		private static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(BeetlePvEQuestAlb)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Laura.CanGiveQuest(typeof (BeetlePvEQuestAlb), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (BeetlePvEQuestAlb)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for your help.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Laura.GiveQuest(typeof (BeetlePvEQuestAlb), player, 1))
					return;

				Laura.SayTo(player, "Please, find the monstrous creatures in Dartmoor, Darkness Falls and Summoner's Hall, erase them and return for your reward.");

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
						return "Kill the monstrous creatures and return to Laura in Camelot.\n" +
						       "Killed: " + ALB_DRAGON_NAME + " ("+ _dragonKilled +" | " + MAX_KILLED + ")\n" +
						       "Killed: " + LEGION_NAME + " ("+ _legionKilled +" | " + MAX_KILLED + ")\n" +
						       "Killed: " + GRAND_SUMMONER_NAME + " ("+ _grandSummonerKilled +" | " + MAX_KILLED + ")\n";
					case 2:
						return "Return to Laura in Camelot and speak with her about the beetle issue.";
					case 3:
						return "Choose your reward at Laura.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(BeetlePvEQuestAlb)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Name.ToLower() == ALB_DRAGON_NAME.ToLower() && gArgs.Target is GameNPC && _dragonKilled < MAX_KILLED)
			{
				_dragonKilled = 1;
				player.Out.SendMessage("[Beetle] You killed " + ALB_DRAGON_NAME + ": (" + _dragonKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Name.ToLower() == LEGION_NAME.ToLower() && gArgs.Target is GameNPC && _legionKilled < MAX_KILLED)
			{
				_legionKilled = 1;
				player.Out.SendMessage("[Beetle] You killed " + LEGION_NAME + ": (" + _legionKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Name.ToLower() == GRAND_SUMMONER_NAME.ToLower() && gArgs.Target is GameNPC && _grandSummonerKilled < MAX_KILLED)
			{
				_grandSummonerKilled = 1;
				player.Out.SendMessage("[Beetle] You killed " + GRAND_SUMMONER_NAME + ": (" + _grandSummonerKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			if (_dragonKilled >= MAX_KILLED && _legionKilled >= MAX_KILLED && _grandSummonerKilled >= MAX_KILLED)
			{
				Step = 2;
			}
		}
		public override string QuestPropertyKey
		{
			get => "BeetlePvEQuestAlb";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_dragonKilled = GetCustomProperty(ALB_DRAGON_NAME) != null ? int.Parse(GetCustomProperty(ALB_DRAGON_NAME)) : 0;
			_legionKilled = GetCustomProperty(LEGION_NAME) != null ? int.Parse(GetCustomProperty(LEGION_NAME)) : 0;
			_grandSummonerKilled = GetCustomProperty(GRAND_SUMMONER_NAME) != null ? int.Parse(GetCustomProperty(GRAND_SUMMONER_NAME)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(ALB_DRAGON_NAME, _dragonKilled.ToString());
			SetCustomProperty(LEGION_NAME, _legionKilled.ToString());
			SetCustomProperty(GRAND_SUMMONER_NAME, _grandSummonerKilled.ToString());
		}
		
		public override void FinishQuest()
		{
			m_questPlayer.Wallet.AddMoney(WalletHelper.ToMoney(0, 0, m_questPlayer.Level * 8, 32, Util.Random(50)),
				"You receive {0} as a reward.");
			AtlasROGManager.GenerateReward(m_questPlayer, 5000);
			_grandSummonerKilled = 0;
			_legionKilled = 0;
			_dragonKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
