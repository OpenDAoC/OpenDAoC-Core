using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using System.Collections.Generic;
using DOL.GS.Styles;

namespace DOL.GS
{
	public class Nosdoden : GameEpicBoss
	{
		protected String[] m_deathAnnounce;
		public Nosdoden() : base() 
		{
			m_deathAnnounce = new String[] { "The earth lurches beneath your feet as {0} staggers and topples to the ground.",
				"A glowing light begins to form on the mound that served as {0}'s lair." };
		}
		#region Custom methods
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		protected void ReportNews(GameObject killer)
		{
			int numPlayers = AwardDragonKillPoint();
			String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
			NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);

			if (Properties.GUILD_MERIT_ON_DRAGON_KILL > 0)
			{
				foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					if (player.IsEligibleToGiveMeritPoints)
					{
						GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_DRAGON_KILL);
					}
				}
			}
		}

		/// <summary>
		/// Award dragon kill point for each XP gainer.
		/// </summary>
		/// <returns>The number of people involved in the kill.</returns>
		protected int AwardDragonKillPoint()
		{
			int count = 0;
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.KillsDragon++;
				count++;
			}
			return count;
		}
		public override void Die(GameObject killer)
		{
			// debug
			if (killer == null)
				log.Error("Nosdoden Killed: killer is null!");
			else
				log.Debug("Nosdoden Killed: killer is " + killer.Name + ", attackers:");

			bool canReportNews = true;
			// due to issues with attackers the following code will send a notify to all in area in order to force quest credit
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

				if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
				{
					if (player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
						canReportNews = false;
				}
			}
			base.Die(killer);
			foreach (String message in m_deathAnnounce)
			{
				BroadcastMessage(String.Format(message, Name));
			}
			if (canReportNews)
			{
				ReportNews(killer);
			}
		}
		#endregion
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Nosdoden Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 80;// dmg reduction for melee dmg
				case eDamageType.Crush: return 80;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 80;// dmg reduction for melee dmg
				default: return 80;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 800;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.65;
		}
		public override int MaxHealth
		{
			get { return 40000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164545);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			Level = Convert.ToByte(npcTemplate.Level);

			Faction = FactionMgr.GetFactionByID(150);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
			NosdodenBrain sbrain = new NosdodenBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void EnemyKilled(GameLiving enemy)
        {
			GamePlayer player = enemy as GamePlayer;
			if (enemy is GamePlayer)
			{
				if (player != null)
				{
					NosdodenGhostAdd add = new NosdodenGhostAdd();
					add.Name = "Spirit of " + player.Name;
					add.X = player.X;
					add.Y = player.Y;
					add.Z = player.Z;
					add.Size = (byte)player.Size;
					add.Flags = eFlags.GHOST;
                    #region Set mob model
                    if (player.Race == (short)eRace.Norseman && player.Gender == eGender.Male)//norse male
						add.Model = (ushort)Util.Random(153, 160);
					if (player.Race == (short)eRace.Norseman && player.Gender == eGender.Female)//norse female
						add.Model = (ushort)Util.Random(161, 168);
					if (player.Race == (short)eRace.Troll && player.Gender == eGender.Male)//troll male
						add.Model = (ushort)Util.Random(137, 144);
					if (player.Race == (short)eRace.Troll && player.Gender == eGender.Female)//troll female
						add.Model = (ushort)Util.Random(145, 152);
					if (player.Race == (short)eRace.Kobold && player.Gender == eGender.Male)//kobolt male
						add.Model = (ushort)Util.Random(169, 176);
					if (player.Race == (short)eRace.Kobold && player.Gender == eGender.Female)//kobolt female
						add.Model = (ushort)Util.Random(177, 184);
					if (player.Race == (short)eRace.Valkyn && player.Gender == eGender.Male)//valkyn male
						add.Model = (ushort)Util.Random(773, 780);
					if (player.Race == (short)eRace.Valkyn && player.Gender == eGender.Female)//valkyn female
						add.Model = (ushort)Util.Random(781, 788);
					if (player.Race == (short)eRace.Dwarf && player.Gender == eGender.Male)//dwarf male
						add.Model = (ushort)Util.Random(185, 192);
					if (player.Race == (short)eRace.Dwarf && player.Gender == eGender.Female)//dwarf female
						add.Model = (ushort)Util.Random(193, 200);
                    #endregion
                    add.Heading = Heading;
					add.CurrentRegionID = CurrentRegionID;
					add.RespawnInterval = -1;
                    #region equiptemplate for mob and styles
                    GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();					
					if (player.Inventory.GetItem(eInventorySlot.TorsoArmor) != null)
					{
						InventoryItem torso = player.Inventory.GetItem(eInventorySlot.TorsoArmor);
						if(torso != null)
							template.AddNPCEquipment(eInventorySlot.TorsoArmor, torso.Model, torso.Color,0,torso.Extension);//modelID,color,effect,extension
					}
					if (player.Inventory.GetItem(eInventorySlot.ArmsArmor) != null)
					{
						InventoryItem arms = player.Inventory.GetItem(eInventorySlot.ArmsArmor);
						if(arms != null)
							template.AddNPCEquipment(eInventorySlot.ArmsArmor, arms.Model, arms.Color);
					}
					if (player.Inventory.GetItem(eInventorySlot.LegsArmor) != null)
					{
						InventoryItem legs = player.Inventory.GetItem(eInventorySlot.LegsArmor);
						if(legs != null)
							template.AddNPCEquipment(eInventorySlot.LegsArmor, legs.Model, legs.Color);
					}
					if (player.Inventory.GetItem(eInventorySlot.HeadArmor) != null)
					{
						InventoryItem head = player.Inventory.GetItem(eInventorySlot.HeadArmor);
						if(head != null)
							template.AddNPCEquipment(eInventorySlot.HeadArmor, head.Model, head.Color);
					}
					if (player.Inventory.GetItem(eInventorySlot.HandsArmor) != null)
					{
						InventoryItem hands = player.Inventory.GetItem(eInventorySlot.HandsArmor);
						if(hands != null)
							template.AddNPCEquipment(eInventorySlot.HandsArmor, hands.Model, hands.Color,0,hands.Extension);
					}
					if (player.Inventory.GetItem(eInventorySlot.FeetArmor) != null)
					{
						InventoryItem feet = player.Inventory.GetItem(eInventorySlot.FeetArmor);
						if(feet != null)
							template.AddNPCEquipment(eInventorySlot.FeetArmor, feet.Model, feet.Color,0,feet.Extension);
					}
					if (player.Inventory.GetItem(eInventorySlot.Cloak) != null)
					{
						InventoryItem cloak = player.Inventory.GetItem(eInventorySlot.Cloak);
						if(cloak != null)
							template.AddNPCEquipment(eInventorySlot.Cloak, cloak.Model, cloak.Color,0,0,cloak.Emblem);
					}
					if (player.Inventory.GetItem(eInventorySlot.RightHandWeapon) != null)
					{
						InventoryItem righthand = player.Inventory.GetItem(eInventorySlot.RightHandWeapon);
						InventoryItem lefthand = player.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
						if (righthand != null && lefthand != null)
						{
							template.AddNPCEquipment(eInventorySlot.RightHandWeapon, righthand.Model, righthand.Color, righthand.Effect);
							#region Styles for Warrior and Thane
							if (player.CharacterClass.ID == (int)eCharacterClass.Warrior || player.CharacterClass.ID == (int)eCharacterClass.Thane)
							{
								if (righthand.Object_Type == (int)eObjectType.Axe)
								{
									if (!add.Styles.Contains(NosdodenGhostAddBrain.tauntAxeWarrior))
										add.Styles.Add(NosdodenGhostAddBrain.tauntAxeWarrior);
								}
								if (righthand.Object_Type == (int)eObjectType.Hammer)
								{
									if (!add.Styles.Contains(NosdodenGhostAddBrain.tauntHammerWarrior))
										add.Styles.Add(NosdodenGhostAddBrain.tauntHammerWarrior);
								}
								if (righthand.Object_Type == (int)eObjectType.Sword)
								{
									if (!add.Styles.Contains(NosdodenGhostAddBrain.tauntSwordWarrior))
										add.Styles.Add(NosdodenGhostAddBrain.tauntSwordWarrior);
								}
							}
							#endregion
							#region Styles for Savage
							if (player.CharacterClass.ID == (int)eCharacterClass.Savage)
							{
								if (righthand.Object_Type == (int)eObjectType.HandToHand || lefthand.Object_Type == (int)eObjectType.HandToHand)
								{
									if (!add.Styles.Contains(NosdodenGhostAddBrain.tauntSavage))
										add.Styles.Add(NosdodenGhostAddBrain.tauntSavage);
									if (!add.Styles.Contains(NosdodenGhostAddBrain.BackSavage))
										add.Styles.Add(NosdodenGhostAddBrain.BackSavage);
								}
							}
							#endregion
						}
					}
                    if (player.Inventory.GetItem(eInventorySlot.LeftHandWeapon) != null)
					{
						InventoryItem lefthand = player.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
						if(lefthand != null)
							template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, lefthand.Model, lefthand.Color, lefthand.Effect);
					}
					if (player.Inventory.GetItem(eInventorySlot.TwoHandWeapon) != null)
                    {
						InventoryItem twohand = player.Inventory.GetItem(eInventorySlot.TwoHandWeapon);
						InventoryItem righthand = player.Inventory.GetItem(eInventorySlot.RightHandWeapon);
						if (twohand != null)
						{
							template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, twohand.Model, twohand.Color, twohand.Effect);
							#region Styles for Savage 2h
							if (player.CharacterClass.ID == (int)eCharacterClass.Savage && righthand == null)
							{
								if (!add.Styles.Contains(NosdodenGhostAddBrain.Taunt2h))
									add.Styles.Add(NosdodenGhostAddBrain.Taunt2h);
							}
							#endregion
							#region Styles for Hunter Spear 2h
							if (player.CharacterClass.ID == (int)eCharacterClass.Hunter && twohand.Object_Type == (int)eObjectType.Spear)
							{
								if (!add.Styles.Contains(NosdodenGhostAddBrain.TauntSpearHunt))
									add.Styles.Add(NosdodenGhostAddBrain.TauntSpearHunt);
								if (!add.Styles.Contains(NosdodenGhostAddBrain.BackSpearHunt))
									add.Styles.Add(NosdodenGhostAddBrain.BackSpearHunt);
							}
							#endregion
						}
                    }
                    if (player.Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
					{
						InventoryItem distance = player.Inventory.GetItem(eInventorySlot.DistanceWeapon);
						if(distance != null)
							template.AddNPCEquipment(eInventorySlot.DistanceWeapon, distance.Model, distance.Color, distance.Effect);
					}						
					add.Inventory = template.CloseTemplate();
                    #endregion
                    #region Set mob visible slot
                    InventoryItem mob_twohand = template.GetItem(eInventorySlot.TwoHandWeapon);
					InventoryItem mob_righthand = template.GetItem(eInventorySlot.RightHandWeapon);
					InventoryItem mob_lefthand = template.GetItem(eInventorySlot.LeftHandWeapon);
					InventoryItem mob_distance = template.GetItem(eInventorySlot.LeftHandWeapon);
					if (mob_lefthand != null && mob_righthand != null)
					{
						if ((mob_righthand.Object_Type == (int)eObjectType.Axe && mob_righthand.Item_Type == Slot.RIGHTHAND) /*axe*/
						|| (mob_righthand.Object_Type == (int)eObjectType.Sword && mob_righthand.Item_Type == Slot.RIGHTHAND) /*sword*/
						|| ((mob_righthand.Object_Type == (int)eObjectType.HandToHand || mob_lefthand.Object_Type == (int)eObjectType.HandToHand) && (mob_righthand.Item_Type == Slot.RIGHTHAND || mob_lefthand.Item_Type == Slot.LEFTHAND)) /*Hand-to-Hand*/
						|| (mob_righthand.Object_Type == (int)eObjectType.Hammer && mob_righthand.Item_Type == Slot.RIGHTHAND) /*hammer*/
						|| (mob_lefthand.Object_Type == (int)eObjectType.LeftAxe) /*left axe*/
						|| (mob_lefthand.Object_Type == (int)eObjectType.Shield)) /*shield*/
						{
							add.SwitchWeapon(eActiveWeaponSlot.Standard);
							add.VisibleActiveWeaponSlots = 16;
						}
					}
					if (mob_twohand != null)
					{
						if (((mob_twohand.Object_Type == (int)eObjectType.Hammer && mob_twohand.Item_Type == Slot.TWOHAND) /*axe2h*/
						|| (mob_twohand.Object_Type == (int)eObjectType.Sword && mob_twohand.Item_Type == Slot.TWOHAND) /*sword2h*/
						|| (mob_twohand.Object_Type == (int)eObjectType.Spear && mob_twohand.Item_Type == Slot.TWOHAND) /*spear*/
						|| (mob_twohand.Object_Type == (int)eObjectType.Staff && mob_twohand.Item_Type == Slot.TWOHAND))) /*Staff*/
						{
							add.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
							add.VisibleActiveWeaponSlots = 34;
						}
					}
					if(mob_distance != null && mob_distance.Object_Type == (int)eObjectType.CompositeBow && mob_distance.Item_Type == Slot.RANGED) /*distance*/
                    {
						add.SwitchWeapon(eActiveWeaponSlot.Distance);
						add.VisibleActiveWeaponSlots = 51;
					}
					#endregion
					add.PackageID = "NosdodenGhost" + player.CharacterClass.Name;
					add.AddToWorld();
				}
			}
            base.EnemyKilled(enemy);
        }
    }
}
namespace DOL.AI.Brain
{
	public class NosdodenBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public NosdodenBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
		}
		public static bool IsPulled = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		#region Worm Dot
		public static bool CanCast2 = false;
		public static bool StartCastDOT = false;
		public static GamePlayer randomtarget2 = null;
		public static GamePlayer RandomTarget2
		{
			get { return randomtarget2; }
			set { randomtarget2 = value; }
		}
		List<GamePlayer> Enemys_To_DOT = new List<GamePlayer>();
		public int PickRandomTarget2(ECSGameTimer timer)
		{
			if (HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1)
						{
							if (!Enemys_To_DOT.Contains(player))
							{
								Enemys_To_DOT.Add(player);
							}
						}
					}
				}
				if (Enemys_To_DOT.Count > 0)
				{
					if (CanCast2 == false)
					{
						GamePlayer Target = (GamePlayer)Enemys_To_DOT[Util.Random(0, Enemys_To_DOT.Count - 1)];//pick random target from list
						RandomTarget2 = Target;//set random target to static RandomTarget
						int _castDotTime = 2000;
						ECSGameTimer _CastDot = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastDOT), _castDotTime);
						_CastDot.Start(_castDotTime);
						CanCast2 = true;
					}
				}
			}
			return 0;
		}
		public int CastDOT(ECSGameTimer timer)
		{
			if (HasAggro && RandomTarget2 != null)
			{
				GamePlayer oldTarget = (GamePlayer)Body.TargetObject;//old target
				if (RandomTarget2 != null && RandomTarget2.IsAlive)
				{
					Body.TargetObject = RandomTarget2;
					Body.TurnTo(RandomTarget2);
					Body.CastSpell(NosdodenDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
				int _resetDotTime = 5000;
				ECSGameTimer _ResetDot = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDOT), _resetDotTime);
				_ResetDot.Start(_resetDotTime);
			}
			return 0;
		}
		public int ResetDOT(ECSGameTimer timer)
		{
			RandomTarget2 = null;
			CanCast2 = false;
			StartCastDOT = false;
			return 0;
		}
		#endregion
		#region Worm DD
		public static bool CanCast = false;
		public static bool StartCastDD = false;
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
		public int PickRandomTarget(ECSGameTimer timer)
		{
			if (HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1)
						{
							if (!Enemys_To_DD.Contains(player))
							{
								Enemys_To_DD.Add(player);
							}
						}
					}
				}
				if (Enemys_To_DD.Count > 0)
				{
					if (CanCast == false)
					{
						GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
						RandomTarget = Target;//set random target to static RandomTarget
						int _castDDTime = 5000;
						ECSGameTimer _CastDD = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastDD), _castDDTime);
						_CastDD.Start(_castDDTime);
						BroadcastMessage(String.Format(Body.Name + " starts casting void magic at " + RandomTarget.Name + "."));
						CanCast = true;
					}
				}
			}
			return 0;
		}
		public int CastDD(ECSGameTimer timer)
		{
			if (HasAggro && RandomTarget != null)
			{
				GamePlayer oldTarget = (GamePlayer)Body.TargetObject;//old target
				if (RandomTarget != null && RandomTarget.IsAlive)
				{
					Body.TargetObject = RandomTarget;
					Body.TurnTo(RandomTarget);
					Body.CastSpell(NosdodenDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
				int _resetDDTime = 5000;
				ECSGameTimer _ResetDD = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDD), _resetDDTime);
				_ResetDD.Start(_resetDDTime);
			}
			return 0;
		}
		public int ResetDD(ECSGameTimer timer)
		{
			RandomTarget = null;
			CanCast = false;
			StartCastDD = false;
			return 0;
		}
		#endregion
		public override void Think()
		{
			if(!HasAggressionTable())
            {
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				StartCastDOT = false;
				StartCastDD = false;
				CanCast2 = false;
				CanCast = false;
				RandomTarget = null;
				RandomTarget2 = null;
            }
			if (Body.IsAlive && HasAggro)
            {
				if (StartCastDOT == false)
				{
					int _pickRandomTarget2Time = Util.Random(20000, 30000);					
					ECSGameTimer _PickRandomTarget2 = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickRandomTarget2), _pickRandomTarget2Time);
					_PickRandomTarget2.Start(_pickRandomTarget2Time);
					StartCastDOT = true;
				}
				if (StartCastDD == false)
				{
					int _pickRandomTargetTime = Util.Random(35000, 45000);
					ECSGameTimer _PickRandomTarget = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickRandomTarget), _pickRandomTargetTime);
					_PickRandomTarget.Start(_pickRandomTargetTime);
					StartCastDD = true;
				}
			}
			base.Think();
		}
        #region Spells
        private Spell m_NosdodenDot;
		private Spell NosdodenDot
		{
			get
			{
				if (m_NosdodenDot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4099;
					spell.Icon = 4099;
					spell.TooltipId = 4099;
					spell.Name = "Nosdoden's Venom";
					spell.Description = "Inflicts 150 damage to the target every 4 sec for 60 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.Damage = 150;
					spell.Duration = 60;
					spell.Frequency = 40;
					spell.Range = 1800;
					spell.Radius = 500;
					spell.SpellID = 11856;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.DamageType = (int)eDamageType.Body;
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_NosdodenDot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NosdodenDot);
				}
				return m_NosdodenDot;
			}
		}
		private Spell m_NosdodenDD;
		private Spell NosdodenDD
		{
			get
			{
				if (m_NosdodenDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4568;
					spell.Icon = 4568;
					spell.TooltipId = 4568;
					spell.Name = "Call of Void";
					spell.Damage = 1100;
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11857;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Cold;
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_NosdodenDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NosdodenDD);
				}
				return m_NosdodenDD;
			}
		}
		#endregion
	}
}
////////////////////////////////////////////////////////////////////////Nosdoden spawned ghosts///////////////////////////////////////////////////
namespace DOL.AI.Brain
{
	public class NosdodenGhostAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public NosdodenGhostAddBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
		}
		#region Mob Class Berserker
		private protected bool CanWalkBerserker = false;

		public static int TauntBerserkerID = 202;
		public static int TauntBerserkerClassID = 31; 
		public static Style tauntBerserker = SkillBase.GetStyleByID(TauntBerserkerID, TauntBerserkerClassID);

		public static int BackBerserkerID = 195;
		public static int BackBerserkerClassID = 31;
		public static Style BackBerserker = SkillBase.GetStyleByID(BackBerserkerID, BackBerserkerClassID);

		public static int AfterEvadeBerserkerID = 198;
		public static int AfterEvadeBerserkerClassID = 31;
		public static Style AfterEvadeBerserker = SkillBase.GetStyleByID(AfterEvadeBerserkerID, AfterEvadeBerserkerClassID);

		public static int EvadeFollowUpBerserkerID = 203;
		public static int EvadeFollowUpBerserkerClassID = 31;
		public static Style EvadeFollowUpBerserker = SkillBase.GetStyleByID(EvadeFollowUpBerserkerID, EvadeFollowUpBerserkerClassID);
		public void IsBerserker()
        {
			if (Body.PackageID == "NosdodenGhostBerserker")
			{
				Body.SwitchWeapon(eActiveWeaponSlot.Standard);
				Body.VisibleActiveWeaponSlots = 16;
				Body.EvadeChance = 60;
				if (Body.IsAlive)
				{
					if (!Body.Styles.Contains(tauntBerserker))
						Body.Styles.Add(tauntBerserker);
					if (!Body.Styles.Contains(AfterEvadeBerserker))
						Body.Styles.Add(AfterEvadeBerserker);
					if (!Body.Styles.Contains(EvadeFollowUpBerserker))
						Body.Styles.Add(EvadeFollowUpBerserker);
					if (!Body.Styles.Contains(BackBerserker))
						Body.Styles.Add(BackBerserker);
				}
				if (!HasAggressionTable())
				{
					CanWalkBerserker = false;
				}
				if (Body.InCombat && HasAggro)
                {
					if (Body.TargetObject != null)
                    {
						GameLiving target = Body.TargetObject as GameLiving;
						float angle = Body.TargetObject.GetAngle(Body);
						if (angle >= 160 && angle <= 200)
                        {
							Body.Quickness = 100;
							Body.Strength = 220;
							Body.styleComponent.NextCombatStyle = BackBerserker;
						}
						else
                        {
							Body.Quickness = 100;
							Body.Strength = 180;
						}
						if (target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun))
						{
							if (CanWalkBerserker == false)
							{
								int _walkBackTime = 500;
								ECSGameTimer _WalkBack = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WalkBack), _walkBackTime);//if target got stun then start timer to run behind it
								_WalkBack.Start(_walkBackTime);
								CanWalkBerserker = true;
							}
						}
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
						{
							CanWalkBerserker = false;//reset flag so can slam again
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Warrior
		private protected bool CanWalkWarrior = false;

		public static int TauntSwordWarriorID = 157;
		public static int TauntSwordWarriorClassID = 22;
		public static Style tauntSwordWarrior = SkillBase.GetStyleByID(TauntSwordWarriorID, TauntSwordWarriorClassID);

		public static int TauntHammerWarriorID = 167;
		public static int TauntHammerWarriorClassID = 22;
		public static Style tauntHammerWarrior = SkillBase.GetStyleByID(TauntHammerWarriorID, TauntHammerWarriorClassID);

		public static int TauntAxeWarriorID = 178;
		public static int TauntAxeWarriorClassID = 22;
		public static Style tauntAxeWarrior = SkillBase.GetStyleByID(TauntAxeWarriorID, TauntAxeWarriorClassID);

		public static int SlamWarriorID = 228;
		public static int SlamWarriorClassID = 22;
		public static Style slamWarrior = SkillBase.GetStyleByID(SlamWarriorID, SlamWarriorClassID);
		public void IsWarrior()
		{
			if (Body.PackageID == "NosdodenGhostWarrior")
			{
				if(Body.IsAlive && !HasAggro)
                {
					Body.ParryChance = 15;
					Body.BlockChance = 60;
					Body.SwitchWeapon(eActiveWeaponSlot.Standard);
					Body.VisibleActiveWeaponSlots = 16;
				}
				if (!HasAggressionTable())
				{
					CanWalkWarrior = false;
				}
				if(Body.IsAlive)
                {
					if (!Body.Styles.Contains(slamWarrior))
						Body.Styles.Add(slamWarrior);
					if (!Body.Styles.Contains(Taunt2h))
						Body.Styles.Add(Taunt2h);
					if (!Body.Styles.Contains(Back2h))
						Body.Styles.Add(Back2h);
                }
				if (Body.InCombat && HasAggro)
				{
					if (Body.TargetObject != null)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						float angle = Body.TargetObject.GetAngle(Body);
						if (angle >= 160 && angle <= 200)
						{
							Body.Strength = 250;
							Body.ParryChance = 60;
							Body.BlockChance = 0;
							Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
							Body.VisibleActiveWeaponSlots = 34;
							Body.styleComponent.NextCombatStyle = Back2h;
							Body.styleComponent.NextCombatBackupStyle = Taunt2h;
						}
						else
						{
							Body.Strength = 180;
							Body.Quickness = 100;
							Body.ParryChance = 15;
							Body.BlockChance = 60;
							Body.SwitchWeapon(eActiveWeaponSlot.Standard);
							Body.VisibleActiveWeaponSlots = 16;
							foreach (Style styles in Body.Styles)
							{
								if (styles != null)
								{
									if (styles.ID == 157 && styles.ClassID == 22)
										Body.styleComponent.NextCombatStyle = tauntSwordWarrior;

									if (styles.ID == 178 && styles.ClassID == 22)
										Body.styleComponent.NextCombatStyle = tauntAxeWarrior;

									if (styles.ID == 167 && styles.ClassID == 22)
										Body.styleComponent.NextCombatStyle = tauntHammerWarrior;
								}
							}
						}
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
						{
							Body.Strength = 180;
							Body.Quickness = 100;
							Body.SwitchWeapon(eActiveWeaponSlot.Standard);
							Body.VisibleActiveWeaponSlots = 16;
							Body.ParryChance = 15;
							Body.BlockChance = 60;
							Body.styleComponent.NextCombatStyle = slamWarrior;
						}
						if (target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun))
						{
							if (CanWalkWarrior == false)
							{
								int _walkBackTime = 500;
								ECSGameTimer _WalkBack = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WalkBack), _walkBackTime);//if target got stun then start timer to run behind it
								_WalkBack.Start(_walkBackTime);
								CanWalkWarrior = true;
							}
						}
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
						{
							CanWalkWarrior = false;//reset flag so can slam again
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Savage
		public static int TauntSavageID = 372;
		public static int TauntSavageClassID = 32;
		public static Style tauntSavage = SkillBase.GetStyleByID(TauntSavageID, TauntSavageClassID);

		public static int BackSavageID = 373;
		public static int BackSavageClassID = 32;
		public static Style BackSavage = SkillBase.GetStyleByID(BackSavageID, BackSavageClassID);

		public void IsSavage()
		{
			if (Body.PackageID == "NosdodenGhostSavage")
			{
				Body.EvadeChance = 50;
				Body.ParryChance = 15;
				Body.SwitchWeapon(eActiveWeaponSlot.Standard);
				Body.VisibleActiveWeaponSlots = 16;
				if (Body.InCombat && HasAggro)
				{
					if (Body.TargetObject != null)
					{
						if (Util.Chance(35))
							Body.CastSpell(Savage_dps_Buff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

						GameLiving target = Body.TargetObject as GameLiving;
						float angle = Body.TargetObject.GetAngle(Body);
						if (angle >= 160 && angle <= 200)
						{
							Body.SwitchWeapon(eActiveWeaponSlot.Standard);
							Body.VisibleActiveWeaponSlots = 16;
							foreach (Style styles in Body.Styles)
							{
								if (styles != null)
								{
									if (styles.ID == 373 && styles.ClassID == 32)
									{
										Body.Quickness = 80;
										Body.Strength = 200;
										Body.SwitchWeapon(eActiveWeaponSlot.Standard);
										Body.VisibleActiveWeaponSlots = 16;
										Body.styleComponent.NextCombatStyle = BackSavage;
									}
									else if (styles.ID == 372 && styles.ClassID == 32)
									{
										Body.Quickness = 80;
										Body.Strength = 170;
										Body.SwitchWeapon(eActiveWeaponSlot.Standard);
										Body.VisibleActiveWeaponSlots = 16;
										Body.styleComponent.NextCombatBackupStyle = tauntSavage;
									}
								}
							}
							if(!Body.Styles.Contains(BackSavage) && !Body.Styles.Contains(tauntSavage))
                            {
								Body.Strength = 250;
								Body.Quickness = 50;
								Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
								Body.VisibleActiveWeaponSlots = 34;
								Body.styleComponent.NextCombatStyle = Taunt2h;
							}
						}
						else
                        {
							foreach (Style styles in Body.Styles)
                            {
								if (styles != null)
                                {
									if (styles.ID == 372 && styles.ClassID == 32)
									{
										Body.Strength = 170;
										Body.Quickness = 80;
										Body.SwitchWeapon(eActiveWeaponSlot.Standard);
										Body.VisibleActiveWeaponSlots = 16;
										Body.styleComponent.NextCombatStyle = tauntSavage;
									}
									else
									{
										if (styles.ID == 103 && styles.ClassID == 1)
										{
											Body.Strength = 250;
											Body.Quickness = 50;
											Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
											Body.VisibleActiveWeaponSlots = 34;
											Body.styleComponent.NextCombatStyle = Taunt2h;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Thane
		private protected bool CanWalkThane = false;
		public void IsThane()
		{
			if (Body.PackageID == "NosdodenGhostThane")
			{
				if (Body.IsAlive && !HasAggro)
				{
					Body.ParryChance = 15;
					Body.BlockChance = 60;
					Body.SwitchWeapon(eActiveWeaponSlot.Standard);
					Body.VisibleActiveWeaponSlots = 16;
				}
				if (!HasAggressionTable())
					CanWalkThane = false;

				if (Body.IsAlive)
				{
					if (!Body.Styles.Contains(slamWarrior))
						Body.Styles.Add(slamWarrior);
					if (!Body.Styles.Contains(Taunt2h))
						Body.Styles.Add(Taunt2h);
					if (!Body.Styles.Contains(Back2h))
						Body.Styles.Add(Back2h);
				}
				if (HasAggro)
				{
					if (Body.TargetObject != null)
					{
						if (!Body.IsCasting && !Body.IsWithinRadius(Body.TargetObject, Body.AttackRange))
						{
							if (Body.attackComponent.AttackState)
								Body.attackComponent.NPCStopAttack();
							if (Body.IsMoving)
								Body.StopFollowing();
							Body.TurnTo(Body.TargetObject);
							Body.CastSpell(InstantThaneDD_casting, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
						}

						if(Util.Chance(15) && Body.IsWithinRadius(Body.TargetObject,Body.AttackRange))
							Body.CastSpell(InstantThaneDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
						if (Util.Chance(15) && Body.IsWithinRadius(Body.TargetObject, Body.AttackRange))
							Body.CastSpell(InstantThaneDD_pbaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
						
						GameLiving target = Body.TargetObject as GameLiving;
						float angle = Body.TargetObject.GetAngle(Body);
						if (angle >= 160 && angle <= 200)
						{
							Body.Strength = 220;
							Body.Quickness = 60;
							Body.ParryChance = 50;
							Body.BlockChance = 0;
							Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
							Body.VisibleActiveWeaponSlots = 34;
							Body.styleComponent.NextCombatStyle = Back2h;
							Body.styleComponent.NextCombatBackupStyle = Taunt2h;
						}
						else
						{
							Body.Strength = 160;
							Body.Quickness = 100;
							Body.ParryChance = 15;
							Body.BlockChance = 50;
							Body.SwitchWeapon(eActiveWeaponSlot.Standard);
							Body.VisibleActiveWeaponSlots = 16;
							foreach (Style styles in Body.Styles)
							{
								if (styles != null)
								{
									if (styles.ID == 157 && styles.ClassID == 22)
										Body.styleComponent.NextCombatStyle = tauntSwordWarrior;

									if (styles.ID == 178 && styles.ClassID == 22)
										Body.styleComponent.NextCombatStyle = tauntAxeWarrior;

									if (styles.ID == 167 && styles.ClassID == 22)
										Body.styleComponent.NextCombatStyle = tauntHammerWarrior;
								}
							}
						}
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
						{
							Body.Strength = 160;
							Body.Quickness = 100;
							Body.SwitchWeapon(eActiveWeaponSlot.Standard);
							Body.VisibleActiveWeaponSlots = 16;
							Body.ParryChance = 15;
							Body.BlockChance = 50;
							Body.styleComponent.NextCombatStyle = slamWarrior;
						}
						if (target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun))
						{
							if (CanWalkThane == false)
							{
								int _walkBackTime = 500;
								ECSGameTimer _WalkBack = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WalkBack), _walkBackTime);//if target got stun then start timer to run behind it
								_WalkBack.Start(_walkBackTime);
								CanWalkThane = true;
							}
						}
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
							CanWalkThane = false;//reset flag so can slam again
					}
				}
			}
		}
		#endregion
		#region Mob Class Skald
		public static int AfterParry2hID = 108;
		public static int AfterParry2hClassID = 1;
		public static Style AfterParry2h = SkillBase.GetStyleByID(AfterParry2hID, AfterParry2hClassID);

		public static int Taunt2hID = 103;
		public static int Taunt2hClassID = 1;
		public static Style Taunt2h = SkillBase.GetStyleByID(Taunt2hID, Taunt2hClassID);

		public static int Back2hID = 113;
		public static int Back2hClassID = 1;
		public static Style Back2h = SkillBase.GetStyleByID(Back2hID, Back2hClassID);
		public void IsSkald()
		{
			if (Body.PackageID == "NosdodenGhostSkald")
			{
				Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
				Body.VisibleActiveWeaponSlots = 34;
				Body.ParryChance = 50;
				if(Body.IsAlive)
                {
					if (!Body.Styles.Contains(Taunt2h))
						Body.Styles.Add(Taunt2h);
					if (!Body.Styles.Contains(AfterParry2h))
						Body.Styles.Add(AfterParry2h);
                }
				if(!HasAggressionTable())
                {
					lock (Body.effectListComponent._effectsLock)
					{
						var effects = Body.effectListComponent.GetAllPulseEffects();
						for (int i = 0; i < effects.Count; i++)
						{
							ECSPulseEffect effect = effects[i];
							if (effect == null)
								continue;

							if (effect == null)
								continue;
							if (effect.SpellHandler.Spell.Pulse == 1)
							{
								EffectService.RequestCancelConcEffect(effect);//cancel here all pulse effect
							}
						}
					}
				}
				if (HasAggro)
				{
					if (Body.TargetObject != null)
					{
						if (Util.Chance(30))
							Body.CastSpell(Skald_DA, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
						if (Util.Chance(35) && Body.IsWithinRadius(Body.TargetObject, 700))
							Body.CastSpell(InstantSkaldDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
						if (Util.Chance(35) && Body.IsWithinRadius(Body.TargetObject, 700))
							Body.CastSpell(InstantSkaldDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

						if (Util.Chance(100))
						{
							Body.Quickness = 80;
							Body.Strength = 220;
							Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
							Body.VisibleActiveWeaponSlots = 34;
							Body.styleComponent.NextCombatStyle = AfterParry2h;
							Body.styleComponent.NextCombatBackupStyle = Taunt2h;
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Hunter
		private protected bool Switch_to_Ranged = false;

		public static int TauntSpearHuntID = 217;
		public static int TauntSpearHuntClassID = 25;
		public static Style TauntSpearHunt = SkillBase.GetStyleByID(TauntSpearHuntID, TauntSpearHuntClassID);

		public static int BackSpearHuntID = 218;
		public static int BackSpearHuntClassID = 25;
		public static Style BackSpearHunt = SkillBase.GetStyleByID(BackSpearHuntID, BackSpearHuntClassID);
		public void IsHunter()
		{
			if (Body.PackageID == "NosdodenGhostHunter")
			{
				Body.EvadeChance = 40;
				if (Body.IsAlive)
				{
					if (!Body.Styles.Contains(Taunt2h))
						Body.Styles.Add(Taunt2h);
				}
				if (!HasAggressionTable())
				{
					Body.SwitchWeapon(eActiveWeaponSlot.Distance);
					Body.VisibleActiveWeaponSlots = 51;
					CanCreateHunterPet = false;
					Switch_to_Ranged = false;
					foreach(GameNPC npc in Body.GetNPCsInRadius(5000))
                    {
						if(npc != null)
                        {
							if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "GhostHunterPet" && npc.Brain is StandardMobBrain brain && !brain.HasAggro)
								npc.Die(npc);
                        }
                    }
				}
				if (HasAggro)
				{
					if (Body.TargetObject != null)
					{
						CreateHunterPet();
						if (!Body.IsWithinRadius(Body.TargetObject, 200))
						{
							if (Body.IsMoving)
								Body.StopFollowing();
							if (Switch_to_Ranged == false)
							{
								Body.SwitchWeapon(eActiveWeaponSlot.Distance);
								Body.VisibleActiveWeaponSlots = 51;
								Body.Strength = 220;
								Switch_to_Ranged = true;
							}
						}
						if (Body.IsWithinRadius(Body.TargetObject, 200))
						{
							Switch_to_Ranged = false;
							GameLiving target = Body.TargetObject as GameLiving;
							float angle = Body.TargetObject.GetAngle(Body);
							if (angle >= 160 && angle <= 200)
							{
								foreach (Style styles in Body.Styles)
								{
									if (styles != null)
									{
										if (styles.ID == 218 && styles.ClassID == 25)
										{
											Body.Quickness = 60;
											Body.Strength = 220;
											Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
											Body.VisibleActiveWeaponSlots = 34;
											Body.styleComponent.NextCombatStyle = BackSpearHunt;
										}
										else if (styles.ID == 217 && styles.ClassID == 25)
										{
											Body.Quickness = 60;
											Body.Strength = 170;
											Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
											Body.VisibleActiveWeaponSlots = 34;
											Body.styleComponent.NextCombatBackupStyle = TauntSpearHunt;
										}
									}
								}
								if (!Body.Styles.Contains(BackSpearHunt) && !Body.Styles.Contains(TauntSpearHunt))
								{
									Body.Quickness = 60;
									Body.Strength = 170;
									Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
									Body.VisibleActiveWeaponSlots = 34;
									Body.styleComponent.NextCombatStyle = Taunt2h;
								}
							}
							else
							{
								foreach (Style styles in Body.Styles)
								{
									if (styles != null)
									{
										if (styles.ID == 217 && styles.ClassID == 25)
										{
											Body.Quickness = 60;
											Body.Strength = 170;
											Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
											Body.VisibleActiveWeaponSlots = 34;
											Body.styleComponent.NextCombatStyle = TauntSpearHunt;
										}
									}
								}
								if (!Body.Styles.Contains(BackSpearHunt) && !Body.Styles.Contains(TauntSpearHunt))
								{
									Body.Quickness = 60;
									Body.Strength = 170;
									Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
									Body.VisibleActiveWeaponSlots = 34;
									Body.styleComponent.NextCombatStyle = Taunt2h;
								}
							}
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Shadowblade
		private protected bool CanWalkShadowblade = false;

		public static int AnyTimerSBID = 342;
		public static int AnyTimerSBClassID = 23;
		public static Style AnyTimerSB = SkillBase.GetStyleByID(AnyTimerSBID, AnyTimerSBClassID);

		public static int AnyTimerFollowUpSBID = 344;
		public static int AnyTimerFollowUpSBClassID = 23;
		public static Style AnyTimerFollowUpSB = SkillBase.GetStyleByID(AnyTimerFollowUpSBID, AnyTimerFollowUpSBClassID);
		public void IsShadowblade()
		{
			if (Body.PackageID == "NosdodenGhostShadowblade")
			{
				Body.SwitchWeapon(eActiveWeaponSlot.Standard);
				Body.VisibleActiveWeaponSlots = 16;
				Body.EvadeChance = 60;
				if (Body.IsAlive)
				{
					if (!Body.Styles.Contains(AnyTimerSB))
						Body.Styles.Add(AnyTimerSB);
					if (!Body.Styles.Contains(AfterEvadeBerserker))
						Body.Styles.Add(AfterEvadeBerserker);
					if (!Body.Styles.Contains(EvadeFollowUpBerserker))
						Body.Styles.Add(EvadeFollowUpBerserker);
					if (!Body.Styles.Contains(BackBerserker))
						Body.Styles.Add(BackBerserker);
				}
				if (!HasAggressionTable())
				{
					CanWalkShadowblade = false;
				}
				if (Body.InCombat && HasAggro)
				{
					if (Body.TargetObject != null)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						float angle = Body.TargetObject.GetAngle(Body);
						if (angle >= 160 && angle <= 200)
						{
							Body.Quickness = 100;
							Body.Strength = 180;
							Body.styleComponent.NextCombatStyle = BackBerserker;
						}
						else
						{
							Body.Quickness = 100;
							Body.Strength = 150;
						}
						if (target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun))
						{
							if (CanWalkShadowblade == false)
							{
								int _walkBackTime = 500;
								ECSGameTimer _WalkBack = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WalkBack), _walkBackTime);//if target got stun then start timer to run behind it
								_WalkBack.Start(_walkBackTime);
								CanWalkShadowblade = true;
							}
						}
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
						{
							CanWalkShadowblade = false;//reset flag so can slam again
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Runemaster
		public void IsRunemaster()
		{
			if (Body.PackageID == "NosdodenGhostRunemaster")
			{
				Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
				Body.VisibleActiveWeaponSlots = 34;
				if (Body.IsAlive)
				{
					if (!Body.Spells.Contains(Rune_Bolt))
						Body.Spells.Add(Rune_Bolt);
					if (!Body.Spells.Contains(Rune_DD))
						Body.Spells.Add(Rune_DD);
				}
				if (HasAggro)
				{
					if (Body.TargetObject != null)
					{
						if (!Body.IsCasting && !Body.IsMoving)
						{						
							foreach(Spell spells in Body.Spells)
                            {
								if(spells != null)
                                {
									if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject,spells.Range))
										Body.StopFollowing();
									else
										Body.Follow(Body.TargetObject,spells.Range - 50,5000);

									Body.TurnTo(Body.TargetObject);
									if (Util.Chance(100))
									{
										if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Rune_Bolt) == 0)
											Body.CastSpell(Rune_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
										else
											Body.CastSpell(Rune_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
									}
								}
                            }
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Spiritmaster
		public void IsSpiritmaster()
		{
			if (Body.PackageID == "NosdodenGhostSpiritmaster")
			{
				Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
				Body.VisibleActiveWeaponSlots = 34;
				if(!HasAggressionTable())
                {
				}
				if (Body.IsAlive)
				{
					if (!Body.Spells.Contains(Spirit_DD))
						Body.Spells.Add(Spirit_DD);
					if (!Body.Spells.Contains(Spirit_Mezz))
						Body.Spells.Add(Spirit_Mezz);
				}
				if (HasAggro)
				{
					SummonSpiritChampion();
					foreach (GameNPC npc in Body.GetNPCsInRadius(2000))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.Brain is GhostSpiritChampionBrain brain && npc.PackageID == Convert.ToString(Body.ObjectID))
							{
								GameLiving target = Body.TargetObject as GameLiving;
								if (target != null)
								{
									if (!brain.HasAggro)
										brain.AddToAggroList(target, 100);
								}
							}
						}
					}
					if (Body.TargetObject != null)
					{
						if (!Body.IsCasting && !Body.IsMoving)
						{
							foreach (Spell spells in Body.Spells)
							{
								if (spells != null)
								{
									if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
										Body.StopFollowing();
									else
										Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

									Body.TurnTo(Body.TargetObject);
									if (Util.Chance(100))
									{
										if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Spirit_Mezz) == 0)
											Body.CastSpell(Spirit_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
										else
											Body.CastSpell(Spirit_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									}
								}
							}
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Bonedancer
		public void IsBonedancer()
		{
			if (Body.PackageID == "NosdodenGhostBonedancer")
			{
				Body.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
				Body.VisibleActiveWeaponSlots = 34;
				if (!HasAggressionTable())
				{
				}
				if (Body.IsAlive)
				{
					if (!Body.Spells.Contains(Bone_DD))
						Body.Spells.Add(Bone_DD);
					if (!Body.Spells.Contains(Bone_DD2))
						Body.Spells.Add(Bone_DD2);
				}
				if (HasAggro)
				{
					SummonSkeletalCommander();
					foreach (GameNPC npc in Body.GetNPCsInRadius(2000))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.Brain is GhostSkeletalCommanderBrain brain && npc.PackageID == Convert.ToString(Body.ObjectID))
							{
								GameLiving target = Body.TargetObject as GameLiving;
								if (target != null)
								{
									if (!brain.HasAggro)
										brain.AddToAggroList(target, 100);
								}
							}
						}
					}
					if (Body.TargetObject != null)
					{
						if (!Body.IsCasting && !Body.IsMoving)
						{
							foreach (Spell spells in Body.Spells)
							{
								if (spells != null)
								{
									if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
										Body.StopFollowing();
									else
										Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

									Body.TurnTo(Body.TargetObject);
									if (Util.Chance(100))
									{
										if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Bone_DD) == 0)
											Body.CastSpell(Bone_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
										else
											Body.CastSpell(Bone_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									}
								}
							}
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Healer
		private protected GamePlayer randomhealertarget = null;
		private protected GamePlayer RandomHealerTarget
		{
			get { return randomhealertarget; }
			set { randomhealertarget = value; }
		}
		private protected List<GamePlayer> HealerEnemys_To_Mezz = new List<GamePlayer>();
		private protected void PickTargetToMezz()
        {
			if(HasAggro)
            {
				foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1)
						{
							if (!HealerEnemys_To_Mezz.Contains(player) && (!player.effectListComponent.ContainsEffectForEffectType(eEffect.MezImmunity) || !player.effectListComponent.ContainsEffectForEffectType(eEffect.Mez)))
								HealerEnemys_To_Mezz.Add(player);
						}
					}
				}
				if (HealerEnemys_To_Mezz.Count > 0)
				{
					if (Body.GetSkillDisabledDuration(Healer_Mezz) == 0)
					{
						GamePlayer Target = HealerEnemys_To_Mezz[Util.Random(0, HealerEnemys_To_Mezz.Count - 1)];//pick random target from list
						RandomHealerTarget = Target;
						if (RandomHealerTarget != null && RandomHealerTarget.IsAlive
							&& (!RandomHealerTarget.effectListComponent.ContainsEffectForEffectType(eEffect.Mez) || !RandomHealerTarget.effectListComponent.ContainsEffectForEffectType(eEffect.MezImmunity)))
						{
							Body.TargetObject = RandomHealerTarget;
							Body.TurnTo(RandomHealerTarget);
							Body.CastSpell(Healer_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);						
						}
					}
				}
			}
        }
		public void IsHealer()
		{
			if (Body.PackageID == "NosdodenGhostHealer")
			{
				Body.SwitchWeapon(eActiveWeaponSlot.Standard);
				Body.VisibleActiveWeaponSlots = 16;
				if (Body.IsAlive)
				{
					if (!Body.Spells.Contains(Healer_Heal))
						Body.Spells.Add(Healer_Heal);
					if (!Body.Spells.Contains(Healer_Mezz))
						Body.Spells.Add(Healer_Mezz);
					if (!Body.Spells.Contains(Healer_Amnesia))
						Body.Spells.Add(Healer_Amnesia);
				}
				if(!HasAggressionTable())
                {
					RandomHealerTarget = null;
					if (HealerEnemys_To_Mezz.Count > 0)
						HealerEnemys_To_Mezz.Clear();

					foreach(GameNPC npc in Body.GetNPCsInRadius((ushort)Healer_Heal.Range))
                    {
						if(npc != null)
                        {
							if(npc.IsAlive && npc.Faction == Body.Faction && npc.HealthPercent < 100)
                            {
								Body.TargetObject = npc;
								if(npc != Body)
									Body.TurnTo(npc);
								Body.CastSpell(Healer_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
							}
                        }							
                    }
                }
				if (HasAggro)
				{
					if (Body.TargetObject != null)
					{
						GameLiving oldtarget = Body.TargetObject as GameLiving;
						if (!Body.IsCasting && !Body.IsMoving)
						{
							foreach (Spell spells in Body.Spells)
							{
								if (spells != null)
								{
									if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
										Body.StopFollowing();
									else
										Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

									if (Util.Chance(100))
									{
										if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Healer_Mezz) == 0)
											PickTargetToMezz();
										else
										{
											foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)Healer_Heal.Range))
											{
												if (npc != null)
												{
													if (npc.IsAlive && npc.Faction == Body.Faction)
													{
														if (npc.HealthPercent < 100)
														{
															Body.TargetObject = npc;
															if(npc != Body)
																Body.TurnTo(npc);
															Body.CastSpell(Healer_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
														}
														if (npc.HealthPercent == 100)
														{
															if (oldtarget != null && oldtarget != Body && !HealerEnemys_To_Mezz.Contains(Body.TargetObject as GamePlayer))
															{
																Body.TargetObject = CalculateNextAttackTarget();
																Body.TurnTo(Body.TargetObject);
																Body.CastSpell(Healer_Amnesia, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		#endregion
		#region Mob Class Shaman 
		public void IsShaman()
		{
			if (Body.PackageID == "NosdodenGhostShaman")
			{
				Body.SwitchWeapon(eActiveWeaponSlot.Standard);
				Body.VisibleActiveWeaponSlots = 16;
				if (Body.IsAlive)
				{
					if (!Body.Spells.Contains(Shamy_Bolt))
						Body.Spells.Add(Shamy_Bolt);
					if (!Body.Spells.Contains(Shamy_DD))
						Body.Spells.Add(Shamy_DD);
					if (!Body.Spells.Contains(Shamy_AoeDot))
						Body.Spells.Add(Shamy_AoeDot);
					if (!Body.Spells.Contains(Shamy_InstaAoeDisease))
						Body.Spells.Add(Shamy_InstaAoeDisease);
				}
				if (HasAggro)
				{
					if (Body.TargetObject != null)
					{
						if (!Body.IsCasting && !Body.IsMoving)
						{
							foreach (Spell spells in Body.Spells)
							{
								if (spells != null)
								{
									if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
										Body.StopFollowing();
									else
										Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

									Body.TurnTo(Body.TargetObject);
									if (Util.Chance(100))
									{
										if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Shamy_Bolt) == 0)
											Body.CastSpell(Shamy_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
										else if(spells.HasRecastDelay && Body.GetSkillDisabledDuration(Shamy_AoeDot) == 0)
											Body.CastSpell(Shamy_AoeDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
										else if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Shamy_InstaAoeDisease) == 0)
											Body.CastSpell(Shamy_InstaAoeDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
										else 
											Body.CastSpell(Shamy_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									}
								}
							}
						}
					}
				}
			}
		}
		#endregion
		public override void Think()
		{
			if(Body.IsAlive)
            {
				IsBerserker();
				IsWarrior();
				IsSavage();
				IsThane();
				IsSkald();
				IsHunter();
				IsShadowblade();
				IsRunemaster();
				IsSpiritmaster();
				IsBonedancer();
				IsHealer();
				IsShaman();
            }
			base.Think();
		}
        public int WalkBack(ECSGameTimer timer)
		{
			if (Body.InCombat && HasAggro && Body.TargetObject != null)
			{
				if (Body.TargetObject is GameLiving)
				{
					GameLiving living = Body.TargetObject as GameLiving;
					float angle = living.GetAngle(Body);
					Point2D positionalPoint;
					positionalPoint = living.GetPointFromHeading((ushort)(living.Heading + (180 * (4096.0 / 360.0))), 65);
					//Body.WalkTo(positionalPoint.X, positionalPoint.Y, living.Z, 280);
					Body.X = positionalPoint.X;
					Body.Y = positionalPoint.Y;
					Body.Z = living.Z;
					Body.Heading = 1250;
				}
			}
			return 0;
		}
		#region Spells
		#region Spells Thane
		private Spell m_InstantThaneDD;
		public Spell InstantThaneDD
		{
			get
			{
				if (m_InstantThaneDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 20;
					spell.ClientEffect = 3510;
					spell.Icon = 3510;
					spell.Damage = 120;
					spell.DamageType = (int)eDamageType.Energy;
					spell.Name = "Toothgnasher's Ram";
					spell.Range = 1500;
					spell.SpellID = 11869;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_InstantThaneDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantThaneDD);
				}
				return m_InstantThaneDD;
			}
		}
		private Spell m_InstantThaneDD_pbaoe;
		public Spell InstantThaneDD_pbaoe
		{
			get
			{
				if (m_InstantThaneDD_pbaoe == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 20;
					spell.ClientEffect = 3528;
					spell.Icon = 35280;
					spell.Damage = 120;
					spell.DamageType = (int)eDamageType.Energy;
					spell.Name = "Greater Thunder Roar";
					spell.Range = 0;
					spell.Radius = 350;
					spell.SpellID = 11870;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_InstantThaneDD_pbaoe = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantThaneDD_pbaoe);
				}
				return m_InstantThaneDD_pbaoe;
			}
		}
		private Spell m_InstantThaneDD_casting;
		public Spell InstantThaneDD_casting
		{
			get
			{
				if (m_InstantThaneDD_casting == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 3510;
					spell.Icon = 3510;
					spell.Damage = 300;
					spell.DamageType = (int)eDamageType.Energy;
					spell.Name = "Thor's Full Lightning";
					spell.Range = 1500;
					spell.SpellID = 11871;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_InstantThaneDD_casting = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantThaneDD_casting);
				}
				return m_InstantThaneDD_casting;
			}
		}
		#endregion
		#region Spells Skald
		private Spell m_InstantSkaldDD;
		public Spell InstantSkaldDD
		{
			get
			{
				if (m_InstantSkaldDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 15;
					spell.ClientEffect = 3628;
					spell.Icon = 3628;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Body;
					spell.Name = "Battle Roar";
					spell.Range = 700;
					spell.SpellID = 11872;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_InstantSkaldDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantSkaldDD);
				}
				return m_InstantSkaldDD;
			}
		}
		private Spell m_InstantSkaldDD2;
		public Spell InstantSkaldDD2
		{
			get
			{
				if (m_InstantSkaldDD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 15;
					spell.ClientEffect = 3624;
					spell.Icon = 3624;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Body;
					spell.Name = "Battle Roar";
					spell.Range = 700;
					spell.SpellID = 11873;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_InstantSkaldDD2 = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantSkaldDD2);
				}
				return m_InstantSkaldDD2;
			}
		}
		private Spell m_Skald_DA;
		public Spell Skald_DA
		{
			get
			{
				if (m_Skald_DA == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 8;
					spell.Duration = 5;
					spell.Frequency = 50;
					spell.Pulse = 1;
					spell.ClientEffect = 3607;
					spell.Icon = 3607;
					spell.Damage = 10;
					spell.DamageType = (int)eDamageType.Body;
					spell.Name = "Chant of Blood";
					spell.Range = 700;
					spell.SpellID = 11875;
					spell.Target = "Self";
					spell.Type = eSpellType.DamageAdd.ToString();
					m_Skald_DA = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Skald_DA);
				}
				return m_Skald_DA;
			}
		}
		#endregion
		#region Spells Savage
		private Spell m_Savage_dps_Buff;
		private Spell Savage_dps_Buff
		{
			get
			{
				if (m_Savage_dps_Buff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 30;
					spell.Duration = 20;
					spell.ClientEffect = 10541;
					spell.Icon = 10541;
					spell.Name = "Savage Blows";
					spell.Message2 = "{0} takes on a feral aura.";
					spell.TooltipId = 10541;
					spell.Range = 0;
					spell.Value = 25;
					spell.SpellID = 11874;
					spell.Target = "Self";
					spell.Type = eSpellType.SavageDPSBuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Savage_dps_Buff = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Savage_dps_Buff);
				}
				return m_Savage_dps_Buff;
			}
		}

		#endregion
		#region Hunter Pet summon
		private protected bool CanCreateHunterPet = false;
		public void CreateHunterPet()
        {
			if(CanCreateHunterPet==false && Body.PackageID == "NosdodenGhostHunter")
            {
				GameLiving ptarget = CalculateNextAttackTarget();
				GameNPC pet = new GameNPC();
				pet.Name = "Hunter's Avatar";
				pet.Model = 648;
				pet.Size = 60;
				pet.Level = 50;
				pet.Strength = 150;
				pet.Quickness = 80;
				pet.MaxSpeedBase = 225;
				pet.Health = 2500;
				pet.X = Body.X;
				pet.Y = Body.Y;
				pet.Z = Body.Z;
				pet.Heading = Body.Heading;
				pet.CurrentRegionID = Body.CurrentRegionID;
				pet.PackageID = "GhostHunterPet";
				pet.RespawnInterval = -1;
				StandardMobBrain sbrain = new StandardMobBrain();
				pet.SetOwnBrain(sbrain);
				sbrain.AggroRange = 500;
				sbrain.AggroLevel = 100;
				if (ptarget != null)
				{
					sbrain.AddToAggroList(ptarget, 10);
					pet.StartAttack(ptarget);
				}
				pet.AddToWorld();
				CanCreateHunterPet = true;
            }
        }
		#endregion
		#region Spells Runemaster
		private Spell m_Rune_DD;
		private Spell Rune_DD
		{
			get
			{
				if (m_Rune_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 2570;
					spell.Icon = 2570;
					spell.TooltipId = 2570;
					spell.Damage = 300;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Greater Rune of Shadow";
					spell.Range = 1500;
					spell.SpellID = 11877;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					m_Rune_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Rune_DD);
				}
				return m_Rune_DD;
			}
		}
		private Spell m_Rune_Bolt;
		private Spell Rune_Bolt
		{
			get
			{
				if (m_Rune_Bolt == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 2970;
					spell.Icon = 2970;
					spell.TooltipId = 2970;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Sigil of Undoing";
					spell.Range = 1800;
					spell.SpellID = 11878;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Bolt.ToString();
					spell.Uninterruptible = true;
					m_Rune_Bolt = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Rune_Bolt);
				}
				return m_Rune_Bolt;
			}
		}
		#endregion
		#region Spells Spiritmaster and Summon Pet
		private Spell m_Spirit_DD;
		private Spell Spirit_DD
		{
			get
			{
				if (m_Spirit_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 2610;
					spell.Icon = 2610;
					spell.TooltipId = 2610;
					spell.Damage = 320;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Extinguish Lifeforce";
					spell.Range = 1500;
					spell.SpellID = 11879;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					m_Spirit_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Spirit_DD);
				}
				return m_Spirit_DD;
			}
		}
		private Spell m_Spirit_Mezz;
		private Spell Spirit_Mezz
		{
			get
			{
				if (m_Spirit_Mezz == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 30;
					spell.ClientEffect = 2643;
					spell.Icon = 2643;
					spell.TooltipId = 2643;
					spell.Duration = 35;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Description = "Target is mesmerized and cannot move or take any other action for the duration of the spell. If the target suffers any damage or other negative effect the spell will break.";
					spell.Name = "Umbral Shroud";
					spell.Range = 1500;
					spell.Radius = 450;
					spell.SpellID = 11880;
					spell.Target = "Enemy";
					spell.Type = "Mesmerize";
					spell.Uninterruptible = true;
					m_Spirit_Mezz = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Spirit_Mezz);
				}
				return m_Spirit_Mezz;
			}
		}	
		public void SummonSpiritChampion()
        {
			foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
			{
				if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == Convert.ToString(Body.ObjectID) && npc.Brain is GhostSpiritChampionBrain)
					return;
			}
			if (Body.PackageID == "NosdodenGhostSpiritmaster")
			{
				GhostSpiritChampion pet = new GhostSpiritChampion();
				pet.X = Body.X;
				pet.Y = Body.Y-100;
				pet.Z = Body.Z;
				pet.Heading = Body.Heading;
				pet.CurrentRegionID = Body.CurrentRegionID;
				pet.Faction = Body.Faction;
				pet.PackageID = Convert.ToString(Body.ObjectID);
				pet.RespawnInterval = -1;
				GhostSpiritChampionBrain sbrain = new GhostSpiritChampionBrain();
				pet.SetOwnBrain(sbrain);
				sbrain.AggroRange = 500;
				sbrain.AggroLevel = 100;
				pet.AddToWorld();
				pet.Brain.Start();
			}
		}
		#endregion
		#region Spells Bonedancer and Summon Skeletal Commander
		private Spell m_Bone_DD2;
		private Spell Bone_DD2
		{
			get
			{
				if (m_Bone_DD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 10029;
					spell.Icon = 10029;
					spell.TooltipId = 10029;
					spell.Damage = 320;
					spell.Value = 35;
					spell.Duration = 30;
					spell.LifeDrainReturn = 90;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Description = "Target is damaged for 179 and also moves 35% slower for the spell duration.";
					spell.Name = "Crystallize Skeleton";
					spell.Range = 1500;
					spell.SpellID = 11882;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DamageSpeedDecreaseNoVariance.ToString();
					m_Bone_DD2 = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bone_DD2);
				}
				return m_Bone_DD2;
			}
		}
		private Spell m_Bone_DD;
		private Spell Bone_DD
		{
			get
			{
				if (m_Bone_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 4;
					spell.ClientEffect = 10081;
					spell.Icon = 10081;
					spell.TooltipId = 10081;
					spell.Damage = 250;
					spell.Value = -90;
					spell.LifeDrainReturn = 90;
					spell.DamageType = (int)eDamageType.Body;
					spell.Name = "Pulverize Skeleton";
					spell.Range = 1500;
					spell.SpellID = 11881;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Lifedrain.ToString();
					m_Bone_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bone_DD);
				}
				return m_Bone_DD;
			}
		}
		public void SummonSkeletalCommander()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
			{
				if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == Convert.ToString(Body.ObjectID) && npc.Brain is GhostSkeletalCommanderBrain)
					return;
			}
			if (Body.PackageID == "NosdodenGhostBonedancer")
			{
				GhostSkeletalCommander pet = new GhostSkeletalCommander();
				pet.X = Body.X;
				pet.Y = Body.Y - 100;
				pet.Z = Body.Z;
				pet.Heading = Body.Heading;
				pet.CurrentRegionID = Body.CurrentRegionID;
				pet.Faction = Body.Faction;
				pet.PackageID = Convert.ToString(Body.ObjectID);
				pet.RespawnInterval = -1;
				GhostSkeletalCommanderBrain sbrain = new GhostSkeletalCommanderBrain();
				pet.SetOwnBrain(sbrain);
				sbrain.AggroRange = 500;
				sbrain.AggroLevel = 100;
				pet.AddToWorld();
				pet.Brain.Start();
			}
		}
		#endregion
		#region Spells Healer
		private Spell m_Healer_Heal;
		private Spell Healer_Heal
		{
			get
			{
				if (m_Healer_Heal == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 3058;
					spell.Icon = 3058;
					spell.TooltipId = 3058;
					spell.Value = 400;
					spell.Name = "Heal";
					spell.Range = 2500;
					spell.SpellID = 11885;
					spell.Target = "Realm";
					spell.Type = "Heal";
					m_Healer_Heal = new Spell(spell, 70);
					spell.Uninterruptible = true;
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Healer_Heal);
				}
				return m_Healer_Heal;
			}
		}
		private Spell m_Healer_Mezz;
		private Spell Healer_Mezz
		{
			get
			{
				if (m_Healer_Mezz == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 3371;
					spell.Icon = 3371;
					spell.TooltipId = 3371;
					spell.Duration = 65;
					spell.Name = "Tranquilize Area";
					spell.Description = "Target is mesmerized and cannot move or take any other action for the duration of the spell. If the target suffers any damage or other negative effect the spell will break.";
					spell.Range = 1500;
					spell.Radius = 400;
					spell.SpellID = 11886;
					spell.Target = "Enemy";
					spell.Type = "Mesmerize";
					spell.DamageType = (int)eDamageType.Body;
					m_Healer_Mezz = new Spell(spell, 70);
					spell.Uninterruptible = true;
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Healer_Mezz);
				}
				return m_Healer_Mezz;
			}
		}
		private Spell m_Healer_Amnesia;
		private Spell Healer_Amnesia
		{
			get
			{
				if (m_Healer_Amnesia == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;					
					spell.CastTime = 2;
					spell.RecastDelay = 0;
					spell.ClientEffect = 3315;
					spell.Icon = 3315;
					spell.TooltipId = 3315;
					spell.Name = "Wake Oblivious";
					spell.AmnesiaChance = 100;
					spell.Message2 = "{0} forgets what they were doing!";
					spell.Range = 2300;
					spell.Radius = 350;
					spell.SpellID = 11887;
					spell.Target = "Enemy";
					spell.Type = "Amnesia";
					m_Healer_Amnesia = new Spell(spell, 44);
					spell.Uninterruptible = true;
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Healer_Amnesia);
				}
				return m_Healer_Amnesia;
			}
		}
		#endregion
		#region Spells Shaman
		private Spell m_Shamy_Bolt;
		private Spell Shamy_Bolt
		{
			get
			{
				if (m_Shamy_Bolt == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 3470;
					spell.Icon = 3470;
					spell.TooltipId = 3470;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Name = "Fungal Spine";
					spell.Range = 1800;
					spell.SpellID = 11888;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Bolt.ToString();
					spell.Uninterruptible = true;
					m_Shamy_Bolt = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Shamy_Bolt);
				}
				return m_Shamy_Bolt;
			}
		}
		private Spell m_Shamy_DD;
		private Spell Shamy_DD
		{
			get
			{
				if (m_Shamy_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 3494;
					spell.Icon = 3494;
					spell.TooltipId = 3494;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Name = "Fungal Mucus";
					spell.Range = 1500;
					spell.SpellID = 11890;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					m_Shamy_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Shamy_DD);
				}
				return m_Shamy_DD;
			}
		}
		private Spell m_Shamy_InstaAoeDisease;
		private Spell Shamy_InstaAoeDisease
		{
			get
			{
				if (m_Shamy_InstaAoeDisease == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 30;
					spell.ClientEffect = 3425;
					spell.Icon = 3425;
					spell.TooltipId = 3425;
					spell.Duration = 120;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Description = "Inflicts a wasting disease on the target that slows it, weakens it, and inhibits heal spells.";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.Name = "Plague Spores";
					spell.Range = 1500;
					spell.Radius = 400;
					spell.SpellID = 11891;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Disease.ToString();
					spell.Uninterruptible = true;
					m_Shamy_InstaAoeDisease = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Shamy_InstaAoeDisease);
				}
				return m_Shamy_InstaAoeDisease;
			}
		}
		private Spell m_Shamy_AoeDot;
		private Spell Shamy_AoeDot
		{
			get
			{
				if (m_Shamy_AoeDot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 15;
					spell.ClientEffect = 3475;
					spell.Icon = 3475;
					spell.TooltipId = 3475;
					spell.Damage = 83;
					spell.Duration = 24;
					spell.Frequency = 40;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Description = "Inflicts 83 damage to the target every 4 sec for 24 seconds";
					spell.Message1 = "Your body is covered with painful sores!";
					spell.Message2 = "{0}'s skin erupts in open wounds!";
					spell.Message3 = "The destructive energy wounding you fades.";
					spell.Message4 = "The destructive energy around {0} fades.";
					spell.Name = "Fungal Spine";
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11889;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					m_Shamy_AoeDot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Shamy_AoeDot);
				}
				return m_Shamy_AoeDot;
			}
		}
		#endregion
		#endregion
	}
}
namespace DOL.GS
{
	public class NosdodenGhostAdd : GameNPC
	{
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 35; // dmg reduction for melee dmg
				case eDamageType.Crush: return 35; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 35; // dmg reduction for melee dmg
				default: return 55; // dmg reduction for rest resists
			}
		}
        public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
        public override void DealDamage(AttackData ad)
        {
			if(ad != null)
            {
				if(PackageID == "NosdodenGhostSpiritmaster")
					Health += ad.Damage / 2;
            }
            base.DealDamage(ad);
        }
        public override void OnAttackedByEnemy(AttackData ad)
        {       
            if (ad != null && ad.AttackResult == eAttackResult.Evaded)
            {
				#region Berserker
				if (PackageID == "NosdodenGhostBerserker")
                {
					styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.tauntBerserker;
					styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AfterEvadeBerserker;
				}
				#endregion
				#region Shadowblade
				if (PackageID == "NosdodenGhostShadowblade")
				{
					styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.AnyTimerSB;
					styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AfterEvadeBerserker;
				}
				#endregion
			}
			base.OnAttackedByEnemy(ad);
        }
        public override void StartAttack(GameObject target)
        {
			if (PackageID == "NosdodenGhostRunemaster" || PackageID == "NosdodenGhostSpiritmaster" || PackageID == "NosdodenGhostBonedancer"
				|| PackageID == "NosdodenGhostHealer" || PackageID == "NosdodenGhostShaman")
				return;
			base.StartAttack(target);
        }
        public override void OnAttackEnemy(AttackData ad)
        {
            #region Berserker
            if (PackageID == "NosdodenGhostBerserker")
			{
				if (ad != null && ad.AttackResult == eAttackResult.HitUnstyled)
				{
					styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.tauntBerserker;
					styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AfterEvadeBerserker;
				}
				if (ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 198 && ad.Style.ClassID == 31)
				{
					styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.tauntBerserker;
					styleComponent.NextCombatStyle = NosdodenGhostAddBrain.EvadeFollowUpBerserker;
				}
			}
			#endregion
			#region Shadowblade
			if (PackageID == "NosdodenGhostShadowblade")
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
					CastSpell(SB_Lifebane, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (ad != null && ad.AttackResult == eAttackResult.HitUnstyled)
				{
					styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.AnyTimerSB;
					styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AnyTimerFollowUpSB;
				}
				if (ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 198 && ad.Style.ClassID == 31)
				{
					styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.AnyTimerSB;
					styleComponent.NextCombatStyle = NosdodenGhostAddBrain.EvadeFollowUpBerserker;
				}
				if (ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 342 && ad.Style.ClassID == 23)
				{
					styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.AnyTimerSB;
					styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AnyTimerFollowUpSB;
				}
			}
			#endregion
			base.OnAttackEnemy(ad);
        }
        public override double GetArmorAF(eArmorSlot slot)
		{
			return 400;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.35;
		}
        public override void Die(GameObject killer)
        {
            #region Kill pet Hunter
            if (PackageID == "NosdodenGhostHunter")
			{
				foreach (GameNPC npc in GetNPCsInRadius(5000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "GhostHunterPet"
							&& npc.Brain is StandardMobBrain brain && !brain.HasAggro)
							npc.Die(npc);
					}
				}
			}
			#endregion
			#region Kill pet Spiritmaster
			if (PackageID == "NosdodenGhostSpiritmaster")
			{
				foreach (GameNPC npc in GetNPCsInRadius(5000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == Convert.ToString(ObjectID) && npc.Brain is GhostSpiritChampionBrain)
							npc.Die(npc);
					}
				}
			}
			#endregion
			#region Kill pet Bonedancer
			if (PackageID == "NosdodenGhostBonedancer")
			{
				foreach (GameNPC npc in GetNPCsInRadius(5000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == Convert.ToString(ObjectID) && npc.Brain is GhostSkeletalCommanderBrain)
							npc.Die(npc);
					}
				}
			}
			#endregion
			base.Die(killer);
        }
        public override int MaxHealth
		{
			get { return 8000; }
		}
		public override bool AddToWorld()
		{
			RespawnInterval = -1;
			MaxSpeedBase = 225;
			Level = (byte)Util.Random(62, 66);
			Faction = FactionMgr.GetFactionByID(150);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
			NosdodenGhostAddBrain add = new NosdodenGhostAddBrain();
			SetOwnBrain(add);
			base.AddToWorld();
			return true;
		}
		#region Spells
		#region Spells Shadoblade
		private Spell m_SB_Lifebane;
		private Spell SB_Lifebane
		{
			get
			{
				if (m_SB_Lifebane == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.Duration = 20;
					spell.Frequency = 39;
					spell.ClientEffect = 4099;
					spell.Icon = 4099;
					spell.TooltipId = 4099;
					spell.Damage = 65;
					spell.DamageType = (int)eDamageType.Body;
					spell.Description = "Inflicts damage to the target repeatly over a given time period.";
					spell.Message1 = "You are afflicted with a vicious poison!";
					spell.Message2 = "{0} has been poisoned!";
					spell.Message3 = "The poison has run its course.";
					spell.Message4 = "{0} looks healthy again.";
					spell.Name = "Lifebane";
					spell.Range = 350;
					spell.SpellID = 11876;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DamageOverTime.ToString();
					m_SB_Lifebane = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SB_Lifebane);
				}
				return m_SB_Lifebane;
			}
		}
		#endregion
		#endregion
	}
}
//////////////////////////////////////////////////////////////////Spiritmaster Pet////////////////////////////////////////////////////
#region Spiritmaster pet
namespace DOL.GS
{
	public class GhostSpiritChampion : GameNPC
	{
		public override int MaxHealth
		{
			get { return 2500; }
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15; // dmg reduction for melee dmg
				case eDamageType.Crush: return 15; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 15; // dmg reduction for melee dmg
				default: return 25; // dmg reduction for rest resists
			}
		}
        public override void OnAttackEnemy(AttackData ad)
        {
			if(ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
            {
				if(Util.Chance(25) && (!ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity) || !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun)) && ad.Target.IsAlive)
					CastSpell(SpiritChampion_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}				
            base.OnAttackEnemy(ad);
        }
        public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.35;
		}
		public override void WalkToSpawn()
		{
			if (IsAlive)
				return;
			base.WalkToSpawn();
		}
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		List<ushort> spirit_champion_models = new List<ushort>()
		{
			153,162,137,146,773,784,169,178,185,194
		};
		public override bool AddToWorld()
		{
			Name = "spirit champion";
			Model = spirit_champion_models[Util.Random(0, spirit_champion_models.Count - 1)];
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 295, 0, 0, 0); //Slot,model,color,effect,extension
			template.AddNPCEquipment(eInventorySlot.ArmsArmor, 297, 0);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 296, 0);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 298, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 299, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.HeadArmor, 1216, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.Cloak, 677, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 310, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 79, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.Standard);
			VisibleActiveWeaponSlots = 16;
			Size = 50;
			Level = 50;
			MaxSpeedBase = 225;
			BlockChance = 40;
			RespawnInterval = -1;
			Flags ^= eFlags.GHOST;
			Realm = eRealm.None;
			GhostSpiritChampionBrain adds = new GhostSpiritChampionBrain();
			SetOwnBrain(adds);
			base.AddToWorld();
			return true;
		}
		public override void DropLoot(GameObject killer) //no loot
		{
		}
		public override long ExperienceValue => 0;
		private Spell m_SpiritChampion_stun;
		private Spell SpiritChampion_stun
		{
			get
			{
				if (m_SpiritChampion_stun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 2165;
					spell.Icon = 2132;
					spell.TooltipId = 2132;
					spell.Duration = 4;
					spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
					spell.Name = "Stun";
					spell.Range = 400;
					spell.SpellID = 11884;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Stun.ToString();
					m_SpiritChampion_stun = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SpiritChampion_stun);
				}
				return m_SpiritChampion_stun;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class GhostSpiritChampionBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public GhostSpiritChampionBrain()
		{
			AggroLevel = 100;
			AggroRange = 450;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				foreach (GameNPC bone in Body.GetNPCsInRadius(5000))
				{
					if (bone != null)
					{
						if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain && bone.PackageID == "NosdodenGhostSpiritmaster" && Body.PackageID == Convert.ToString(bone.ObjectID))
							Body.Follow(bone, 100, 5000);
					}
				}
			}
			if (HasAggro)
			{
				foreach (GameNPC bone in Body.GetNPCsInRadius(5000))
				{
					if (bone != null)
					{
						if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain brain && bone.PackageID == "NosdodenGhostSpiritmaster" && Body.PackageID == Convert.ToString(bone.ObjectID))
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (target != null)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(target, 100);
							}
						}
					}
				}
			}
			base.Think();
		}
	}
}
#endregion
//////////////////////////////////////////////////////////////////Bonedancer sub pets/////////////////////////////////////////////////
#region Skeletal Commander
namespace DOL.GS
{
	public class GhostSkeletalCommander : GameNPC
	{
		public override int MaxHealth
		{
			get { return 2500; }
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15; // dmg reduction for melee dmg
				case eDamageType.Crush: return 15; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 15; // dmg reduction for melee dmg
				default: return 25; // dmg reduction for rest resists
			}
		}
        public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.35;
		}
		public override void WalkToSpawn()
		{
			if (IsAlive)
				return;
			base.WalkToSpawn();
		}
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override bool AddToWorld()
		{
			Model = 2220;
			Flags = eFlags.GHOST;
			EquipmentTemplateID = "bd_armor";
			Name = "bone commander";
			Size = 60;
			Level = 50;
			RespawnInterval = -1;
			Realm = eRealm.None;
			GhostSkeletalCommanderBrain adds = new GhostSkeletalCommanderBrain();
			SetOwnBrain(adds);
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc.IsAlive && npc.RespawnInterval == -1 && npc.Brain is SkeletalCommanderHealerBrain && npc.PackageID == PackageID)
					npc.Die(npc);
			}
			base.Die(killer);
        }
        public override void DropLoot(GameObject killer) //no loot
		{
		}
        public override long ExperienceValue => 0;
	}
}

