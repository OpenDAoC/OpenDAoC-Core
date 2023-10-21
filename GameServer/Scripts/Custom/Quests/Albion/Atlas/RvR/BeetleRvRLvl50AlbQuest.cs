using System;
using System.Reflection;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Quests;
using log4net;

namespace Core.GS.AtlasQuest.Albion
{
	public class BeetleRvrLvl50AlbQuest : Quests.CoreQuest
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
		private static GameNpc Laura = null; // Start NPC
		private static GameNpc Beetle = null;
		private static GameNpc MobEffect = null;

		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;
		
		private static DbItemTemplate beetle_egg = null;
		private static DbItemTemplate beetle_bone = null;
		
		// Constructors
		public BeetleRvrLvl50AlbQuest() : base()
		{
		}

		public BeetleRvrLvl50AlbQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public BeetleRvrLvl50AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public BeetleRvrLvl50AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			
			#region defineNPCs

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Laura", ERealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 10 && npc.X == 36450 && npc.Y == 30958)
					{
						Laura = npc;
						break;
					}

			if (Laura == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Laura , creating it ...");
				Laura = new GameNpc();
				Laura.Model = 261;
				Laura.Name = "Laura";
				Laura.GuildName = "Protector of Beetles";
				Laura.Realm = ERealm.Albion;
				Laura.CurrentRegionID = 10;
				Laura.Size = 50;
				Laura.Level = 59;
				//Camelot Location
				Laura.X = 36450;
				Laura.Y = 30958;
				Laura.Z = 8010;
				Laura.Heading = 1012;
				GameNpcInventoryTemplate templateAlb = new GameNpcInventoryTemplate();
				templateAlb.AddNPCEquipment(EInventorySlot.Cloak, 559, 43);
				templateAlb.AddNPCEquipment(EInventorySlot.TorsoArmor, 1005, 23);
				templateAlb.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 43);
				templateAlb.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 43);
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Laura, GameObjectEvent.Interact, new CoreEventHandler(TalkToLaura));
			GameEventMgr.AddHandler(Laura, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToLaura));
			
			Laura.AddQuestToGive(typeof (BeetleRvrLvl50AlbQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Laura == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Laura, GameObjectEvent.Interact, new CoreEventHandler(TalkToLaura));
			GameEventMgr.RemoveHandler(Laura, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToLaura));

			Laura.RemoveQuestToGive(typeof (BeetleRvrLvl50AlbQuest));
		}

		private static void TalkToLaura(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Laura.CanGiveQuest(typeof (BeetleRvrLvl50AlbQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			BeetleRvrLvl50AlbQuest quest = player.IsDoingQuest(typeof (BeetleRvrLvl50AlbQuest)) as BeetleRvrLvl50AlbQuest;

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
							player.Out.SendQuestSubscribeCommand(Laura, QuestMgr.GetIDForQuestType(typeof(BeetleRvrLvl50AlbQuest)), "Will you help Laura "+questTitle+"?");
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
								new EcsGameTimer(Laura, new EcsGameTimer.EcsTimerCallback(CreateEffect), 1000);
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
		
		private static int CreateBeetle(EcsGameTimer timer)
        {
	        Beetle = new GameNpc();
            Beetle.Model = 669;
            Beetle.Name = "Francis";
            Beetle.GuildName = "";
            Beetle.Realm = ERealm.Albion;
            Beetle.Race = 2007;
            Beetle.BodyType = (ushort) EBodyType.Magical;
            Beetle.Size = 40;
            Beetle.Level = 55;
            Beetle.Flags ^= ENpcFlags.PEACE;
            Beetle.CurrentRegionID = 10;
            Beetle.X = 36434;
            Beetle.Y = 31031;
            Beetle.Z = 8010;
            Beetle.Heading = 1428;
            
            Beetle.AddToWorld();
            return 0;
        }
		
		private static int CreateEffect(EcsGameTimer timer)
		{
			MobEffect = new GameNpc();
			MobEffect.Model = 1822;
			MobEffect.Name = "power of the beetle";
			MobEffect.GuildName = "";
			MobEffect.Realm = ERealm.Albion;
			MobEffect.Race = 2007;
			MobEffect.BodyType = (ushort) EBodyType.Magical;
			MobEffect.Size = 25;
			MobEffect.Level = 65;
			MobEffect.Flags ^= ENpcFlags.CANTTARGET;
			MobEffect.Flags ^= ENpcFlags.DONTSHOWNAME;
			MobEffect.Flags ^= ENpcFlags.PEACE;
			
			MobEffect.CurrentRegionID = 10;
			MobEffect.X = 36434;
			MobEffect.Y = 31031;
			MobEffect.Z = 8010;
			MobEffect.Heading = 1428;
			
			MobEffect.AddToWorld();
			
			new EcsGameTimer(Laura, new EcsGameTimer.EcsTimerCallback(RemoveEffectMob), 1000);
			new EcsGameTimer(Laura, new EcsGameTimer.EcsTimerCallback(CreateBeetle), 1000);
			new EcsGameTimer(Laura, new EcsGameTimer.EcsTimerCallback(RemoveBeetle), 2000);
			return 0;
		}
		
		private static int RemoveEffectMob(EcsGameTimer timer)
		{
			foreach (GameNpc effect in Laura.GetNPCsInRadius(600))
			{
				if (effect.Name.ToLower() == "power of the beetle")
					effect.RemoveFromWorld();
			}

			return 0;
		}
		
		private static int RemoveBeetle(EcsGameTimer timer)
		{
			foreach (GameNpc effect in Laura.GetNPCsInRadius(600))
			{
				if (effect.Name.ToLower() == "francis")
					effect.RemoveFromWorld();
			}
			return 0;
		}
		
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (BeetlePveLvl50AlbQuest)) != null)
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
			BeetleRvrLvl50AlbQuest quest = player.IsDoingQuest(typeof (BeetleRvrLvl50AlbQuest)) as BeetleRvrLvl50AlbQuest;

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
			BeetleRvrLvl50AlbQuest quest = player.IsDoingQuest(typeof (BeetleRvrLvl50AlbQuest)) as BeetleRvrLvl50AlbQuest;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Choose your reward wisely!");
			}
			else
			{
				if (player.Inventory.IsSlotsFree(2, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
				{
					SendSystemMessage(player, "Thank you for helping Francis and his beetle family.");
					GiveItem(player, beetle_egg);
					quest.FinishQuest();
				}
				else
				{
					player.Out.SendMessage("Clear two slots of your inventory for your reward!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
			}
		}
		
		private static void QuestRewardBone(GamePlayer player, byte response)
		{
			BeetleRvrLvl50AlbQuest quest = player.IsDoingQuest(typeof (BeetleRvrLvl50AlbQuest)) as BeetleRvrLvl50AlbQuest;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Choose your reward wisely!");
			}
			else
			{
				if (player.Inventory.IsSlotsFree(2, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
				{
					SendSystemMessage(player, "Thank you for helping Francis and his beetle family.");
					GiveItem(player, beetle_bone);
					quest.FinishQuest();
				}
				else
				{
					player.Out.SendMessage("Clear two slots of your inventory for your reward!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
			}
		}

		private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(BeetleRvrLvl50AlbQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Laura.CanGiveQuest(typeof (BeetleRvrLvl50AlbQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (BeetleRvrLvl50AlbQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for your help.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Laura.GiveQuest(typeof (BeetleRvrLvl50AlbQuest), player, 1))
					return;

				Laura.SayTo(player, player.Name + ", please find enemies in their, aswell in our lands and kill them. Come back when you also captured keeps and a relic for your reward.");

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
						return "Invade the enemy realm for capturing a relic and slay enemies in the frontiers for Albion." +
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

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(BeetleRvrLvl50AlbQuest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (e == GameLivingEvent.EnemyKilled && Step == 1 && _enemiesKilled < MAX_KILLED)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs.Target.Realm == 0 || gArgs.Target.Realm == player.Realm || gArgs.Target is not GamePlayer ||
				    !(player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON)) return;
				_enemiesKilled++;
				player.Out.SendMessage("[Beetle] Enemies Killed: ("+_enemiesKilled+" | "+MAX_KILLED+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			if (e == GamePlayerEvent.CapturedKeepsChanged && Step == 1 && _captured < MAX_CAPTURED)
			{
				_captured++;
				player.Out.SendMessage("[Beetle] Captured Keeps: ("+_captured+" | "+MAX_CAPTURED+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			if (e == GamePlayerEvent.CapturedRelicsChanged && Step == 1 && _relicsCaptured < MAX_RELICS_CAPTURED)
			{
				_relicsCaptured++;
				player.Out.SendMessage("[Beetle] Captured Relics: ("+_relicsCaptured+" | "+MAX_RELICS_CAPTURED+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
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
			int reward = ServerProperties.Properties.BEETLE_RVR_REWARD;
			
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0, 0, m_questPlayer.Level * 8, 32, Util.Random(50)),
				"You receive {0} as a reward.");
			CoreRoGMgr.GenerateReward(m_questPlayer, 5000);
			_enemiesKilled = 0;
			_captured = 0;
			_relicsCaptured = 0;
			
			if (reward > 0)
			{
				m_questPlayer.Out.SendMessage($"You have been rewarded {reward} Realmpoints for finishing Beetle Quest.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				m_questPlayer.GainRealmPoints(reward, false);
				m_questPlayer.Out.SendUpdatePlayer();
			}
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
