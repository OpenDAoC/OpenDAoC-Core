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
using log4net;

namespace DOL.GS.AtlasQuest.Midgard
{
	public class BeetlePvEQuestMid : Quests.AtlasQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Beetle] A peace offering from a beetle";
		private const int minimumLevel = 50;
		private const int maximumLevel = 50;
		
		// Kill Goal
		private const int MAX_KILLED = 1;
		// Quest Counter
		private int _dragonKilled = 0;
		private int _legionKilled = 0;
		private int _grandSummonerKilled = 0;

		private static GameNPC Rengahr = null; // Start NPC
		private static GameNPC Beetle = null;
		private static GameNPC MobEffect = null;
		private static String MID_DRAGON_NAME = "Gjalpinulva";
		private static String LEGION_NAME = "Legion";
		private static String GRAND_SUMMONER_NAME = "Grand Summoner Govannon";
		
		private static ItemTemplate beetle_egg = null;
		private static ItemTemplate beetle_bone = null;
		
		// Constructors
		public BeetlePvEQuestMid() : base()
		{
		}

		public BeetlePvEQuestMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public BeetlePvEQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public BeetlePvEQuestMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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
			GameNPC[] npcs = WorldMgr.GetNPCsByName("Rengahr", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 101 && npc.X == 31458 && npc.Y == 27707)
					{
						Rengahr = npc;
						break;
					}

			if (Rengahr == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rengahr , creating it ...");
				Rengahr = new GameNPC();
				Rengahr.Model = 512;
				Rengahr.Name = "Rengahr";
				Rengahr.GuildName = "Protector of Beetles";
				Rengahr.Realm = eRealm.Midgard;
				Rengahr.CurrentRegionID = 101;
				Rengahr.Size = 50;
				Rengahr.Level = 59;
				//Jordheim Location
				Rengahr.X = 31458;
				Rengahr.Y = 27707;
				Rengahr.Z = 8776;
				Rengahr.Heading = 3053;
				GameNpcInventoryTemplate templateMid = new GameNpcInventoryTemplate();
				templateMid.AddNPCEquipment(eInventorySlot.Cloak, 559, 43);
				templateMid.AddNPCEquipment(eInventorySlot.TorsoArmor, 983);
				templateMid.AddNPCEquipment(eInventorySlot.LegsArmor, 984, 43);
				templateMid.AddNPCEquipment(eInventorySlot.ArmsArmor, 985, 43);
				templateMid.AddNPCEquipment(eInventorySlot.HandsArmor, 986);
				templateMid.AddNPCEquipment(eInventorySlot.FeetArmor, 987);
				Rengahr.Inventory = templateMid.CloseTemplate();
				Rengahr.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Rengahr.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			beetle_egg = GameServer.Database.FindObjectByKey<ItemTemplate>("beetle_egg");
			if (beetle_egg == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Beetle Egg, creating it ...");
				beetle_egg = new ItemTemplate();
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
			
			beetle_bone = GameServer.Database.FindObjectByKey<ItemTemplate>("beetle_bone");
			if (beetle_bone == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Beetle Bone, creating it ...");
				beetle_bone = new ItemTemplate();
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

			GameEventMgr.AddHandler(Rengahr, GameObjectEvent.Interact, new DOLEventHandler(TalkToRengahr));
			GameEventMgr.AddHandler(Rengahr, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToRengahr));
			
			Rengahr.AddQuestToGive(typeof (BeetlePvEQuestMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Rengahr == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Rengahr, GameObjectEvent.Interact, new DOLEventHandler(TalkToRengahr));
			GameEventMgr.RemoveHandler(Rengahr, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToRengahr));

			Rengahr.RemoveQuestToGive(typeof (BeetlePvEQuestMid));
		}

		private static void TalkToRengahr(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Rengahr.CanGiveQuest(typeof (BeetlePvEQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			BeetlePvEQuestMid quest = player.IsDoingQuest(typeof (BeetlePvEQuestMid)) as BeetlePvEQuestMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Rengahr.SayTo(player, player.Name + ", please find the monstrous creatures in Malmohus, Darkness Falls and Summoner's Hall, erase them and return for your reward.");
							break;
						case 2:
							Rengahr.SayTo(player, "Hello " + player.Name + ", I am glad that you are back, the [beetles] will be very happy about this news!");
							break;
						case 3:
							Rengahr.SayTo(player, "The friendly beetle gave me two rewards for you. You can [choose], which one you need the most!");
							break;
					}
				}
				else
				{
					Rengahr.SayTo(player, "Hello "+ player.Name +", I am Rengahr. The mission is very dangerous and i'm not as strong as i used to be. " +
					                    "Creatures reign in Darkness Falls, Malmohus and Summoner's Hall, " +
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
							player.Out.SendQuestSubscribeCommand(Rengahr, QuestMgr.GetIDForQuestType(typeof(BeetlePvEQuestMid)), "Will you help Rengahr "+questTitle+"?");
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
								Rengahr.SayTo(player, "Jeremy is a [friendly beetle] which will be here soon!");
							}
							break;
						case "friendly beetle":
							if (quest.Step == 2)
							{
								new ECSGameTimer(Rengahr, new ECSGameTimer.ECSTimerCallback(CreateEffect), 1000);
								quest.Step = 3;
								Rengahr.SayTo(player, "The friendly beetle gave me two rewards for you. You can [choose], which one you need the most!");
							}
							break;
						case "choose":
							if (quest.Step == 3)
							{
								Rengahr.SayTo(player,
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
            Beetle.Model = 670;
            Beetle.Name = "Jeremy";
            Beetle.GuildName = "";
            Beetle.Realm = eRealm.Midgard;
            Beetle.Race = 2007;
            Beetle.BodyType = (ushort) NpcTemplateMgr.eBodyType.Magical;
            Beetle.Size = 40;
            Beetle.Level = 55;
            Beetle.Flags ^= GameNPC.eFlags.PEACE;
            Beetle.CurrentRegionID = 101;
            Beetle.X = 31445;
            Beetle.Y = 27618;
            Beetle.Z = 8776;
            Beetle.Heading = 3288;
            
            Beetle.AddToWorld();
            return 0;
        }
		
		private static int CreateEffect(ECSGameTimer timer)
		{
			MobEffect = new GameNPC();
			MobEffect.Model = 1822;
			MobEffect.Name = "power of the beetle";
			MobEffect.GuildName = "";
			MobEffect.Realm = eRealm.Hibernia;
			MobEffect.Race = 2007;
			MobEffect.BodyType = (ushort) NpcTemplateMgr.eBodyType.Magical;
			MobEffect.Size = 25;
			MobEffect.Level = 65;
			MobEffect.Flags ^= GameNPC.eFlags.CANTTARGET;
			MobEffect.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
			MobEffect.Flags ^= GameNPC.eFlags.PEACE;
			
			MobEffect.CurrentRegionID = 101;
			MobEffect.X = 31445;
			MobEffect.Y = 27618;
			MobEffect.Z = 8776;
			MobEffect.Heading = 3288;
			
			MobEffect.AddToWorld();
			
			new ECSGameTimer(Rengahr, new ECSGameTimer.ECSTimerCallback(RemoveEffectMob), 1000);
			new ECSGameTimer(Rengahr, new ECSGameTimer.ECSTimerCallback(CreateBeetle), 1000);
			new ECSGameTimer(Rengahr, new ECSGameTimer.ECSTimerCallback(RemoveBeetle), 2000);
			return 0;
		}
		
		private static int RemoveEffectMob(ECSGameTimer timer)
		{
			foreach (GameNPC effect in Rengahr.GetNPCsInRadius(600))
			{
				if (effect.Name.ToLower() == "power of the beetle")
					effect.RemoveFromWorld();
			}

			return 0;
		}
		
		private static int RemoveBeetle(ECSGameTimer timer)
		{
			foreach (GameNPC effect in Rengahr.GetNPCsInRadius(600))
			{
				if (effect.Name.ToLower() == "jeremy")
					effect.RemoveFromWorld();
			}
			return 0;
		}
		
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (BeetlePvEQuestMid)) != null)
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
			BeetlePvEQuestMid quest = player.IsDoingQuest(typeof (BeetlePvEQuestMid)) as BeetlePvEQuestMid;

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
			BeetlePvEQuestMid quest = player.IsDoingQuest(typeof (BeetlePvEQuestMid)) as BeetlePvEQuestMid;

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
					SendSystemMessage(player, "Thank you for helping Jeremy and his beetle family.");
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
			BeetlePvEQuestMid quest = player.IsDoingQuest(typeof (BeetlePvEQuestMid)) as BeetlePvEQuestMid;

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
					SendSystemMessage(player, "Thank you for helping Jeremy and his beetle family.");
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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(BeetlePvEQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Rengahr.CanGiveQuest(typeof (BeetlePvEQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (BeetlePvEQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Rengahr.GiveQuest(typeof (BeetlePvEQuestMid), player, 1))
					return;

				Rengahr.SayTo(player, "Please, find the monstrous creatures in Malmohus, Darkness Falls and Summoner's Hall, erase them and return for your reward.");

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
						return "Kill the monstrous creatures and return to Rengahr in Aegirhamn.\n" +
						       "Killed: " + MID_DRAGON_NAME + " ("+ _dragonKilled +" | " + MAX_KILLED + ")\n" +
						       "Killed: " + LEGION_NAME + " ("+ _legionKilled +" | " + MAX_KILLED + ")\n" +
						       "Killed: " + GRAND_SUMMONER_NAME + " ("+ _grandSummonerKilled +" | " + MAX_KILLED + ")\n";
					case 2:
						return "Return to Rengahr in Aegirhamn and speak with him about the beetle issue.";
					case 3:
						return "Choose your reward at Rengahr.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(BeetlePvEQuestMid)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Name.ToLower() == MID_DRAGON_NAME.ToLower() && gArgs.Target is GameNPC && _dragonKilled < MAX_KILLED)
			{
				_dragonKilled = 1;
				player.Out.SendMessage("[Beetle] You killed " + MID_DRAGON_NAME + ": (" + _dragonKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
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
			get => "BeetlePvEQuestMid";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_dragonKilled = GetCustomProperty(MID_DRAGON_NAME) != null ? int.Parse(GetCustomProperty(MID_DRAGON_NAME)) : 0;
			_legionKilled = GetCustomProperty(LEGION_NAME) != null ? int.Parse(GetCustomProperty(LEGION_NAME)) : 0;
			_grandSummonerKilled = GetCustomProperty(GRAND_SUMMONER_NAME) != null ? int.Parse(GetCustomProperty(GRAND_SUMMONER_NAME)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(MID_DRAGON_NAME, _dragonKilled.ToString());
			SetCustomProperty(LEGION_NAME, _legionKilled.ToString());
			SetCustomProperty(GRAND_SUMMONER_NAME, _grandSummonerKilled.ToString());
		}
		
		public override void FinishQuest()
		{
			m_questPlayer.AddMoney(Money.GetMoney(0, 0, m_questPlayer.Level * 8, 32, Util.Random(50)),
				"You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 5000);
			_grandSummonerKilled = 0;
			_legionKilled = 0;
			_dragonKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