namespace DOL.AI.Brain
{
	public class GhostSkeletalCommanderBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public GhostSkeletalCommanderBrain()
		{
			AggroLevel = 100;
			AggroRange = 450;
		}
		public override void Think()
		{
			if(!HasAggressionTable())
            {
				CanSummonBonemender=false;
				foreach(GameNPC bone in Body.GetNPCsInRadius(5000))
                {
					if(bone != null)
                    {
						if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain && bone.PackageID == "NosdodenGhostBonedancer" && Body.PackageID == Convert.ToString(bone.ObjectID))
							Body.Follow(bone, 100, 5000);
                    }
                }
            }
			if(HasAggro)
            {
				foreach (GameNPC bone in Body.GetNPCsInRadius(5000))
				{
					if (bone != null)
					{
						if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain brain && bone.PackageID == "NosdodenGhostBonedancer" && Body.PackageID == Convert.ToString(bone.ObjectID))
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (target != null)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(target, 100);
							}
						}
					}
				}
			}
			if (Body.IsAlive)
				SummonBonemender();
			base.Think();
		}
		private protected bool CanSummonBonemender = false;
		private protected void SummonBonemender()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
			{
				if (npc.IsAlive && npc.RespawnInterval == -1 && npc.Brain is SkeletalCommanderHealerBrain && npc.PackageID == Body.PackageID)
					return;
			}
			SkeletalCommanderHealer pet = new SkeletalCommanderHealer();
			pet.X = Body.X;
			pet.Y = Body.Y - 50;
			pet.Z = Body.Z;
			pet.PackageID = Body.PackageID;
			pet.Heading = Body.Heading;
			pet.Faction = Body.Faction;
			pet.CurrentRegionID = Body.CurrentRegionID;
			pet.Faction = Body.Faction;
			pet.AddToWorld();
		}
	}
}
#endregion
#region Bonemender
namespace DOL.GS
{
	public class SkeletalCommanderHealer : GameNPC
	{
		public override int MaxHealth
		{
			get { return 1500; }
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15; // dmg reduction for melee dmg
				case eDamageType.Crush: return 15; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 15; // dmg reduction for melee dmg
				default: return 25; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override void WalkToSpawn()
		{
			if (IsAlive)
				return;
			base.WalkToSpawn();
		}
		public override void StartAttack(GameObject target)
		{
			if (IsAlive)
				return;
			base.StartAttack(target);
		}	
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.35;
		}
		public override bool AddToWorld()
		{
			Model = 2220;
			Name = "bonemender";
			Size = 45;
			Level = 44;
			RespawnInterval = -1;
			Dexterity = 200;
			Flags ^= eFlags.GHOST;
			Realm = eRealm.None;
			SkeletalCommanderHealerBrain adds = new SkeletalCommanderHealerBrain();
			SetOwnBrain(adds);
			base.AddToWorld();
			return true;
		}
		public override void DropLoot(GameObject killer) //no loot
		{
		}
		public override long ExperienceValue => 0;
	}
}

