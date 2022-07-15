using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
    public class SpectralProvisioner : GameEpicBoss
    {
	public SpectralProvisioner()
		: base() { }
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
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (damageType == eDamageType.Heat || damageType == eDamageType.Spirit || damageType == eDamageType.Cold) //take no damage
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GamePet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage("The Spectral Provisioner is immune to this form of attack.", eChatType.CT_System,eChatLoc.CL_ChatWindow);

					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public override double GetArmorAF(eArmorSlot slot)
	    {
		    return 350;
	    }
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == "CCImmunity")
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
	    {
		    // 85% ABS is cap.
		    return 0.20;
	    }

	    public override short MaxSpeedBase
	    {
		    get => (short)(191 + (Level * 2));
		    set => m_maxSpeedBase = value;
	    }
	    public override int MaxHealth => 200000;

	    public override int AttackRange
	    {
		    get => 180;
		    set { }
	    }
		//private Point3D spawnPoint = new Point3D(30058, 40883, 17004);
		//public override ushort SpawnHeading { get => base.SpawnHeading; set => base.SpawnHeading = 2036; }
		//public override Point3D SpawnPoint { get => spawnPoint; set => base.SpawnPoint = spawnPoint; }
		public override bool AddToWorld()
		{
			Level = 77;
			Gender = eGender.Neutral;
			BodyType = 11; // undead
			MaxDistance = 0;
			TetherRange = 0;
			RoamingRange = 0;
			MaxSpeedBase = 300;
			CurrentSpeed = 300;

			/*SpawnPoint.X = 30058;
			SpawnPoint.Y = 40883;
			SpawnPoint.Z = 17004;

			X = 30058;
			Y = 40883;
			Z = 17004;
			Heading = 2036;*/

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166427);
			LoadTemplate(npcTemplate);
			SpectralProvisionerBrain.point1check = false;
			SpectralProvisionerBrain.point2check = false;
			SpectralProvisionerBrain.point3check = false;
			SpectralProvisionerBrain.point4check = false;
			SpectralProvisionerBrain.point5check = false;
			SpectralProvisionerBrain.point6check = false;
			SpectralProvisionerBrain.point7check = false;
			SpectralProvisionerBrain.point8check = false;
			SpectralProvisionerBrain sBrain = new SpectralProvisionerBrain();
			SetOwnBrain(sBrain);
			LoadedFromScript = false;//load from database
			X = sBrain.spawnPoint.X;
			Y = sBrain.spawnPoint.Y;
			Z = sBrain.spawnPoint.Z;
			base.AddToWorld();
			return true;
		}
	   
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Spectral Provisioner NPC Initializing...");
		}
		public override void WalkToSpawn(short speed)
		{
			if (IsAlive)
				return;
			base.WalkToSpawn(speed);
		}
		public override void StartAttack(GameObject target)
        {
        }
		public override bool IsVisibleToPlayers => true;
	}  
}

