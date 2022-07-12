using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;
using DOL.GS.ServerProperties;
using DOL.GS.PacketHandler;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
	public class AlbGolestandt : GameEpicBoss
	{
		protected String[] m_deathAnnounce;
		public AlbGolestandt() : base()
		{
			m_deathAnnounce = new String[] { "The earth lurches beneath your feet as {0} staggers and topples to the ground.",
				"A glowing light begins to form on the mound that served as {0}'s lair." };
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Golestandt Initializing...");
		}
		#region Custom Methods
		public static ushort LairRadius
		{
			get { return 2000; }
		}
		/// <summary>
		/// Create dragon's lair after it was loaded from the DB.
		/// </summary>
		/// <param name="obj"></param>
		public override void LoadFromDatabase(DataObject obj)
		{
			base.LoadFromDatabase(obj);
			String[] dragonName = Name.Split(new char[] { ' ' });
			WorldMgr.GetRegion(CurrentRegionID).AddArea(new Area.Circle(String.Format("{0}'s Lair",
				dragonName[0]),
				X, Y, 0, LairRadius + 200));
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (!IsWithinRadius(spawnPoint,LairRadius))//dragon take 0 dmg is it's out of his lair
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
						damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit ||
						damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GamePet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System,
								eChatLoc.CL_ChatWindow);
						base.TakeDamage(source, damageType, 0, 0);
						return;
					}
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		/// <summary>
		/// Post a message in the server news and award a dragon kill point for
		/// every XP gainer in the raid.
		/// </summary>
		/// <param name="killer">The living that got the killing blow.</param>
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
				log.Error("Dragon Killed: killer is null!");
			else
				log.Debug("Dragon Killed: killer is " + killer.Name + ", attackers:");
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
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
			}
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
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
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 300000; }
		}
		public override void WalkToSpawn(short speed)
		{
			speed = 400;
			if (AlbGolestandtBrain.IsRestless)
				return;
			else
				base.WalkToSpawn(speed);
		}
		public override void StartAttack(GameObject target)
		{
			if (AlbGolestandtBrain.IsRestless)
				return;
			else
				base.StartAttack(target);
		}
		private static Point3D spawnPoint = new Point3D(391344, 755419, 395);
		public override ushort SpawnHeading { get => base.SpawnHeading; set => base.SpawnHeading = 2071; }
		public override Point3D SpawnPoint { get => spawnPoint; set => base.SpawnPoint = spawnPoint; }
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157497);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			#region All bools here
			AlbGolestandtBrain.pathpoint1 = false;
			AlbGolestandtBrain.pathpoint2 = false;
			AlbGolestandtBrain.pathpoint3 = false;
			AlbGolestandtBrain.pathpoint4 = false;
			AlbGolestandtBrain.pathpoint5 = false;
			AlbGolestandtBrain.pathpoint6 = false;
			AlbGolestandtBrain.pathpoint7 = false;
			AlbGolestandtBrain.pathpoint8 = false;
			AlbGolestandtBrain.pathpoint9 = false;
			AlbGolestandtBrain.pathpoint10 = false;
			AlbGolestandtBrain.pathpoint11 = false;
			AlbGolestandtBrain.pathpoint12 = false;
			AlbGolestandtBrain.pathpoint13 = false;
			AlbGolestandtBrain.pathpoint14 = false;
			AlbGolestandtBrain.pathpoint15 = false;
			AlbGolestandtBrain.pathpoint16 = false;
			AlbGolestandtBrain.pathpoint17 = false;
			AlbGolestandtBrain.pathpoint18 = false;
			AlbGolestandtBrain.pathpoint19 = false;
			AlbGolestandtBrain.pathpoint20 = false;
			AlbGolestandtBrain.pathpoint21 = false;
			AlbGolestandtBrain.pathpoint22 = false;
			AlbGolestandtBrain.pathpoint23 = false;
			AlbGolestandtBrain.pathpoint24 = false;
			AlbGolestandtBrain.pathpoint25 = false;
			AlbGolestandtBrain.pathpoint26 = false;
			AlbGolestandtBrain.pathpoint27 = false;
			AlbGolestandtBrain.pathpoint28 = false;
			AlbGolestandtBrain.ResetChecks = false;
			AlbGolestandtBrain.IsRestless = false;
			AlbGolestandtBrain.LockIsRestless = false;
			AlbGolestandtBrain.CanSpawnMessengers = false;
			AlbGolestandtBrain.checkForMessangers = false;
			AlbGolestandtBrain.LockIsRestless = false;
			AlbGolestandtBrain.CanGlare = false;
			AlbGolestandtBrain.CanGlare2 = false;
			AlbGolestandtBrain.RandomTarget = null;
			AlbGolestandtBrain.RandomTarget2 = null;
			AlbGolestandtBrain.CanStun = false;
			AlbGolestandtBrain.CanThrow = false;
			AlbGolestandtBrain.DragonKaboom1 = false;
			AlbGolestandtBrain.DragonKaboom2 = false;
			AlbGolestandtBrain.DragonKaboom3 = false;
			AlbGolestandtBrain.DragonKaboom4 = false;
			AlbGolestandtBrain.DragonKaboom5 = false;
			AlbGolestandtBrain.DragonKaboom6 = false;
			AlbGolestandtBrain.DragonKaboom7 = false;
			AlbGolestandtBrain.DragonKaboom8 = false;
			AlbGolestandtBrain.DragonKaboom9 = false;
			#endregion
			MeleeDamageType = eDamageType.Crush;
			Faction = FactionMgr.GetFactionByID(31);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(31));
			AlbGolestandtBrain sbrain = new AlbGolestandtBrain();
			SetOwnBrain(sbrain);
			sbrain.Start();
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void EnemyKilled(GameLiving enemy)
		{
			if (enemy != null && enemy is GamePlayer)
			{
				GamePlayer player = (GamePlayer)enemy;
				foreach(GameClient client in WorldMgr.GetClientsOfZone(CurrentZone.ID))
                {
					if (client == null) break;
					if (client.Player == null) continue;
					if (client.IsPlaying)
                    {
						client.Out.SendMessage(Name + " roars in triumph as another " + player.CharacterClass.Name + " falls before his might.", eChatType.CT_Say, eChatLoc.CL_ChatWindow);
					}
				}				
			}
			base.EnemyKilled(enemy);
		}
		public override bool IsVisibleToPlayers => true;//this make dragon think all the time, no matter if player is around or not
	}
}
namespace DOL.AI.Brain
{
	public class AlbGolestandtBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public AlbGolestandtBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 5000;
		}
		public static bool CanGlare = false;
		public static bool CanGlare2 = false;
		public static bool CanStun = false;
		public static bool CanThrow = false;
		public static bool CanSpawnMessengers = false;
		public static bool ResetChecks = false;
		public static bool LockIsRestless = false;
		public static bool LockEndRoute = false;
		public static bool checkForMessangers = false;
		public static List<GameNPC> DragonAdds = new List<GameNPC>();

		public static bool m_isrestless = false;
		public static bool IsRestless
		{
			get { return m_isrestless; }
			set { m_isrestless = value; }
		}
		public override void Think()
		{
			Point3D spawn = new Point3D(391326, 755351, 388);
			if (!HasAggressionTable())
			{
				Body.Health = Body.MaxHealth;
				#region !IsRestless
				if (!IsRestless)
				{
					DragonKaboom1 = false;
					DragonKaboom2 = false;
					DragonKaboom3 = false;
					DragonKaboom4 = false;
					DragonKaboom5 = false;
					DragonKaboom6 = false;
					DragonKaboom7 = false;
					DragonKaboom8 = false;
					DragonKaboom9 = false;
					CanThrow = false;
					CanGlare = false;
					CanStun = false;
					RandomTarget = null;
					if (Glare_Enemys.Count > 0)//clear glare enemys
						Glare_Enemys.Clear();

					if (Port_Enemys.Count > 0)//clear port players
						Port_Enemys.Clear();
					if (randomlyPickedPlayers.Count > 0)//clear randomly picked players
						randomlyPickedPlayers.Clear();

					var prepareGlare = Body.TempProperties.getProperty<ECSGameTimer>("golestandt_glare");
					if (prepareGlare != null)
					{
						prepareGlare.Stop();
						Body.TempProperties.removeProperty("golestandt_glare");
					}
					var prepareStun = Body.TempProperties.getProperty<ECSGameTimer>("golestandt_stun");
					if (prepareStun != null)
					{
						prepareStun.Stop();
						Body.TempProperties.removeProperty("golestandt_stun");
					}
					var throwPlayer = Body.TempProperties.getProperty<ECSGameTimer>("golestandt_throw");
					if (throwPlayer != null)
					{
						throwPlayer.Stop();
						Body.TempProperties.removeProperty("golestandt_throw");
					}
					var spawnMessengers = Body.TempProperties.getProperty<ECSGameTimer>("golestandt_messengers");
					if (spawnMessengers != null)
					{
						spawnMessengers.Stop();
						CanSpawnMessengers = false;
						Body.TempProperties.removeProperty("golestandt_messengers");
					}
				}
                #endregion
				if (!checkForMessangers)
				{
					if (DragonAdds.Count > 0)
					{
						foreach (GameNPC messenger in DragonAdds)
						{
							if (messenger != null && messenger.IsAlive && messenger.Brain is GolestandtMessengerBrain)
								messenger.RemoveFromWorld();
						}
						foreach (GameNPC granitegiant in DragonAdds)
						{
							if (granitegiant != null && granitegiant.IsAlive && granitegiant.Brain is GolestandtSpawnedAdBrain)
								granitegiant.RemoveFromWorld();
						}
						DragonAdds.Clear();
					}
					checkForMessangers = true;
				}
			}

			#region Dragon IsRestless fly route activation
			if (Body.CurrentRegion.IsPM && Body.CurrentRegion.IsNightTime == false && !LockIsRestless)//Dragon will start roam
			{
				if (Glare_Enemys.Count > 0)
					Glare_Enemys.Clear();

				if (HasAggro)//if got aggro clear it
				{
					if (Body.attackComponent.AttackState && Body.IsCasting)//make sure it stop all actions
						Body.attackComponent.NPCStopAttack();
					ClearAggroList();
				}
				IsRestless = true;//start roam
				LockEndRoute = false;
				foreach (GameClient client in WorldMgr.GetClientsOfZone(Body.CurrentZone.ID))//from current zone
				{
					if (client == null) break;
					if (client.Player == null) continue;
					if (client.IsPlaying)
					{
						client.Out.SendSoundEffect(2467, 0, 0, 0, 0, 0);//play sound effect for every player in boss currentregion
						client.Out.SendMessage("A voice explodes across the land. You hear a roar in the distance, 'I will grind your bones and shred your flesh.'"
							, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
					}
				}
				Body.Flags = GameNPC.eFlags.FLYING;//make dragon fly mode
				ResetChecks = false;//reset it so can reset bools at end of path
				LockIsRestless = true;
			}

			if (IsRestless)
				DragonFlyingPath();//make dragon follow the path

			if (!ResetChecks && pathpoint28)
			{
				IsRestless = false;//can roam again
				Body.WalkToSpawn();//move dragon to spawn so he can attack again
				Body.Flags = 0; //remove all flags
				#region All bools pathpoints here
				pathpoint1 = false;
				pathpoint2 = false;
				pathpoint3 = false;
				pathpoint4 = false;
				pathpoint5 = false;
				pathpoint6 = false;
				pathpoint7 = false;
				pathpoint8 = false;
				pathpoint9 = false;
				pathpoint10 = false;
				pathpoint11 = false;
				pathpoint12 = false;
				pathpoint13 = false;
				pathpoint14 = false;
				pathpoint15 = false;
				pathpoint16 = false;
				pathpoint17 = false;
				pathpoint18 = false;
				pathpoint19 = false;
				pathpoint20 = false;
				pathpoint21 = false;
				pathpoint22 = false;
				pathpoint23 = false;
				pathpoint24 = false;
				pathpoint25 = false;
				pathpoint26 = false;
				pathpoint27 = false;
				pathpoint28 = false;
				#endregion
				ResetChecks = true;//do it only once
			}
			if (Body.CurrentRegion.IsNightTime == true && !LockEndRoute)//reset bools to dragon can roam again
			{
				LockIsRestless = false; //roam 2nd check		
				LockEndRoute = true;
			}
			if (IsRestless)//special glare phase, during dragon roam it will cast glare like a mad
			{
				if (!CanGlare2 && !Body.IsCasting)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PrepareGlareRoam), Util.Random(5000, 8000));//Glare at target every 5-10s
					CanGlare2 = true;
				}
			}
			#endregion
			if (HasAggro && Body.TargetObject != null)
			{
				checkForMessangers = false;
				DragonBreath();//Method that handle dragon kabooom breaths
				if (CanThrow == false && !IsRestless)
				{
					ECSGameTimer throwPlayer = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ThrowPlayer), Util.Random(60000, 80000));//Teleport 2-5 Players every 60-80s
					Body.TempProperties.setProperty("golestandt_throw", throwPlayer);
					CanThrow = true;
				}
				if (CanGlare == false && !Body.IsCasting && !IsRestless)
				{
					ECSGameTimer prepareGlare = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PrepareGlare), Util.Random(40000, 60000));//Glare at target every 40-60s
					Body.TempProperties.setProperty("golestandt_glare", prepareGlare);
					CanGlare = true;
				}
				if (CanStun == false && !Body.IsCasting && !IsRestless)
				{
					ECSGameTimer prepareStun = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PrepareStun), Util.Random(120000, 180000));//prepare Stun every 120s-180s
					Body.TempProperties.setProperty("golestandt_stun", prepareStun);
					CanStun = true;
				}
				if (Body.HealthPercent <= 50 && CanSpawnMessengers == false && !IsRestless)
				{
					ECSGameTimer spawnMessengers = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnMssengers), Util.Random(80000, 90000));//spawn messengers at 50% hp every 80/90s
					Body.TempProperties.setProperty("golestandt_messengers", spawnMessengers);
					CanSpawnMessengers = true;
				}
			}
			base.Think();
		}
		#region Dragon Roaming Path
		#region PathPoints checks
		public static bool pathpoint1 = false;
		public static bool pathpoint2 = false;
		public static bool pathpoint3 = false;
		public static bool pathpoint4 = false;
		public static bool pathpoint5 = false;
		public static bool pathpoint6 = false;
		public static bool pathpoint7 = false;
		public static bool pathpoint8 = false;
		public static bool pathpoint9 = false;
		public static bool pathpoint10 = false;
		public static bool pathpoint11 = false;
		public static bool pathpoint12 = false;
		public static bool pathpoint13 = false;
		public static bool pathpoint14 = false;
		public static bool pathpoint15 = false;
		public static bool pathpoint16 = false;
		public static bool pathpoint17 = false;
		public static bool pathpoint18 = false;
		public static bool pathpoint19 = false;
		public static bool pathpoint20 = false;
		public static bool pathpoint21 = false;
		public static bool pathpoint22 = false;
		public static bool pathpoint23 = false;
		public static bool pathpoint24 = false;
		public static bool pathpoint25 = false;
		public static bool pathpoint26 = false;
		public static bool pathpoint27 = false;
		public static bool pathpoint28 = false;
		#endregion
		private void DragonFlyingPath()
		{
			#region Route PathPoints
			Point3D point1 = new Point3D(385865, 756961, 3504);
			Point3D point2 = new Point3D(378547, 755862, 3504);
			Point3D point3 = new Point3D(373114, 749008, 3504);
			Point3D point4 = new Point3D(365764, 745172, 3504);
			Point3D point5 = new Point3D(365007, 734622, 3504);
			Point3D point6 = new Point3D(366398, 727898, 3504);
			Point3D point7 = new Point3D(364666, 722970, 3504);
			Point3D point8 = new Point3D(365500, 718003, 3504);
			Point3D point9 = new Point3D(362982, 714084, 3504);
			Point3D point10 = new Point3D(363536, 706078, 3504);
			Point3D point11 = new Point3D(374879, 705288, 3504);
			Point3D point12 = new Point3D(382939, 704836, 4649);
			Point3D point13 = new Point3D(388354, 708784, 4649);
			Point3D point14 = new Point3D(392940, 712391, 3723);
			Point3D point15 = new Point3D(395754, 717498, 3507);
			Point3D point16 = new Point3D(395476, 722965, 3507);
			Point3D point17 = new Point3D(394829, 726232, 3507);
			Point3D point18 = new Point3D(393783, 743566, 3512);
			Point3D point19 = new Point3D(381718, 739900, 3512);
			Point3D point20 = new Point3D(371903, 718204, 3512);
			Point3D point21 = new Point3D(380357, 716827, 3512);
			Point3D point22 = new Point3D(388960, 725072, 3512);
			Point3D point23 = new Point3D(394914, 726548, 3512);
			Point3D point24 = new Point3D(397830, 713380, 3512);
			Point3D point25 = new Point3D(407425, 720655, 3512);
			Point3D point26 = new Point3D(408918, 742335, 3512);
			Point3D point27 = new Point3D(397944, 754701, 3512);
			Point3D point28 = new Point3D(391129, 755603, 378);//spawn
			#endregion
			if (IsRestless && Body.IsAlive)
			{
				Body.MaxSpeedBase = 400;
				short speed = 350;
				#region WalkToPoints
				if (!Body.IsWithinRadius(point1, 30) && !pathpoint1)
					Body.WalkTo(point1, speed);
				else
				{
					pathpoint1 = true;
					if (!Body.IsWithinRadius(point2, 30) && pathpoint1 && !pathpoint2)
						Body.WalkTo(point2, speed);
					else
					{
						pathpoint2 = true;
						if (!Body.IsWithinRadius(point3, 30) && pathpoint1 && pathpoint2 && !pathpoint3)
							Body.WalkTo(point3, speed);
						else
						{
							pathpoint3 = true;
							if (!Body.IsWithinRadius(point4, 30) && pathpoint1 && pathpoint2 && pathpoint3 && !pathpoint4)
								Body.WalkTo(point4, speed);
							else
							{
								pathpoint4 = true;
								if (!Body.IsWithinRadius(point5, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && !pathpoint5)
									Body.WalkTo(point5, speed);
								else
								{
									pathpoint5 = true;
									if (!Body.IsWithinRadius(point6, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && !pathpoint6)
										Body.WalkTo(point6, speed);
									else
									{
										pathpoint6 = true;
										if (!Body.IsWithinRadius(point7, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && !pathpoint7)
											Body.WalkTo(point7, speed);
										else
										{
											pathpoint7 = true;
											if (!Body.IsWithinRadius(point8, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
												&& !pathpoint8)
												Body.WalkTo(point8, speed);
											else
											{
												pathpoint8 = true;
												if (!Body.IsWithinRadius(point9, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
													&& pathpoint8 && !pathpoint9)
													Body.WalkTo(point9, speed);
												else
												{
													pathpoint9 = true;
													if (!Body.IsWithinRadius(point10, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
														&& pathpoint8 && pathpoint9 && !pathpoint10)
														Body.WalkTo(point10, speed);
													else
													{
														pathpoint10 = true;
														if (!Body.IsWithinRadius(point11, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
															&& pathpoint8 && pathpoint9 && pathpoint10 && !pathpoint11)
															Body.WalkTo(point11, speed);
														else
														{
															pathpoint11 = true;
															if (!Body.IsWithinRadius(point12, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && !pathpoint12)
																Body.WalkTo(point12, speed);
															else
															{
																pathpoint12 = true;
																if (!Body.IsWithinRadius(point13, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																	&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && !pathpoint13)
																	Body.WalkTo(point13, speed);
																else
																{
																	pathpoint13 = true;
																	if (!Body.IsWithinRadius(point14, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																		&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && !pathpoint14)
																		Body.WalkTo(point14, speed);
																	else
																	{
																		pathpoint14 = true;
																		if (!Body.IsWithinRadius(point15, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																			&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && !pathpoint15)
																			Body.WalkTo(point15, speed);
																		else
																		{

																			pathpoint15 = true;
																			if (!Body.IsWithinRadius(point16, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																				&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && !pathpoint16)
																				Body.WalkTo(point16, speed);
																			else
																			{
																				pathpoint16 = true;
																				if (!Body.IsWithinRadius(point17, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																					&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																					&& !pathpoint17)
																					Body.WalkTo(point17, speed);
																				else
																				{
																					pathpoint17 = true;
																					if (!Body.IsWithinRadius(point18, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																						&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																						&& pathpoint17 && !pathpoint18)
																						Body.WalkTo(point18, speed);
																					else
																					{
																						pathpoint18 = true;
																						if (!Body.IsWithinRadius(point19, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																							&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																							&& pathpoint17 && pathpoint18 && !pathpoint19)
																							Body.WalkTo(point19, speed);
																						else
																						{
																							pathpoint19 = true;
																							if (!Body.IsWithinRadius(point20, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																								&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																								&& pathpoint17 && pathpoint18 && pathpoint19 && !pathpoint20)
																								Body.WalkTo(point20, speed);
																							else
																							{
																								pathpoint20 = true;
																								if (!Body.IsWithinRadius(point21, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																									&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																									&& pathpoint17 && pathpoint18 && pathpoint19 && pathpoint20 && !pathpoint21)
																									Body.WalkTo(point21, speed);
																								else
																								{
																									pathpoint21 = true;
																									if (!Body.IsWithinRadius(point22, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																										&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																										&& pathpoint17 && pathpoint18 && pathpoint19 && pathpoint20 && pathpoint21 && !pathpoint22)
																										Body.WalkTo(point22, speed);
																									else
																									{
																										pathpoint22 = true;
																										if (!Body.IsWithinRadius(point23, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																											&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																											&& pathpoint17 && pathpoint18 && pathpoint19 && pathpoint20 && pathpoint21 && pathpoint22 && !pathpoint23)
																											Body.WalkTo(point23, speed);
																										else
																										{
																											pathpoint23 = true;
																											if (!Body.IsWithinRadius(point24, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																												&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																												&& pathpoint17 && pathpoint18 && pathpoint19 && pathpoint20 && pathpoint21 && pathpoint22 && pathpoint23 && !pathpoint24)
																												Body.WalkTo(point24, speed);
																											else
																											{
																												pathpoint24 = true;
																												if (!Body.IsWithinRadius(point25, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																													&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																													&& pathpoint17 && pathpoint18 && pathpoint19 && pathpoint20 && pathpoint21 && pathpoint22 && pathpoint23 && pathpoint24 && !pathpoint25)
																													Body.WalkTo(point25, speed);
																												else
																												{
																													pathpoint25 = true;
																													if (!Body.IsWithinRadius(point26, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																														&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																														&& pathpoint17 && pathpoint18 && pathpoint19 && pathpoint20 && pathpoint21 && pathpoint22 && pathpoint23 && pathpoint24 && pathpoint25
																														&& !pathpoint26)
																														Body.WalkTo(point26, speed);
																													else
																													{
																														pathpoint26 = true;
																														if (!Body.IsWithinRadius(point27, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																															&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																															&& pathpoint17 && pathpoint18 && pathpoint19 && pathpoint20 && pathpoint21 && pathpoint22 && pathpoint23 && pathpoint24 && pathpoint25
																															&& pathpoint26 && !pathpoint27)
																															Body.WalkTo(point27, speed);
																														else
																														{
																															pathpoint27 = true;
																															if (!Body.IsWithinRadius(point28, 30) && pathpoint1 && pathpoint2 && pathpoint3 && pathpoint4 && pathpoint5 && pathpoint6 && pathpoint7
																																&& pathpoint8 && pathpoint9 && pathpoint10 && pathpoint11 && pathpoint12 && pathpoint13 && pathpoint14 && pathpoint15 && pathpoint16
																																&& pathpoint17 && pathpoint18 && pathpoint19 && pathpoint20 && pathpoint21 && pathpoint22 && pathpoint23 && pathpoint24 && pathpoint25
																																&& pathpoint26 && pathpoint27 && !pathpoint28)
																																Body.WalkTo(point28, speed);
																															else
																															{
																																pathpoint28 = true;
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
			}
		}
		#endregion

		#region Throw Players
		List<GamePlayer> Port_Enemys = new List<GamePlayer>();
		List<GamePlayer> randomlyPickedPlayers = new List<GamePlayer>();
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		public static List<t> GetRandomElements<t>(IEnumerable<t> list, int elementsCount)//pick X elements from list
		{
			return list.OrderBy(x => Guid.NewGuid()).Take(elementsCount).ToList();
		}
		private int ThrowPlayer(ECSGameTimer timer)
		{
			if (Body.IsAlive && HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1)
						{
							if (!Port_Enemys.Contains(player))
							{
								if (player != Body.TargetObject)//dont throw main target
									Port_Enemys.Add(player);
							}
						}
					}
				}
				if (Port_Enemys.Count > 0)
				{
					randomlyPickedPlayers = GetRandomElements(Port_Enemys, Util.Random(2, 5));//pick 2-5players from list to new list

					if (randomlyPickedPlayers.Count > 0)
					{
						foreach (GamePlayer player in randomlyPickedPlayers)
						{
							if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && HasAggro && player.IsWithinRadius(Body, 2000))
							{
								player.Out.SendMessage(Body.Name + " begins flapping his wings violently. You struggle to hold your footing on the ground!", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
								switch (Util.Random(1, 5))
								{
									case 1: player.MoveTo(Body.CurrentRegionID, 391348, 755751, 1815, 2069); break;//lair spawn point
									case 2: player.MoveTo(Body.CurrentRegionID, 398605, 754458, 1404, 1042); break;
									case 3: player.MoveTo(Body.CurrentRegionID, 392450, 743176, 1404, 4063); break;
									case 4: player.MoveTo(Body.CurrentRegionID, 383669, 758112, 1847, 3003); break;
									case 5: player.MoveTo(Body.CurrentRegionID, 401432, 755310, 1728, 1065); break;
								}
							}
						}
						randomlyPickedPlayers.Clear();//clear list after port
					}
				}
				CanThrow = false;// set to false, so can throw again
			}
			return 0;
		}
		#endregion

		#region Glare Standard
		List<string> glare_text = new List<string>()
		{
			"{0} shouts, 'Foolish {1}! Your flesh will make a splendid meal.'",
			"{0} shouts, 'Perhaps your dark ages would end if {1}s like you continue to be weeded out!'",
			"{0} shouts, 'Meddle not in the affairs of dragons, {1}! Yes, you are indeed crunchy.'",
		};
		List<GamePlayer> Glare_Enemys = new List<GamePlayer>();
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		private int PrepareGlare(ECSGameTimer timer)
		{
			if (!IsRestless && HasAggro && Body.IsAlive)
			{
				ushort DragonRange = 2500;
				foreach (GamePlayer player in Body.GetPlayersInRadius(DragonRange))
				{
					if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Glare_Enemys.Contains(player))
							Glare_Enemys.Add(player);
					}
				}
				if (Glare_Enemys.Count > 0)
				{
					GamePlayer Target = Glare_Enemys[Util.Random(0, Glare_Enemys.Count - 1)];
					RandomTarget = Target;
					if (RandomTarget != null && RandomTarget.IsAlive && RandomTarget.IsWithinRadius(Body, Dragon_DD.Range))
					{
						BroadcastMessage(String.Format("{0} stares at {1} and prepares a massive attack.", Body.Name, RandomTarget.Name));
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastGlare), 6000);
					}
					else
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetGlare), 2000);
				}
				else
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetGlare), 2000);
			}
			return 0;
		}
		private int CastGlare(ECSGameTimer timer)
		{
			if (!IsRestless && HasAggro && Body.IsAlive && RandomTarget != null && RandomTarget.IsAlive && RandomTarget.IsWithinRadius(Body, Dragon_DD.Range) && !Body.IsCasting)
			{
				Body.TargetObject = RandomTarget;
				Body.TurnTo(RandomTarget);
				Body.CastSpell(Dragon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				string glaretext = glare_text[Util.Random(0, glare_text.Count - 1)];
				RandomTarget.Out.SendMessage(String.Format(glaretext, Body.Name, RandomTarget.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
			}
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetGlare), 2000);
			return 0;
		}
		private int ResetGlare(ECSGameTimer timer)
		{
			if (Glare_Enemys.Count > 0)
				Glare_Enemys.Clear();

			RandomTarget = null;
			CanGlare = false;
			return 0;
		}
		#endregion

		#region Glare Roam
		List<string> glareroam_text = new List<string>()
		{
			"{0} shouts, 'Foolish {1}! Your flesh will make a splendid meal.'",
			"{0} shouts, 'Perhaps your dark ages would end if {1}s like you continue to be weeded out!'",
			"{0} shouts, 'Meddle not in the affairs of dragons, {1}! Yes, you are indeed crunchy.'",
		};
		List<GamePlayer> GlareRoam_Enemys = new List<GamePlayer>();
		public static GamePlayer randomtarget2 = null;
		public static GamePlayer RandomTarget2
		{
			get { return randomtarget2; }
			set { randomtarget2 = value; }
		}
		private int PrepareGlareRoam(ECSGameTimer timer)
		{
			if (IsRestless && Body.IsAlive)
			{
				ushort DragonRange = 5000;
				foreach (GamePlayer player in Body.GetPlayersInRadius(DragonRange))
				{
					if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!GlareRoam_Enemys.Contains(player))
							GlareRoam_Enemys.Add(player);
						if (!AggroTable.ContainsKey(player))
							AggroTable.Add(player, 100);
					}
				}
				if (GlareRoam_Enemys.Count > 0)
				{
					GamePlayer Target = GlareRoam_Enemys[Util.Random(0, GlareRoam_Enemys.Count - 1)];
					RandomTarget2 = Target;
					if (RandomTarget2 != null && RandomTarget2.IsAlive && RandomTarget2.IsWithinRadius(Body, Dragon_DD2.Range))
					{
						foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
						{
							if (player != null)
								player.Out.SendMessage(String.Format("{0} stares at {1} and prepares a massive attack.", Body.Name, RandomTarget2.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
						}
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastGlareRoam), 3000);
					}
					else
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetGlareRoam), 2000);
				}
				else
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetGlareRoam), 2000);
			}
			return 0;
		}
		private int CastGlareRoam(ECSGameTimer timer)
		{
			if (IsRestless && Body.IsAlive && RandomTarget2 != null && RandomTarget2.IsAlive && RandomTarget2.IsWithinRadius(Body, Dragon_DD2.Range) && !Body.IsCasting)
			{
				Body.TargetObject = RandomTarget2;
				Body.TurnTo(RandomTarget2);
				Body.CastSpell(Dragon_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);//special roaming glare
				string glaretextroam = glareroam_text[Util.Random(0, glareroam_text.Count - 1)];
				RandomTarget2.Out.SendMessage(String.Format(glaretextroam,Body.Name,RandomTarget2.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
			}
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetGlareRoam), 2000);
			return 0;
		}
		private int ResetGlareRoam(ECSGameTimer timer)
		{
			if (GlareRoam_Enemys.Count > 0)
				GlareRoam_Enemys.Clear();

			if (IsRestless)
			{
				ClearAggroList();
			}
			RandomTarget2 = null;
			CanGlare2 = false;
			return 0;
		}
		#endregion

		#region Stun
		private int PrepareStun(ECSGameTimer timer)
		{
			if (!IsRestless && HasAggro && Body.IsAlive)
			{
				BroadcastMessage(String.Format("{0} looks mindfully around.", Body.Name));
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastStun), 6000);
			}
			return 0;
		}
		private int CastStun(ECSGameTimer timer)
		{
			if (!IsRestless && HasAggro && Body.IsAlive)
				Body.CastSpell(Dragon_Stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			CanStun = false;
			return 0;
		}
		#endregion

		#region Dragon Breath Big Bang
		public static bool DragonKaboom1 = false;
		public static bool DragonKaboom2 = false;
		public static bool DragonKaboom3 = false;
		public static bool DragonKaboom4 = false;
		public static bool DragonKaboom5 = false;
		public static bool DragonKaboom6 = false;
		public static bool DragonKaboom7 = false;
		public static bool DragonKaboom8 = false;
		public static bool DragonKaboom9 = false;

		List<string> breath_text = new List<string>()
		{
				"You feel a rush of air flow past you as {0} inhales deeply!",
				"{0} takes another powerful breath as he prepares to unleash a raging inferno upon you!",
				"{0} bellows in rage and glares at all of the creatures attacking him.",
				"{0} noticeably winces from his wounds as he attempts to prepare for yet another life-threatening attack!"
		};

		private void DragonBreath()
		{
			string message = breath_text[Util.Random(0, breath_text.Count - 1)];
			if (Body.HealthPercent <= 90 && DragonKaboom1 == false && !Body.IsCasting && !IsRestless)
			{
				BroadcastMessage(String.Format(message, Body.Name));
				Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DragonCastDebuff), 5000);
				DragonKaboom1 = true;
			}
			if (Body.HealthPercent <= 80 && DragonKaboom2 == false && !Body.IsCasting && !IsRestless)
			{
				BroadcastMessage(String.Format(message, Body.Name));
				Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DragonCastDebuff), 5000);
				DragonKaboom2 = true;
			}
			if (Body.HealthPercent <= 70 && DragonKaboom3 == false && !Body.IsCasting && !IsRestless)
			{
				BroadcastMessage(String.Format(message, Body.Name));
				Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DragonCastDebuff), 5000);
				DragonKaboom3 = true;
			}
			if (Body.HealthPercent <= 60 && DragonKaboom4 == false && !Body.IsCasting && !IsRestless)
			{
				BroadcastMessage(String.Format(message, Body.Name));
				Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DragonCastDebuff), 5000);
				DragonKaboom4 = true;
			}
			if (Body.HealthPercent <= 50 && DragonKaboom5 == false && !Body.IsCasting && !IsRestless)
			{
				BroadcastMessage(String.Format(message, Body.Name));
				Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DragonCastDebuff), 5000);
				DragonKaboom5 = true;
			}
			if (Body.HealthPercent <= 40 && DragonKaboom6 == false && !Body.IsCasting && !IsRestless)
			{
				BroadcastMessage(String.Format(message, Body.Name));
				Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DragonCastDebuff), 5000);
				DragonKaboom6 = true;
			}
			if (Body.HealthPercent <= 30 && DragonKaboom7 == false && !Body.IsCasting && !IsRestless)
			{
				BroadcastMessage(String.Format(message, Body.Name));
				Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DragonCastDebuff), 5000);
				DragonKaboom7 = true;
			}
			if (Body.HealthPercent <= 20 && DragonKaboom8 == false && !Body.IsCasting && !IsRestless)
			{
				BroadcastMessage(String.Format(message, Body.Name));
				Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DragonCastDebuff), 5000);
				DragonKaboom8 = true;
			}
			if (Body.HealthPercent <= 10 && DragonKaboom9 == false && !Body.IsCasting && !IsRestless)
			{
				BroadcastMessage(String.Format(message, Body.Name));
				Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DragonCastDebuff), 5000);
				DragonKaboom9 = true;
			}
		}
		private int DragonCastDebuff(ECSGameTimer timer)
		{
			Body.CastSpell(Dragon_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			return 0;
		}
		#endregion

		#region Messengers
		private int SpawnMssengers(ECSGameTimer timer)
		{
			for (int i = 0; i <= Util.Random(3, 5); i++)
			{
				GolestandtMessenger messenger = new GolestandtMessenger();
				messenger.X = 391345 + Util.Random(-100, 100);
				messenger.Y = 755661 + Util.Random(-100, 100);
				messenger.Z = 410;
				messenger.Heading = Body.Heading;
				messenger.CurrentRegion = Body.CurrentRegion;
				messenger.AddToWorld();
			}
			CanSpawnMessengers = false;
			return 0;
		}
		#endregion

		#region Spells
		private Spell m_Dragon_DD2;
		private Spell Dragon_DD2
		{
			get
			{
				if (m_Dragon_DD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5700;
					spell.Icon = 5700;
					spell.TooltipId = 5700;
					spell.Damage = 2000;
					spell.Name = "Golestandt's Glare";
					spell.Range = 5000;//very long range cause dragon is flying and got big aggro
					spell.Radius = 1000;
					spell.SpellID = 11955;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Dragon_DD2 = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_DD2);
				}
				return m_Dragon_DD2;
			}
		}
		private Spell m_Dragon_DD;
		private Spell Dragon_DD
		{
			get
			{
				if (m_Dragon_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5700;
					spell.Icon = 5700;
					spell.TooltipId = 5700;
					spell.Damage = 1500;
					spell.Name = "Golestandt's Glare";
					spell.Range = 1500;
					spell.Radius = 1000;
					spell.SpellID = 11956;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Dragon_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_DD);
				}
				return m_Dragon_DD;
			}
		}
		private Spell m_Dragon_PBAOE;
		private Spell Dragon_PBAOE
		{
			get
			{
				if (m_Dragon_PBAOE == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 6;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5700;
					spell.Icon = 5700;
					spell.TooltipId = 5700;
					spell.Damage = 2400;
					spell.Name = "Golestandt's Breath";
					spell.Range = 0;
					spell.Radius = 2000;
					spell.SpellID = 11957;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Dragon_PBAOE = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_PBAOE);
				}
				return m_Dragon_PBAOE;
			}
		}
		private Spell m_Dragon_Stun;
		private Spell Dragon_Stun
		{
			get
			{
				if (m_Dragon_Stun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5703;
					spell.Icon = 5703;
					spell.TooltipId = 5703;
					spell.Duration = 30;
					spell.Name = "Dragon's Stun";
					spell.Range = 0;
					spell.Radius = 2000;
					spell.SpellID = 11958;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Stun.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Body;
					m_Dragon_Stun = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_Stun);
				}
				return m_Dragon_Stun;
			}
		}
		private Spell m_Dragon_Debuff;
		private Spell Dragon_Debuff
		{
			get
			{
				if (m_Dragon_Debuff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 777;
					spell.Icon = 5700;
					spell.TooltipId = 5700;
					spell.Duration = 120;
					spell.Value = 50;
					spell.Name = "Dragon's Breath";
					spell.Description = "Decreases a target's given resistance to Heat magic by 50%";
					spell.Range = 0;
					spell.Radius = 2000;
					spell.SpellID = 11965;
					spell.Target = "Enemy";
					spell.Type = eSpellType.HeatResistDebuff.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Dragon_Debuff = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_Debuff);
				}
				return m_Dragon_Debuff;
			}
		}
		#endregion
	}
}
#region Golestandt's messengers
namespace DOL.GS
{
	public class GolestandtMessenger : GameNPC
	{
		public override int MaxHealth
		{
			get { return 1500; }
		}
		public override void WalkToSpawn()
		{
			if (IsAlive)
				return;
			base.WalkToSpawn();
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 10; // dmg reduction for melee dmg
				case eDamageType.Crush: return 10; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 10; // dmg reduction for melee dmg
				default: return 20; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override void StartAttack(GameObject target)//messengers do not attack, these just run to point
		{
		}
		public override bool AddToWorld()
		{
			Model = 2386;
			Name = "Golestandt's messenger";
			Size = 80;
			Level = (byte)Util.Random(50, 55);
			RespawnInterval = -1;
			Realm = eRealm.None;
			MaxSpeedBase = 225;
			Faction = FactionMgr.GetFactionByID(31);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(31));
			GolestandtMessengerBrain adds = new GolestandtMessengerBrain();

			if (!AlbGolestandtBrain.DragonAdds.Contains(this))
				AlbGolestandtBrain.DragonAdds.Add(this);

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
	public class GolestandtMessengerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public GolestandtMessengerBrain()
		{
			AggroLevel = 100;
			AggroRange = 500;
		}
		private protected bool ChoosePath = false;
		private protected bool ChoosePath1 = false;
		private protected bool ChoosePath2 = false;
		private protected bool ChoosePath3 = false;
		private protected bool ChoosePath4 = false;
		private protected bool CanSpawnGraniteGiants = false;
		public override void Think()
		{
			if (Body.IsAlive)
			{
				if (ChoosePath == false)
				{
					switch (Util.Random(1, 4))//choose which path messenger will walk
					{
						case 1: ChoosePath1 = true; break;
						case 2: ChoosePath2 = true; break;
						case 3: ChoosePath3 = true; break;
						case 4: ChoosePath4 = true; break;
					}
					ChoosePath = true;
				}
				if (ChoosePath1)
					Path1();
				if (ChoosePath2)
					Path2();
				if (ChoosePath3)
					Path3();
				if (ChoosePath4)
					Path4();
			}
			base.Think();
		}

		#region Messengers Paths
		private short speed = 225;
		private protected bool path1point1 = false;
		private protected bool path1point2 = false;
		private protected bool path1point3 = false;

		private protected bool path2point1 = false;
		private protected bool path2point2 = false;
		private protected bool path2point3 = false;

		private protected bool path3point1 = false;
		private protected bool path3point2 = false;
		private protected bool path3point3 = false;

		private protected bool path4point1 = false;
		private protected bool path4point2 = false;
		private protected bool path4point3 = false;

		#region Path1
		private protected void Path1()
		{
			Point3D point1 = new Point3D(391353, 752390, 200);
			Point3D point2 = new Point3D(391185, 749212, 363);
			Point3D point3 = new Point3D(393537, 748310, 563);

			if (!Body.IsWithinRadius(point1, 30) && path1point1 == false)
			{
				Body.WalkTo(point1, speed);
			}
			else
			{
				path1point1 = true;
				if (!Body.IsWithinRadius(point2, 30) && path1point1 == true && path1point2 == false)
				{
					Body.WalkTo(point2, speed);
				}
				else
				{
					path1point2 = true;
					if (!Body.IsWithinRadius(point3, 30) && path1point1 == true && path1point2 == true
						&& path1point3 == false)
					{
						Body.WalkTo(point3, speed);
					}
					else
					{
						path1point3 = true;
						if (CanSpawnGraniteGiants == false)
						{
							SpawnGraniteGiants();
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(RemoveMessenger), 1000);
							CanSpawnGraniteGiants = true;
						}
					}
				}
			}
		}
		#endregion

		#region Path2
		private protected void Path2()
		{
			Point3D point1 = new Point3D(394175, 755677, 200);
			Point3D point2 = new Point3D(395993, 753955, 200);
			Point3D point3 = new Point3D(397102, 752316, 448);

			if (!Body.IsWithinRadius(point1, 30) && path2point1 == false)
			{
				Body.WalkTo(point1, speed);
			}
			else
			{
				path2point1 = true;
				if (!Body.IsWithinRadius(point2, 30) && path2point1 == true && path2point2 == false)
				{
					Body.WalkTo(point2, speed);
				}
				else
				{
					path2point2 = true;
					if (!Body.IsWithinRadius(point3, 30) && path2point1 == true && path2point2 == true
						&& path2point3 == false)
					{
						Body.WalkTo(point3, speed);
					}
					else
					{
						path2point3 = true;
						if (CanSpawnGraniteGiants == false)
						{
							SpawnGraniteGiants();
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(RemoveMessenger), 1000);
							CanSpawnGraniteGiants = true;
						}
					}
				}
			}
		}
		#endregion

		#region Path3
		private protected void Path3()
		{
			Point3D point1 = new Point3D(390959, 758732, 431);
			Point3D point2 = new Point3D(394489, 758450, 411);
			Point3D point3 = new Point3D(397504, 756902, 324);

			if (!Body.IsWithinRadius(point1, 30) && path3point1 == false)
			{
				Body.WalkTo(point1, speed);
			}
			else
			{
				path3point1 = true;
				if (!Body.IsWithinRadius(point2, 30) && path3point1 == true && path3point2 == false)
				{
					Body.WalkTo(point2, speed);
				}
				else
				{
					path3point2 = true;
					if (!Body.IsWithinRadius(point3, 30) && path3point1 == true && path3point2 == true
						&& path3point3 == false)
					{
						Body.WalkTo(point3, speed);
					}
					else
					{
						path3point3 = true;
						if (CanSpawnGraniteGiants == false)
						{
							SpawnGraniteGiants();
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(RemoveMessenger), 1000);
							CanSpawnGraniteGiants = true;
						}
					}
				}
			}
		}
		#endregion

		#region Path4
		private protected void Path4()
		{
			Point3D point1 = new Point3D(388541, 754932, 204);
			Point3D point2 = new Point3D(386798, 756434, 323);
			Point3D point3 = new Point3D(385011, 757680, 523);

			if (!Body.IsWithinRadius(point1, 30) && path4point1 == false)
			{
				Body.WalkTo(point1, speed);
			}
			else
			{
				path4point1 = true;
				if (!Body.IsWithinRadius(point2, 30) && path4point1 == true && path4point2 == false)
				{
					Body.WalkTo(point2, speed);
				}
				else
				{
					path4point2 = true;
					if (!Body.IsWithinRadius(point3, 30) && path4point1 == true && path4point2 == true
						&& path4point3 == false)
					{
						Body.WalkTo(point3, speed);
					}
					else
					{
						path4point3 = true;
						if (CanSpawnGraniteGiants == false)
						{
							SpawnGraniteGiants();
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(RemoveMessenger), 1000);
							CanSpawnGraniteGiants = true;
						}
					}
				}
			}
		}
		#endregion

		#endregion
		private protected int RemoveMessenger(ECSGameTimer timer)
		{
			if (Body.IsAlive)
			{
				Body.RemoveFromWorld();
			}
			return 0;
		}
		private protected void SpawnGraniteGiants()
		{
			for (int i = 0; i <= Util.Random(3, 5); i++)
			{
				GolestandtSpawnedAdd add = new GolestandtSpawnedAdd();
				add.X = Body.X + Util.Random(-200, 200);
				add.Y = Body.Y + Util.Random(-200, 200);
				add.Z = Body.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				if (ChoosePath1)
					add.PackageID = "ChoosePath1";
				if (ChoosePath2)
					add.PackageID = "ChoosePath2";
				if (ChoosePath3)
					add.PackageID = "ChoosePath3";
				if (ChoosePath4)
					add.PackageID = "ChoosePath4";
				add.AddToWorld();
			}
		}
	}
}

#endregion

#region Golestandt's  spawned adds
namespace DOL.GS
{
	public class GolestandtSpawnedAdd : GameNPC
	{
		public GolestandtSpawnedAdd() : base() { }

		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.15;
		}
		public override void DropLoot(GameObject killer) //no loot
		{
		}
		public override void WalkToSpawn()
		{
			if (IsAlive)
				return;
			base.WalkToSpawn();
		}
		public override long ExperienceValue => 0;
		public override int MaxHealth
		{
			get { return 5000; }
		}
		List<string> adds_names = new List<string>()
		{
				"granite giant stonelord","granite giant pounder","granite giant outlooker",
		};
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 120; }
		public override bool AddToWorld()
		{
			Name = adds_names[Util.Random(0, adds_names.Count - 1)];
			switch (Name)
			{
				case "granite giant stonelord": Model = 2386; Size = (byte)Util.Random(150, 170); break;
				case "granite giant pounder": Model = 2386; Size = (byte)Util.Random(130, 150); break;
				case "granite giant outlooker": Model = 2386; Size = (byte)Util.Random(130, 140); break;
			}
			Level = (byte)Util.Random(60, 64);
			Faction = FactionMgr.GetFactionByID(31);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(31));
			RespawnInterval = -1;

			MaxSpeedBase = 225;
			GolestandtSpawnedAdBrain sbrain = new GolestandtSpawnedAdBrain();

			if (!AlbGolestandtBrain.DragonAdds.Contains(this))
				AlbGolestandtBrain.DragonAdds.Add(this);

			SetOwnBrain(sbrain);
			sbrain.Start();
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
		public override bool IsVisibleToPlayers => true;
	}
}
namespace DOL.AI.Brain
{
	public class GolestandtSpawnedAdBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GolestandtSpawnedAdBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override bool Start()
		{
			if (Body.IsAlive)
				return true;

			return base.Start();
		}
		public override void Think()
		{
			if (Body.PackageID == "ChoosePath1" && !Body.InCombat && !HasAggro)
				Path1();
			if (Body.PackageID == "ChoosePath2" && !Body.InCombat && !HasAggro)
				Path2();
			if (Body.PackageID == "ChoosePath3" && !Body.InCombat && !HasAggro)
				Path3();
			if (Body.PackageID == "ChoosePath4" && !Body.InCombat && !HasAggro)
				Path4();

			base.Think();
		}
		#region Paths
		private protected bool path1point1 = false;
		private protected bool path1point2 = false;
		private protected bool path1point3 = false;

		private protected bool path2point1 = false;
		private protected bool path2point2 = false;
		private protected bool path2point3 = false;

		private protected bool path3point1 = false;
		private protected bool path3point2 = false;
		private protected bool path3point3 = false;

		private protected bool path4point1 = false;
		private protected bool path4point2 = false;
		private protected bool path4point3 = false;

		#region Path1
		private protected void Path1()
		{
			Point3D point1 = new Point3D(393690, 747560, 585);
			Point3D point2 = new Point3D(391348, 749033, 374);
			Point3D point3 = new Point3D(391351, 755567, 412);

			if (!Body.IsWithinRadius(point1, 30) && path1point1 == false)
			{
				Body.WalkTo(point1, 200);
			}
			else
			{
				path1point1 = true;
				if (!Body.IsWithinRadius(point2, 30) && path1point1 == true && path1point2 == false)
				{
					Body.WalkTo(point2, 200);
				}
				else
				{
					path1point2 = true;
					if (!Body.IsWithinRadius(point3, 30) && path1point1 == true && path1point2 == true
						&& path1point3 == false)
					{
						Body.WalkTo(point3, 200);
					}
					else
						path1point3 = true;
				}
			}
		}
		#endregion

		#region Path2
		private protected void Path2()
		{
			Point3D point1 = new Point3D(397218, 752345, 444);
			Point3D point2 = new Point3D(394848, 755457, 200);
			Point3D point3 = new Point3D(391445, 755608, 410);
			if (!Body.IsWithinRadius(point1, 30) && path2point1 == false)
			{
				Body.WalkTo(point1, 200);
			}
			else
			{
				path2point1 = true;
				if (!Body.IsWithinRadius(point2, 30) && path2point1 == true && path2point2 == false)
				{
					Body.WalkTo(point2, 200);
				}
				else
				{
					path2point2 = true;
					if (!Body.IsWithinRadius(point3, 30) && path2point1 == true && path2point2 == true
						&& path2point3 == false)
					{
						Body.WalkTo(point3, 200);
					}
					else
						path2point3 = true;
				}
			}
		}
		#endregion

		#region Path3
		private protected void Path3()
		{
			Point3D point1 = new Point3D(397504, 756902, 324);
			Point3D point2 = new Point3D(390933, 758814, 446);
			Point3D point3 = new Point3D(391331, 755730, 398);
			if (!Body.IsWithinRadius(point1, 30) && path3point1 == false)
			{
				Body.WalkTo(point1, 200);
			}
			else
			{
				path3point1 = true;
				if (!Body.IsWithinRadius(point2, 30) && path3point1 == true && path3point2 == false)
				{
					Body.WalkTo(point2, 200);
				}
				else
				{
					path3point2 = true;
					if (!Body.IsWithinRadius(point3, 30) && path3point1 == true && path3point2 == true
						&& path3point3 == false)
					{
						Body.WalkTo(point3, 200);
					}
					else
						path3point3 = true;
				}
			}
		}
		#endregion

		#region Path4
		private protected void Path4()
		{
			Point3D point1 = new Point3D(384804, 757887, 537);
			Point3D point2 = new Point3D(388423, 754910, 212);
			Point3D point3 = new Point3D(391103, 755534, 380);

			if (!Body.IsWithinRadius(point1, 30) && path4point1 == false)
			{
				Body.WalkTo(point1, 200);
			}
			else
			{
				path4point1 = true;
				if (!Body.IsWithinRadius(point2, 30) && path4point1 == true && path4point2 == false)
				{
					Body.WalkTo(point2, 200);
				}
				else
				{
					path4point2 = true;
					if (!Body.IsWithinRadius(point3, 30) && path4point1 == true && path4point2 == true
						&& path4point3 == false)
					{
						Body.WalkTo(point3, 200);
					}
					else
						path4point3 = true;
				}
			}
		}
		#endregion

		#endregion
	}
}
#endregion

