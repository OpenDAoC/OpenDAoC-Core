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
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	   
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Spectral Provisioner NPC Initializing...");
		}
		public override void WalkToSpawn()
		{
			if (CurrentRegionID == 60) //if region is caer sidi
			{
				if (IsAlive)
					return;
			}
			base.WalkToSpawn();
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
					if (player.IsAlive)
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
		public override void Think()
		{
			if (Body.IsAlive)
			{
				Point3D spawn = new Point3D(30049, 40799, 17004);
				Body.MaxSpeedBase = 300;
				Body.CurrentSpeed = 300;
				Point3D point1 = new Point3D(30062, 39454, 17004);
				Point3D point2 = new Point3D(31109, 39425, 17004);
				Point3D point3 = new Point3D(32012, 40286, 17004);
				Point3D point4 = new Point3D(32064, 42195, 17004);
				Point3D point5 = new Point3D(32089, 40241, 17004);
				Point3D point6 = new Point3D(33021, 39384, 17004);
				Point3D point7 = new Point3D(32059, 38549, 17004);
				Point3D point8 = new Point3D(31124, 39405, 17004);

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
				}
				else
				{
					point1check = true;
					point8check = false;
					if (!Body.IsWithinRadius(point2, 30) && point1check == true && point2check == false)
					{
						Body.WalkTo(point2, (short)Util.Random(195, 300));
					}
					else
					{
						point2check = true;
						if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true &&
							point3check == false)
						{
							Body.WalkTo(point3, (short)Util.Random(195, 300));
						}
						else
						{
							point3check = true;
							if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true &&
								point3check == true && point4check == false)
							{
								Body.WalkTo(point4, (short)Util.Random(195, 300));
							}
							else
							{
								point4check = true;
								if (!Body.IsWithinRadius(point5, 30) && point1check == true && point2check == true &&
									point3check == true && point4check == true && point5check == false)
								{
									Body.WalkTo(point5, (short)Util.Random(195, 300));
								}
								else
								{
									point5check = true;
									if (!Body.IsWithinRadius(point6, 30) && point1check == true && point2check == true &&
									point3check == true && point4check == true && point5check == true && point6check == false)
									{
										Body.WalkTo(point6, (short)Util.Random(195, 300));
									}
									else
									{
										point6check = true;
										if (!Body.IsWithinRadius(point7, 30) && point1check == true && point2check == true &&
										point3check == true && point4check == true && point5check == true && point6check == true && point7check == false)
										{
											Body.WalkTo(point7, (short)Util.Random(195, 300));
										}
										else
										{
											point7check = true;
											if (!Body.IsWithinRadius(point8, 30) && point1check == true && point2check == true &&
											point3check == true && point4check == true && point5check == true && point6check == true && point7check == true && !point8check)
											{
												Body.WalkTo(point8, (short)Util.Random(195, 300));
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