namespace DOL.AI.Brain
{
	public class SpectralProvisionerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SpectralProvisionerBrain()
				: base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 2000;
		}
		private bool CanAddJunk = false;
        public override void OnAttackedByEnemy(AttackData ad)
		{
			if (Util.Chance(30) && ad != null && !CanAddJunk && ad.Attacker is GamePlayer)
			{
				ItemTemplate sackJunk = GameServer.Database.FindObjectByKey<ItemTemplate>("sack_of_decaying_junk");
				InventoryItem item = GameInventoryItem.Create(sackJunk);

				foreach (GamePlayer player in Body.GetPlayersInRadius(500))
				{
					if (!player.IsAlive) continue;
					item.OwnerID = player.ObjectId;
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);
				}
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDecayingJunk), Util.Random(25000,35000));
				CanAddJunk = true;
			}
			base.OnAttackedByEnemy(ad);
		}
		private int ResetDecayingJunk(ECSGameTimer timer)
        {
			CanAddJunk = false;
			return 0;
        }
		public static bool point1check = false;
		public static bool point2check = false;
		public static bool point3check = false;
		public static bool point4check = false;
		public static bool point5check = false;
		public static bool point6check = false;
		public static bool point7check = false;
		public static bool point8check = false;
		private Point3D point1 = new Point3D(30050, 39425, 17004);
		private Point3D point2 = new Point3D(30940, 39418, 17004);
		private Point3D point3 = new Point3D(32065, 40205, 17004);
		private Point3D point4 = new Point3D(32075, 42378, 17004);
		private Point3D point5 = new Point3D(32072, 40376, 17006);
		private Point3D point6 = new Point3D(32967, 39369, 17007);
		private Point3D point7 = new Point3D(32057, 38494, 17007);
		private Point3D point8 = new Point3D(31022, 39382, 17006);
		public Point3D spawnPoint = new Point3D(30058, 40883, 17004);
		public override void Think()
		{
			if (Body.X < 0 || Body.Y < 0 || Body.Z < 0)
			{
				log.Warn(Body.Name + " position is under 0! Moving mob to spawn point! Possition was: X: "+Body.X+", Y: "+Body.Y+", Z: "+Body.Z);
				Body.MoveTo(60, 30058, 40883, 17004, 2036);
			}
			if (Body.IsAlive)
			{
				//Point3D spawn = new Point3D(30049, 40799, 17004);
				Body.MaxSpeedBase = 300;
				Body.CurrentSpeed = 300;

				// if (HasAggro && Body.TargetObject != null)
				// {
				// 	foreach (GameNPC npc in Body.GetNPCsInRadius(800))
				// 	{
				// 		if (npc != null && npc.IsAlive && npc.PackageID == "ProvisionerBaf")
				// 			AddAggroListTo(npc.Brain as StandardMobBrain);
				// 	}
				// }

				#region Walk path
				if (!Body.IsWithinRadius(point1, 30) && point1check == false)
				{
					Body.WalkTo(point1, (short)Util.Random(195, 300));
					//log.Warn("Moving to point1, " + point1+"Corrent Pos: "+Body.X+", "+Body.Y+", "+Body.Z);
				}
				else
				{
					point1check = true;
					point8check = false;
					if (!Body.IsWithinRadius(point2, 30) && point1check == true && point2check == false)
					{
						Body.WalkTo(point2, (short)Util.Random(195, 300));
						//log.Warn("Arrived at point1,Moving to point2, " + point2);
					}
					else
					{
						point2check = true;
						if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true &&
							point3check == false)
						{
							Body.WalkTo(point3, (short)Util.Random(195, 300));
							//log.Warn("Arrived at point2,Moving to point3, " + point3);
						}
						else
						{
							point3check = true;
							if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true &&
								point3check == true && point4check == false)
							{
								Body.WalkTo(point4, (short)Util.Random(195, 300));
								//log.Warn("Arrived at point3,Moving to point4, " + point4);
							}
							else
							{
								point4check = true;
								if (!Body.IsWithinRadius(point5, 30) && point1check == true && point2check == true &&
									point3check == true && point4check == true && point5check == false)
								{
									Body.WalkTo(point5, (short)Util.Random(195, 300));
									//log.Warn("Arrived at point4,Moving to point5, " + point4);
								}
								else
								{
									point5check = true;
									if (!Body.IsWithinRadius(point6, 30) && point1check == true && point2check == true &&
									point3check == true && point4check == true && point5check == true && point6check == false)
									{
										Body.WalkTo(point6, (short)Util.Random(195, 300));
										//log.Warn("Arrived at point5,Moving to point6, " + point6);
									}
									else
									{
										point6check = true;
										if (!Body.IsWithinRadius(point7, 30) && point1check == true && point2check == true &&
										point3check == true && point4check == true && point5check == true && point6check == true && point7check == false)
										{
											Body.WalkTo(point7, (short)Util.Random(195, 300));
											//log.Warn("Arrived at point6,Moving to point7, " + point7);
										}
										else
										{
											point7check = true;
											if (!Body.IsWithinRadius(point8, 30) && point1check == true && point2check == true &&
											point3check == true && point4check == true && point5check == true && point6check == true && point7check == true && !point8check)
											{
												Body.WalkTo(point8, (short)Util.Random(195, 300));
												//log.Warn("Arrived at point7,Moving to point8, " + point8);
											}
											else
											{
												point8check = true;
												point7check = false;
												point1check = false;
												point2check = false;
												point3check = false;
												point4check = false;
												point5check = false;
												point6check = false;
												//log.Warn("Clearing flags");
											}
										}
									}
								}
							}
						}
					}
				}
                #endregion

                if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
				{
					ClearAggroList();
					Body.Health = Body.MaxHealth;
				}
			}
			base.Think();
		}		
	}
}