namespace DOL.AI.Brain
{
	public class SkeletalCommanderHealerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public SkeletalCommanderHealerBrain()
		{
			AggroLevel = 0;
			AggroRange = 450;
		}
		public override void Think()
		{
			if(Body.IsAlive)
            {
				if (!Body.Spells.Contains(Pet_Heal))
					Body.Spells.Add(Pet_Heal);
				foreach (GameNPC commander in Body.GetNPCsInRadius(5000))
                {
					foreach (GameNPC bone in Body.GetNPCsInRadius(5000))
					{
						if (commander != null && bone != null)
						{
							if (commander.IsAlive && commander.Brain is GhostSkeletalCommanderBrain brain && commander.PackageID == Body.PackageID)
							{
								if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain brain2 && bone.PackageID== "NosdodenGhostBonedancer" && bone.ObjectID == Convert.ToInt16(Body.PackageID))
								{
									if (!Body.IsCasting && !Body.IsMoving)
									{
										foreach (Spell spells in Body.Spells)
										{
											if (spells != null)
											{
												if (Body.IsMoving && Body.IsCasting)
													Body.StopFollowing();
												else
													Body.Follow(commander, 100, 5000);

												if (Util.Chance(100))
												{
													if (commander.HealthPercent < 100)
													{
														if (Body.TargetObject != commander)
															Body.TargetObject = commander;
														Body.TurnTo(commander);
														Body.CastSpell(Pet_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
													}
													else if (Body.HealthPercent < 100)
													{
														if (Body.TargetObject != Body)
															Body.TargetObject = Body;
														Body.TurnTo(Body);
														Body.CastSpell(Pet_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
													}
													else if (bone.HealthPercent < 100)
													{
														if (Body.TargetObject != bone)
															Body.TargetObject = bone;
														Body.TurnTo(bone);
														Body.CastSpell(Pet_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
													}
												}
											}
										}
									}
								}
							}
						}
					}
                }
            }
			base.Think();
		}
		private Spell m_Pet_Heal;
		private Spell Pet_Heal
		{
			get
			{
				if (m_Pet_Heal == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 3058;
					spell.Icon = 3058;
					spell.TooltipId = 3058;
					spell.Value = 250;
					spell.Name = "Heal";
					spell.Range = 1500;
					spell.SpellID = 11883;
					spell.Target = "Realm";
					spell.Type = "Heal";
					m_Pet_Heal = new Spell(spell, 70);
					spell.Uninterruptible = true;
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Pet_Heal);
				}
				return m_Pet_Heal;
			}
		}
	}
}
#endregion