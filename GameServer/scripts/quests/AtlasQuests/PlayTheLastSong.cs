/*
*Author         : Kelt
*Editor			: Kelt
*Source         : Custom
*Date           : 20 December 2021
*Quest Name     : [Memorial] Play the last Song
*Quest Classes  : all
*Quest Version  : v1.0
*
*Changes:
* 
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;

namespace DOL.GS.Quests.Midgard
{
	public class PlayTheLastSong : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Memorial] Play the Last Song";
		protected const int minimumLevel = 1;
		protected const int maximumLevel = 50;

		private static bool IsSinging;
		private static GameNPC VikingDextz = null; // Start NPC
		private static GameNPC Freeya = null; // Finish NPC
		
		private static DbWorldObject FreeyasGrave = null; // Object

		private static IList<DbWorldObject> GetItems()
		{
			return GameServer.Database.SelectObjects<DbWorldObject>(DB.Column("Name").IsEqualTo("Freeya's Grave"));
		}

		// Constructors
		public PlayTheLastSong() : base()
		{
		}

		public PlayTheLastSong(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public PlayTheLastSong(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public PlayTheLastSong(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
		{
		}


		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			

			#region defineNPCs
			//Freeya
			GameNPC[] npcs = WorldMgr.GetNPCsByName("Freeya", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 763734 && npc.Y == 646142)
					{
						Freeya = npc;
						break;
					}
			
			// Freeya is near Svasud Faste, North West on the Hill between trees
			if (Freeya == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find FreeyaMid , creating it ...");
				Freeya = new GameNPC();
				Freeya.Model = 165;
				Freeya.Name = "Freeya";
				Freeya.GuildName = "Thor Boyaux";
				Freeya.Realm = eRealm.Midgard;
				Freeya.CurrentRegionID = 100;
				Freeya.Flags += (ushort) GameNPC.eFlags.GHOST + (ushort) GameNPC.eFlags.PEACE + (ushort) GameNPC.eFlags.TORCH;
				Freeya.Size = 50;
				Freeya.RespawnInterval = 120000; //2min
				Freeya.Level = 65;
				Freeya.Gender = eGender.Female;
				Freeya.X = 763734;
				Freeya.Y = 646142;
				Freeya.Z = 8687;
				Freeya.Heading = 60;
								
				GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
				template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 3341);
				template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 3342);
				template.AddNPCEquipment(eInventorySlot.Cloak, 326, 43);
				template.AddNPCEquipment(eInventorySlot.TorsoArmor, 771, 50);
				template.AddNPCEquipment(eInventorySlot.LegsArmor, 772);
				template.AddNPCEquipment(eInventorySlot.HandsArmor, 774, 50);
				template.AddNPCEquipment(eInventorySlot.ArmsArmor, 773);
				template.AddNPCEquipment(eInventorySlot.FeetArmor, 775, 50);
				template.AddNPCEquipment(eInventorySlot.HeadArmor, 1227);
				Freeya.Inventory = template.CloseTemplate();
				Freeya.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Freeya.SaveIntoDatabase();
				}
			}
			
			//Viking Dextz
			npcs = WorldMgr.GetNPCsByName("Viking Dextz", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 101 && npc.X == 30621 && npc.Y == 32310)
					{
						VikingDextz = npc;
						break;
					}
			
			// Viking Dextz is near Healer Trainers in Jordheim
			if (VikingDextz == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find VikingDextzMid , creating it ...");
				VikingDextz = new GameNPC();
				VikingDextz.Model = 187;
				VikingDextz.Name = "Viking Dextz";
				VikingDextz.GuildName = "Thor Boyaux";
				VikingDextz.Realm = eRealm.Midgard;
				VikingDextz.CurrentRegionID = 101;
				VikingDextz.Flags += (ushort) GameNPC.eFlags.PEACE + (ushort) GameNPC.eFlags.TORCH;
				VikingDextz.Size = 52;
				VikingDextz.Level = 63;
				VikingDextz.X = 30621;
				VikingDextz.Y = 32310;
				VikingDextz.Z = 8305;
				VikingDextz.Heading = 3346;
				
				GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
				template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 3335);
				template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 2218);
				template.AddNPCEquipment(eInventorySlot.Cloak, 677);
				template.AddNPCEquipment(eInventorySlot.TorsoArmor, 698, 50);
				template.AddNPCEquipment(eInventorySlot.LegsArmor, 699);
				template.AddNPCEquipment(eInventorySlot.HandsArmor, 701, 50);
				template.AddNPCEquipment(eInventorySlot.ArmsArmor, 700);
				template.AddNPCEquipment(eInventorySlot.FeetArmor, 702, 50);
				template.AddNPCEquipment(eInventorySlot.HeadArmor, 1227);
				VikingDextz.Inventory = template.CloseTemplate();
				VikingDextz.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					VikingDextz.SaveIntoDatabase();
				}
			}
			#endregion

			#region defineItems

			#endregion

			#region defineObject

			var graveCheck = GetItems();
			if (graveCheck.Count == 0)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Freeyas Grave, creating it ...");
				var FreeyasGrave = new DbWorldObject();
				FreeyasGrave.Name = "Freeya\'s Grave";
				FreeyasGrave.X = 763740;
				FreeyasGrave.Y = 646102;
				FreeyasGrave.Z = 8682;
				FreeyasGrave.Heading = 118;
				FreeyasGrave.Region = 100;
				FreeyasGrave.Model = 636;
				FreeyasGrave.Realm = 2;
				FreeyasGrave.ObjectId = "freeya_grave_questitem";
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(FreeyasGrave);
					PlayTheLastSong.FreeyasGrave = FreeyasGrave;
				}

			}
			
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(VikingDextz, GameObjectEvent.Interact, new DOLEventHandler(TalkToVikingDextz));
			GameEventMgr.AddHandler(VikingDextz, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToVikingDextz));
			
			GameEventMgr.AddHandler(Freeya, GameObjectEvent.Interact, new DOLEventHandler(TalkToFreeya));
			GameEventMgr.AddHandler(Freeya, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToFreeya));
			
			/* Now we bring to NPC_Name the possibility to give this quest to players */
			VikingDextz.AddQuestToGive(typeof (PlayTheLastSong));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (VikingDextz == null)
				return;
			
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
			
			GameEventMgr.RemoveHandler(VikingDextz, GameObjectEvent.Interact, new DOLEventHandler(TalkToVikingDextz));
			GameEventMgr.RemoveHandler(VikingDextz, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToVikingDextz));
			
			GameEventMgr.RemoveHandler(Freeya, GameObjectEvent.Interact, new DOLEventHandler(TalkToFreeya));
			GameEventMgr.RemoveHandler(Freeya, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToFreeya));

			/* Now we remove to NPC_Name the possibility to give this quest to players */
			VikingDextz.RemoveQuestToGive(typeof (PlayTheLastSong));
		}

		protected static void TalkToVikingDextz(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(VikingDextz.CanGiveQuest(typeof (PlayTheLastSong), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			PlayTheLastSong quest = player.IsDoingQuest(typeof (PlayTheLastSong)) as PlayTheLastSong;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							VikingDextz.SayTo(player, "God dag " +player.Name+ ", my mission is a difficult one! Last year we lost a wonderful and helpful Skald. " +
							                          "She defended Midgard from hordes of monsters, and was a valiant soldier in our frontiers. Through her efforts, Midgard prospered. " +
							                          "[Freeya] even helped protect the Horn of Valhalla on multiple occasions... she had a good soul.");
							break;
						case 2:
							VikingDextz.SayTo(player, player.Name +", you will find Freeya's grave on the hill northwest from Svasud Faste. Please check if everything is fine there.");
							break;
						case 3:
							VikingDextz.SayTo(player, "You are probably forsaken by all good spirits! You saw Freeya? " +
							                          "Please tell her, Thor Boyaux and Exiled Vaettir pay great respect for a legend of Midgard!\nRest in Peace my friend.");
							break;
					}
				}
				else
				{
					VikingDextz.SayTo(player, "Hello "+ player.Name +", I am Dextz. "+ 
					                          "I am expecting you could help me, which is a very dangerous task. However I cannot leave Jordheim, because I need to help new budding healers.\n" +
					                       "\nCan you [support Thor Boyaux] and check Freeya\'s Grave in Uppland?");
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
						case "support Thor Boyaux":
							player.Out.SendQuestSubscribeCommand(VikingDextz, QuestMgr.GetIDForQuestType(typeof(PlayTheLastSong)), "Will you help Viking Dextz ([Memorial] Play the last Song)?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "Freeya":
							VikingDextz.SayTo(player, "Freeya - a Master Enforcer and a good friend. I miss her every day. We could use her help now. " +
							                          "Our borders need constant reinforcements, and we have heard of a growing threat in the north. ");
							if (quest.Step == 1)
							{
								VikingDextz.Emote(eEmote.Cry);
								VikingDextz.SayTo(player, "We buried her in Uppland on the hill, north west of Svasud Faste. " +
								                          "It has been a month or two since I visited her resting place. Could you please [help me] and check on Freeya\'s grave? " +
								                          "\n\nIt would bring me peace of mind to know it is fine and not broken. " +
								                          "Sadly, I currently can not leave my post as I need to train new healers for the ongoing battles.");
							}
							break;
						case "help me":
							if (quest.Step == 1)
							{
								VikingDextz.SayTo(player, "Thank you " + player.Name + ", that's very kind of you! You do me a great service.");
								quest.Step = 2;
								VikingDextz.Interact((player));
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
		
		private static void TalkToFreeya(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null || IsSinging)
				return;
			
			//We also check if the player is already doing the quest
			PlayTheLastSong quest = player.IsDoingQuest(typeof (PlayTheLastSong)) as PlayTheLastSong;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Freeya.SayTo(player, "Hello Adventurer, do you know Dextz? He is a good friend, please visit him!");
							break;
						case 2:
							player.Emote(eEmote.Shiver);
							Freeya.TurnTo(player, 500);
							if (player.Name.Contains("Dextz") || (player.Guild != null && player.Guild.Name.Contains("Thor Boyaux")))
							{
								Freeya.Emote(eEmote.Hug);
								Freeya.SayTo(player, player.CharacterClass.Name + "... It is good to see you here again, my friend. " +
								                     "I know you are protecting the realm in my stead. Fight with confidence knowing that I am watching from Odin's halls and lending you my strength! " +
								                     "Would you join me in singing my [last songs for Midgard]?");
							}
							else
							{
								Freeya.Emote(eEmote.Hug);
								Freeya.SayTo(player, "God dag " + player.CharacterClass.Name + ". I could sense you were coming. Please don't be scared, I promise I am friendly! " +
								                     "My friend Dextz use to visit me very often, it must be something special that he has chosen you to check on me! " +
								                     "I really wished to see him once again, but maybe you can help me to play my [last songs for Midgard]?");	
							}
							break;
						case 3:
							Freeya.SayTo(player, "Alright " + player.Name + ", all you have to do is say \"song\" to me and I will begin the ceremony!");
							break;
					}
				}
				else
				{
					if (player.Realm != eRealm.Midgard)
					{
						Freeya.SayTo(player, "Hello Adventurer, do you know Dextz? I had a wonderful time with him, once! " +
						                     "He was the most amazing Healer, I hope you meet him one day.\n" +
						                     "Do not forget, nobody is useless in this world who makes someone else\'s burden easier.");
					}
					else
					{
						Freeya.SayTo(player, "Hello " + player.CharacterClass.Name + ". Do not forget, nobody is useless in this world who makes someone else\'s burden easier.");
					}
					
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null || IsSinging)
				{
					switch (wArgs.Text)
					{
						
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "last songs for Midgard":
							if (quest.Step == 2)
							{
								Freeya.Emote(eEmote.Induct);
								Freeya.SayTo(player, player.Name + ", I will begin the ceremony at your word.");
								quest.Step = 3;
							}
							break;
						case "song":
							//when ceremony begins, it isnt possible to interact with Freeya (prevent Spell/Quest Bugs)
							if (quest.Step == 3 && !IsSinging)
							{
								quest.Step = 4;
								IsSinging = true;

								//cast Health Song
								new ECSGameTimer(Freeya, new ECSGameTimer.ECSTimerCallback(CastHealthRegen), 3000);

								//cast Speed Song
								new ECSGameTimer(Freeya, new ECSGameTimer.ECSTimerCallback(CastSpeed), 8000);
								
								//cast Damage Add Song
								new ECSGameTimer(Freeya, new ECSGameTimer.ECSTimerCallback(CastDamageAdd), 13000);
								
								
								new ECSGameTimer(Freeya, new ECSGameTimer.ECSTimerCallback(timer => FinishSinging(timer, player)), 18000);
								
								if (quest.Step == 4 && !IsSinging)
								{
									quest.Step = 5;
								}
								
								new ECSGameTimer(Freeya, new ECSGameTimer.ECSTimerCallback(DelayedDeath), 23000);
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
		
		private static int FinishSinging(ECSGameTimer timer, GamePlayer player)
		{
			PlayTheLastSong quest = player.IsDoingQuest(typeof(PlayTheLastSong)) as PlayTheLastSong;
			if (quest == null)
				return 0;
			
			//cast Resistance Song
			Freeya.Say("And this song is for you, " + player.Name + ". You are very brave to come here in service of Midgard. " +
			           "I'll play a resistance song for you, and all of Midgard, so the realm can continue to prosper.");
			Freeya.TurnTo(player, 500);
			Freeya.Emote(eEmote.Military);
			CastResistance();
			IsSinging = false;
			quest.FinishQuest();
			ClientService.UpdateNpcForPlayer(player, Freeya);

			return 0;
		}
		
		#region HealthRegen
		/// <summary>
		/// Cast Health Regen Song.
		/// </summary>
		/// <param name="timer">The timer that started this cast.</param>
		/// <returns></returns>
		private static int CastHealthRegen(ECSGameTimer timer)
		{
			Freeya.Say("Dextz, my friend, I will use the last of my power and play my final songs for you!\n" +
			           " I will protect you wherever you are!");
			Freeya.Emote(eEmote.Military);
			foreach (GamePlayer player in Freeya.GetPlayersInRadius(500))
			{
				Freeya.TargetObject = player;
				Freeya.CastSpell(HealthRegen, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));				
			}
			
			return 0;
		}
		
		
		private static Spell m_HealthRegen;
		/// <summary>
		/// The Health Regen Song.
		/// </summary>
		protected static Spell HealthRegen
		{
			get
			{
				if (m_HealthRegen == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Icon = 3618;
					spell.ClientEffect = 3618;
					spell.Damage = 0;
					spell.Duration = 5;
					spell.Name = "Freeya's Heavenly Song of Rest";
					spell.Range = 500;
					spell.Radius = 500;
					spell.SpellID = 3618;
					spell.Target = "Realm";
					spell.Type = "HealthRegenBuff";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = 0;
					spell.Message1 = "Dextz looks calmer.";
					m_HealthRegen = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_HealthRegen);
				}
				return m_HealthRegen;
			}
		}

		#endregion HealthRegen
		#region SpeedSong
		/// <summary>
		/// Cast Speed Song.
		/// </summary>
		/// <param name="timer">The timer that started this cast.</param>
		/// <returns></returns>
		private static int CastSpeed(ECSGameTimer timer)
		{
			Freeya.Say("Thor Boyaux, you were my family and forever shall be! \n" +
			           "I will protect you wherever you are!");
			Freeya.Emote(eEmote.Military);
			foreach (GamePlayer player in Freeya.GetPlayersInRadius(500))
			{
				Freeya.TargetObject = player;
				Freeya.CastSpell(SpeedSong, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));			
			}
			
			return 0;
		}
		
		
		private static Spell m_SpeedSong;
		/// <summary>
		/// The Speed Song.
		/// </summary>
		protected static Spell SpeedSong
		{
			get
			{
				if (m_SpeedSong == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Icon = 3612;
					spell.ClientEffect = 3612;
					spell.Damage = 0;
					spell.Duration = 5;
					spell.Name = "Freeya's Heavenly Song of Travel";
					spell.Range = 500;
					spell.Radius = 500;
					spell.SpellID = 3612;
					spell.Target = "Realm";
					spell.Type = "SpeedEnhancement";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = 0;
					spell.Message1 = "Thor Boyaux will be protected with Enhancement.";
					m_SpeedSong = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SpeedSong);
				}
				return m_SpeedSong;
			}
		}

		#endregion SpeedSong
		#region DamageAdd
		/// <summary>
		/// Cast Damage Add Song.
		/// </summary>
		/// <param name="timer">The timer that started this cast.</param>
		/// <returns></returns>
		private static int CastDamageAdd(ECSGameTimer timer)
		{
			Freeya.Say("Exiled Vaettir, you accepted and supported me. For that, I am very grateful to you! \n" +
			           "I will protect you wherever you are!");
			Freeya.Emote(eEmote.Military);
			foreach (GamePlayer player in Freeya.GetPlayersInRadius(500))
			{
				Freeya.TargetObject = player;
				Freeya.CastSpell(DamageAdd, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			
			return 0;
		}
		
		
		private static Spell m_DamageAdd;
		/// <summary>
		/// The Damage Add Song.
		/// </summary>
		protected static Spell DamageAdd
		{
			get
			{
				if (m_DamageAdd == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Icon = 3607;
					spell.ClientEffect = 3607;
					spell.Damage = 0;
					spell.Duration = 5;
					spell.Name = "Freeya's Chant of Blood";
					spell.Range = 500;
					spell.Radius = 500;
					spell.SpellID = 3607;
					spell.Target = "Realm";
					spell.Type = "DamageAdd";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = 0;
					spell.Message1 = "Exiled Vaettir will swing their weapons with zeal.";
					m_DamageAdd = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DamageAdd);
				}
				return m_DamageAdd;
			}
		}

		#endregion DamageAdd
		#region ResistanceSong
		/// <summary>
		/// Cast Resistance Song.
		/// </summary>
		/// <param name="timer">The timer that started this cast.</param>
		/// <returns></returns>
		private static int CastResistance()
		{
			foreach (GamePlayer player in Freeya.GetPlayersInRadius(500))
			{
				Freeya.TargetObject = player;
				Freeya.CastSpell(Resistance, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			
			return 0;
		}
		
		
		private static Spell m_Resistance;
		/// <summary>
		/// The Resistance Song.
		/// </summary>
		protected static Spell Resistance
		{
			get
			{
				if (m_Resistance == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Icon = 3656;
					spell.ClientEffect = 3656;
					spell.Damage = 0;
					spell.Duration = 5;
					spell.Name = "Freeya's Energy Diminishing Song";
					spell.Range = 500;
					spell.Radius = 500;
					spell.SpellID = 3656;
					spell.Target = "Realm";
					spell.Type = "EnergyResistBuff";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = 0;
					spell.Message1 = "You are protected from energy!";
					m_Resistance = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Resistance);
				}
				return m_Resistance;
			}
		}

		#endregion DamageAdd
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (PlayTheLastSong)) != null)
				return true;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			PlayTheLastSong quest = player.IsDoingQuest(typeof (PlayTheLastSong)) as PlayTheLastSong;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(PlayTheLastSong)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(VikingDextz.CanGiveQuest(typeof (PlayTheLastSong), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (PlayTheLastSong)) != null)
				return;

			if (response == 0x00)
			{
				
			}
			else
			{
				//Check if we can add the quest!
				if (!VikingDextz.GiveQuest(typeof (PlayTheLastSong), player, 1))
					return;
			}
			VikingDextz.Interact(player);
		}

		//Set quest name
		public override string Name
		{
			get { return "[Memorial] Play the Last Song"; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "Speak with Viking Dextz to get more information.";
					case 2:
						return "Find Freeya's Grave in Uppland North West from Svasud Faste on the hill.\n" +
						       "(Loc: X:42850 Y:39926 Z:8691)";
					case 3:
						return "Help Freeya to play the last Songs. (/whisper \"song\")";
					case 4:
						return "Listen to Freeya\'s ceremony!";
					case 5:
						return "Rest in Peace Freeya! (quest completed)";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player==null || player.IsDoingQuest(typeof (PlayTheLastSong)) == null)
				return;

			if (e == GameObjectEvent.Interact && (Step == 4 && !IsSinging))
			{
				InteractEventArgs gArgs = (InteractEventArgs) args;
				if (gArgs.Source.Name == Freeya.Name)
				{
					new ECSGameTimer(Freeya, new ECSGameTimer.ECSTimerCallback(timer => FinishSinging(timer, player)), 3000);
								
					if (Step == 4 && !IsSinging)
					{
						Step = 5;
					}
								
					new ECSGameTimer(Freeya, new ECSGameTimer.ECSTimerCallback(DelayedDeath), 8000);
					FinishQuest();
				}
			}
			if (e == GameObjectEvent.AddToWorld)
			{
				GameEventMgr.AddHandler(Freeya, GameObjectEvent.Interact, new DOLEventHandler(TalkToFreeya));
				GameEventMgr.AddHandler(Freeya, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToFreeya));
			}
			if (e == GameLivingEvent.Dying)
			{
				GameEventMgr.RemoveHandler(Freeya, GameObjectEvent.Interact, new DOLEventHandler(TalkToFreeya));
				GameEventMgr.RemoveHandler(Freeya, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToFreeya));
			}
		}
		
		private static int DelayedDeath(ECSGameTimer timer)
		{
			Freeya.Say(
				"And with that... the horn has sounded. Valhalla is calling me and it's time I must go. Walk in Strength.\nHa det, my friend.");
			GameEventMgr.RemoveHandler(Freeya, GameObjectEvent.Interact, new DOLEventHandler(TalkToFreeya));
			GameEventMgr.RemoveHandler(Freeya, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToFreeya));
			Freeya.Die(Freeya);
			return 0;
		}
		
		public class PlayTheLastSongTitle : EventPlayerTitle 
		{
	        /// <summary>
	        /// The title description, shown in "Titles" window.
	        /// </summary>
	        /// <param name="player">The title owner.</param>
	        /// <returns>The title description.</returns>
	        public override string GetDescription(GamePlayer player)
	        {
	            return "Protected by Songs";
	        }

	        /// <summary>
	        /// The title value, shown over player's head.
	        /// </summary>
	        /// <param name="source">The player looking.</param>
	        /// <param name="player">The title owner.</param>
	        /// <returns>The title value.</returns>
	        public override string GetValue(GamePlayer source, GamePlayer player)
	        {
	            return "Protected by Songs";
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
		        return player.HasFinishedQuest(typeof(PlayTheLastSong)) == 1;
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
			m_questPlayer.Wallet.AddMoney(WalletHelper.ToMoney(0,0,1,32,Util.Random(50)), "You receive {0} as a reward.");

			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
