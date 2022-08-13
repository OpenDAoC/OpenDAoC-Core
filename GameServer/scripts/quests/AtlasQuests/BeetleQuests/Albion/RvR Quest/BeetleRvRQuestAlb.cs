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

namespace DOL.GS.AtlasQuest.Albion
{
	public class BeetleRvRQuestAlb : Quests.AtlasQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Beetle] The glorious life of the beetles";
		private const int minimumLevel = 50;
		private const int maximumLevel = 50;
		
		// Quest Goal
		private const int MAX_KILLED = 250;
		private const int MAX_CAPTURED = 5;
		private const int MAX_RELICS_CAPTURED = 1;
		
		// Quest Counter
		private int _enemiesKilled = 0;
		private int _captured = 0;
		private int _relicsCaptured = 0;

		// Quest NPC
		private static GameNPC Laura = null; // Start NPC
		private static GameNPC Beetle = null;
		private static GameNPC MobEffect = null;

		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;
		
		private static ItemTemplate beetle_egg = null;
		private static ItemTemplate beetle_bone = null;
		
		// Constructors
		public BeetleRvRQuestAlb() : base()
		{
		}

		public BeetleRvRQuestAlb(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public BeetleRvRQuestAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public BeetleRvRQuestAlb(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameEventMgr.AddHandler(Laura, GameObjectEvent.Interact, new DOLEventHandler(TalkToLaura));
			GameEventMgr.AddHandler(Laura, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToLaura));
			
			Laura.AddQuestToGive(typeof (BeetleRvRQuestAlb));

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

			Laura.RemoveQuestToGive(typeof (BeetleRvRQuestAlb));
		}

		private static void TalkToLaura(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Laura.CanGiveQuest(typeof (BeetleRvRQuestAlb), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			BeetleRvRQuestAlb quest = player.IsDoingQuest(typeof (BeetleRvRQuestAlb)) as BeetleRvRQuestAlb;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Laura.SayTo(player, player.Name + ", please find enemies in their, aswell in our lands and kill them. Come back when you also captured keeps and a relic for your reward.");
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
					Laura.SayTo(player, "Hello "+ player.Name +", I am Laura. The mission is very dangerous and I hope you can help me and Albion. " +
					                    "Enemies in Hadrian's Wall, Emain Macha, Odin's Gate, pretty much everywhere, are terrorizing our forces. " +
					                    "Albion needs brave warriors to help us and the beetles that were expelled from monstrous creatures in Darkness Falls and are trying to live in the frontiers. Can you [help Albion and the beetles]?");
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
						case "help Albion and the beetles":
							player.Out.SendQuestSubscribeCommand(Laura, QuestMgr.GetIDForQuestType(typeof(BeetleRvRQuestAlb)), "Will you help Laura "+questTitle+"?");
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
            Beetle.GuildName = "";
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
			MobEffect.GuildName = "";
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
			BeetleRvRQuestAlb quest = player.IsDoingQuest(typeof (BeetleRvRQuestAlb)) as BeetleRvRQuestAlb;

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
			BeetleRvRQuestAlb quest = player.IsDoingQuest(typeof (BeetleRvRQuestAlb)) as BeetleRvRQuestAlb;

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
			BeetleRvRQuestAlb quest = player.IsDoingQuest(typeof (BeetleRvRQuestAlb)) as BeetleRvRQuestAlb;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(BeetleRvRQuestAlb)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Laura.CanGiveQuest(typeof (BeetleRvRQuestAlb), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (BeetleRvRQuestAlb)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Laura.GiveQuest(typeof (BeetleRvRQuestAlb), player, 1))
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
						return "Invade the enemy realm for capturing a relic or slay enemies in the frontiers for Albion." +
						       "\nEnemies Killed: ("+ _enemiesKilled +" | "+ MAX_KILLED +")" +
						       "\nCaptured Keeps: ("+ _captured + " | "+ MAX_CAPTURED +")" +
						       "\nCaptured Relics: ("+ _relicsCaptured +" | "+ MAX_RELICS_CAPTURED +")";
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

			if (player?.IsDoingQuest(typeof(BeetleRvRQuestAlb)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (e == GameLivingEvent.EnemyKilled && Step == 1 && _enemiesKilled < MAX_KILLED)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs.Target.Realm == 0 || gArgs.Target.Realm == player.Realm || gArgs.Target is not GamePlayer ||
				    !(player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON)) return;
				_enemiesKilled++;
				player.Out.SendMessage("[Beetle] Enemies Killed: ("+_enemiesKilled+" | "+MAX_KILLED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			if (e == GamePlayerEvent.CapturedKeepsChanged && Step == 1 && _captured < MAX_CAPTURED)
			{
				_captured++;
				player.Out.SendMessage("[Beetle] Captured Keeps: ("+_captured+" | "+MAX_CAPTURED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			if (e == GamePlayerEvent.CapturedRelicsChanged && Step == 1 && _relicsCaptured < MAX_RELICS_CAPTURED)
			{
				_relicsCaptured++;
				player.Out.SendMessage("[Beetle] Captured Relics: ("+_relicsCaptured+" | "+MAX_RELICS_CAPTURED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}

			if (_enemiesKilled >= MAX_KILLED && _captured >= MAX_CAPTURED && _relicsCaptured >= MAX_RELICS_CAPTURED)
			{
				Step = 2;
			}

		}
		public override string QuestPropertyKey
		{
			get => "BeetleRvRQuestAlb";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_enemiesKilled = GetCustomProperty(QuestPropertyKey + "Enemy") != null ? int.Parse(GetCustomProperty(QuestPropertyKey + "Enemy")) : 0;
			_captured = GetCustomProperty(QuestPropertyKey + "Keep") != null ? int.Parse(GetCustomProperty(QuestPropertyKey + "Keep")) : 0;
			_relicsCaptured = GetCustomProperty(QuestPropertyKey + "Relic") != null ? int.Parse(GetCustomProperty(QuestPropertyKey + "Relic")) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey + "Enemy", _enemiesKilled.ToString());
			SetCustomProperty(QuestPropertyKey + "Keep", _captured.ToString());
			SetCustomProperty(QuestPropertyKey + "Relic", _relicsCaptured.ToString());
		}
		
		public override void FinishQuest()
		{
			m_questPlayer.AddMoney(Money.GetMoney(0, 0, m_questPlayer.Level * 8, 32, Util.Random(50)),
				"You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 5000);
			_enemiesKilled = 0;
			_captured = 0;
			_relicsCaptured = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
