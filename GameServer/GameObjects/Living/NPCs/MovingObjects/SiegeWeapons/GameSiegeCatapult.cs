using System.Collections;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class GameSiegeCatapult : GameSiegeWeapon
	{
		public GameSiegeCatapult()
			: base()
		{
			MeleeDamageType = EDamageType.Crush;
			Name = "field catapult";
			AmmoType = 0x13;
			this.Effect = 0x89C;
			this.Model = 0xA26;
			ActionDelay = new int[]
			{
				0,//none
				5000,//aiming
				10000,//arming
				0,//loading
				0//fireing base delay
			};//en ms
			BaseDamage = 100;
			MinAttackRange = 1000;
			MaxAttackRange = 3000;
			AttackRadius = 150;

			/*SpellLine siegeWeaponSpellLine = SkillBase.GetSpellLine(GlobalSpellsLines.SiegeWeapon_Spells);
			IList spells = SkillBase.GetSpellList(siegeWeaponSpellLine.KeyName);
			if (spells != null)
			{
				foreach (Spell spell in spells)
				{
					if (spell.ID == 2430) //TODO good id for catapult
					{
						if(spell.Level <= Level)
						{
							m_spellHandler = ScriptMgr.CreateSpellHandler(this, spell, siegeWeaponSpellLine);
						}
						break;
					}
				}
			}*/
		}
		
		public int AttackRadius;

		private GameNpc tempLOSSkyChecker;
		private GameNpc tempLOSGTChecker;
		private int loschecks;
		public override void Aim()
		{
			if (!CanUse()) return;
			if(SiegeWeaponTimer.IsAlive || this.IsMoving)
			{
				Owner.Out.SendMessage(GetName(0, true) +" isn't ready to be aimed yet!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}
			
			Point3D newGroundTarget = null;
			newGroundTarget = Owner.TargetObject != null ? Owner.TargetObject : Owner.GroundTarget;
			if (newGroundTarget == null)
			{
				Owner.Out.SendMessage("You must have a target!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			} 
			
			//Range Checks
			if (MinAttackRange != -1 && this.GetDistanceTo(newGroundTarget) < MinAttackRange)
			{
				Owner.Out.SendMessage("The " + GetName(0, false) + "'s target location is too close!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}
			if (MaxAttackRange != -1 && this.GetDistanceTo(newGroundTarget) > MaxAttackRange)
			{
				Owner.Out.SendMessage("The " + GetName(0, false) + "'s target is too far away to reach!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}

			//CheckGTLOS(newGroundTarget);

			CurrentState &= ~eState.Aimed;
			SetGroundTarget(newGroundTarget.X, newGroundTarget.Y, newGroundTarget.Z);
			SiegeWeaponTimer.CurrentAction = SiegeTimer.eAction.Aiming;
            Heading = GetHeading( GroundTarget );
			PreAction();
			if (Owner != null)
			{
				Owner.Out.SendMessage(GetName(0, true) + " is turning to your target. (" + (GetActionDelay(SiegeTimer.eAction.Aiming) / 1000).ToString("N") + "s)", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}

		private void CheckGTLOS(Point3D gt)
		{
			loschecks=0;
			tempLOSSkyChecker = new GameNpc();
			tempLOSSkyChecker.Name = "tempLOSSkyChecker";
			tempLOSSkyChecker.Flags ^= ENpcFlags.FLYING | ENpcFlags.PEACE;
		
			tempLOSSkyChecker.Model = 408; //temp model for testing
			tempLOSSkyChecker.Create(this.CurrentRegionID,gt.X, gt.Y, gt.Z+2000,0);

			tempLOSGTChecker = new GameNpc();
			tempLOSGTChecker.Name = "tempLOSGTChecker";
			tempLOSGTChecker.Flags ^= ENpcFlags.FLYING | ENpcFlags.PEACE;
			tempLOSGTChecker.Model = 408; //temp model for testing
			tempLOSGTChecker.Create(this.CurrentRegionID,gt.X, gt.Y, gt.Z,0);

			if (Owner is GamePlayer)
			{
				ClientService.UpdateObjectForPlayer(Owner, tempLOSSkyChecker);
				ClientService.UpdateObjectForPlayer(Owner, tempLOSGTChecker);
				Owner.Out.SendCheckLOS(tempLOSSkyChecker, tempLOSGTChecker, new CheckLOSResponse(this.CheckGTLOSCallback));
			}
		}

		private void CheckGTLOSCallback(GamePlayer player, ushort response, ushort targetOID)
		{
			log.Debug($"LOSCallback {player} {response} {targetOID}");
			if(response == 0 && targetOID ==0) return;
			if ((response & 0x100) == 0x100)
			{
				log.Debug($"LOSCheck Succeeded {response} GroundTarget set to: {tempLOSGTChecker.X} {tempLOSGTChecker.Y} {tempLOSGTChecker.Z}");
				loschecks=0;
				SetGroundTarget(tempLOSGTChecker.X, tempLOSGTChecker.Y, tempLOSGTChecker.Z);
				//tempLOSSkyChecker.Delete();
				//tempLOSGTChecker.Delete();
			} 
			else if(loschecks<10)
			{
				log.Debug("LOSCheck Failed, raising GT Z by 200");
				loschecks++;
				tempLOSGTChecker.MoveTo(tempLOSGTChecker.CurrentRegionID, tempLOSGTChecker.X, tempLOSGTChecker.Y, tempLOSGTChecker.Z+200, tempLOSGTChecker.Heading);

				if (Owner is GamePlayer)
				{
					ClientService.UpdateObjectForPlayer(Owner, tempLOSGTChecker);
					Owner.Out.SendCheckLOS(tempLOSSkyChecker, tempLOSGTChecker, new CheckLOSResponse(this.CheckGTLOSCallback));
				}
			}
			else
				log.Debug ("Could not get LOS");
		}

		protected IList SelectTargets()
		{
			ArrayList list = new ArrayList(20);

			foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(CurrentRegionID, GroundTarget.X, GroundTarget.Y, GroundTarget.Z, (ushort) AttackRadius))
			{
				if (Owner != null && GameServer.ServerRules.IsAllowedToAttack(Owner, player, true))
				{
					list.Add(player);
				}
				else if (GameServer.ServerRules.IsAllowedToAttack(this, player, true)) //use this siege as attacker if owner is null
				{
					list.Add(player);
				}
			}

			foreach (GameDoorBase door in CurrentRegion.GetDoorsInRadius(GroundTarget, (ushort) AttackRadius))
			{
				if (Owner != null && door is GameKeepDoor && GameServer.ServerRules.IsAllowedToAttack(Owner, (GameKeepDoor)door, true))
				{
					list.Add(door);
				}
				else if (door is GameKeepDoor && GameServer.ServerRules.IsAllowedToAttack(this, (GameKeepDoor)door, true)) //use this siege as attacker if Owner is null
				{
					list.Add(door);
				}
			}

			foreach (GameNpc npc in WorldMgr.GetNPCsCloseToSpot(CurrentRegionID, GroundTarget.X, GroundTarget.Y, GroundTarget.Z, (ushort) AttackRadius))
			{
				if (Owner != null &&GameServer.ServerRules.IsAllowedToAttack(Owner, npc, true))
				{
					list.Add(npc);
				}
				else if (GameServer.ServerRules.IsAllowedToAttack(this, npc, true)) //use this siege as attacker if Owner is null
				{
					list.Add(npc);
				}
			}
			
			if (!list.Contains(this.TargetObject))
			{
				list.Add(this.TargetObject);
			}
			return list;
		}

		public override void DoDamage()
		{
			//			InventoryItem ammo = this.Ammo[AmmoSlot] as InventoryItem;
			//todo remove ammo + spell in db and uncomment
			//m_spellHandler.StartSpell(player);
			base.DoDamage();//anim mut be called after damage
			if (GroundTarget == null) return;
			IList targets = SelectTargets();

			foreach (GameLiving living in targets)
			{
				if(living == null)
					continue;
				int damageAmount = CalcDamageToTarget(living) + Util.Random(50);

				AttackData ad = new AttackData();
				ad.Target = living;
				ad.AttackType = EAttackType.Ranged;
				ad.AttackResult = EAttackResult.HitUnstyled;
				ad.Damage = damageAmount;
				ad.DamageType = MeleeDamageType;
				

				if(Owner != null)
				{
					ad.Attacker = Owner;
					living.TakeDamage(Owner, EDamageType.Crush, damageAmount, 0);
					living.OnAttackedByEnemy(ad);
	
					Owner.OnAttackEnemy(ad);
					Owner.Out.SendMessage("The " + this.Name + " hits " + living.Name + " for " + damageAmount + " damage!", EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
				}
				else
				{
					ad.Attacker = this;
					living.TakeDamage(this, EDamageType.Crush, damageAmount, 0);
					living.OnAttackedByEnemy(ad);
				}


				foreach (GamePlayer player in living.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					player.Out.SendCombatAnimation(this, living, 0x0000, 0x0000, 0x00, 0x00, 0x14, living.HealthPercent);
			}
			return;
		}

		/// <summary>
		/// Calculates the damage based on the target type (door, siege, player)
		/// <summary>
		public override int CalcDamageToTarget(GameLiving target)
		{
			//Catapults are better against players/NPCs than against doors/other objects
			if(target is GamePlayer || target is GameNpc)
				return BaseDamage * 2;
			else
				return BaseDamage;
		}

		
		public override bool ReceiveItem(GameLiving source, DOL.Database.DbInventoryItem item)
		{
			//todo check if bullet
			return base.ReceiveItem(source, item);
		}
	}
}
