using System;
using System.Collections;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Commands;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Keeps;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS
{
	#region GameSiegeweapon
	public class GameSiegeWeapon : GameMovingObject
	{
		public GameSiegeWeapon()
		{
			SetOwnBrain(new BlankBrain());
			this.Realm = 0;
			Level = 1;
			CurrentState = eState.Inactive;
			m_ammo = new ArrayList();

			m_ammoSlot = 0x14;
			ActionDelay	= new int[]
				{
					0,//none
					10000,//aiming
					15000,//arming
					5000,//loading
					0//fireing
				};//en ms
			m_enableToMove = true;
			MaxSpeedBase = 50;
			MinAttackRange = -1;
			MaxAttackRange = -1;
			BaseDamage = 0;
		}
		public int SIEGE_WEAPON_CONTROLE_DISTANCE = 256;
		public const int TIME_TO_DECAY = 60 * 1000 * 3; //3 min
		public const int DECAYPERIOD = 240000; //ms
		#region enum
		public enum eState : byte
		{
			Inactive = 0x0,
			Armed = 0x01,
			Aimed = 0x02,
			Ready = 0x03,//armed+aimed
		}

		public enum eCommand : byte
		{
			None = 0x00,
			PutAmmo = 0x01,
			Aim = 0x03,
			Fire = 0x04,
			Move = 0x05,
			Repair = 0x06,
			Salvage = 0x07,
			Release = 0x08,
		}
		#endregion
		#region properties
		private GameKeepHookPoint m_hookPoint;
		public GameKeepHookPoint HookPoint
		{
			get { return m_hookPoint; }
			set { m_hookPoint = value; }
		}

		public override Region CurrentRegion
		{
			get
			{
				return base.CurrentRegion;
			}
			set
			{
				base.CurrentRegion = value;
				SiegeWeaponTimer = new SiegeTimer(this);
			}
		}

		private byte m_ammoType;
		public byte AmmoType
		{
			get { return m_ammoType; }
			set { m_ammoType = value; }
		}

		private GamePlayer m_owner;
		public GamePlayer Owner
		{
			get { return m_owner; }
			set { m_owner = value; }
		}
		private eState m_currentState;
		public eState CurrentState
		{
			get { return m_currentState; }
			set { m_currentState = value; }
		}

		public virtual ushort Effect
		{
			get { return m_effect; }
			set { m_effect = value; }
		}
		protected ArrayList m_ammo;
		public virtual ArrayList Ammo
		{
			get { return m_ammo; }
			set { m_ammo = value; }
		}
		protected SiegeTimer m_siegeTimer;
		public virtual SiegeTimer SiegeWeaponTimer
		{
			get { return m_siegeTimer; }
			set { m_siegeTimer = value; }
		}
		public override int MaxHealth
		{
			get
			{
				return 10000;
			}
		}

		public override int Mana
		{
			get { return 50000; }
		}

		private int m_timesrepaired;
		public int TimesRepaired
		{
			get { return m_timesrepaired; }
			set { m_timesrepaired = value; }
		}

		protected EcsGameTimer m_decayTimer;
		/// <summary>
		/// The lock object for lazy regen timers initialization
		/// </summary>
		protected readonly object m_decayTimerLock = new object();

		protected EcsGameTimer m_controlRangeTimer;
		/// <summary>
		/// The lock object for controlcheck timers initialization
		/// </summary>
		protected readonly object m_controlRangeTimerLock = new object();

		private ushort m_ammoSlot;
		public ushort AmmoSlot
		{
			get { return m_ammoSlot; }
			set { m_ammoSlot = value; }
		}

		//The Decayed HP ammount to put this Siege Weapon into "this siegeweapon needs to be repaired" and unusable;
		public int DecayedHp
		{
			get { return 3 * (this.MaxHealth / 10); }
		}

		//Amount of HP to deduct from Siege Weapon on each Decay Tick
		public int DecayDeductHp
		{
			get { return this.MaxHealth / 10; }
		}

		private string m_itemId;
		public string ItemId
		{
			get { return m_itemId; }
			set { m_itemId = value; }
		} 

		/// <summary>
		/// Base damage for this siege, used to calculate damage against specific targets
		/// <summary>
		private int m_baseDamage;
	
		public virtual int BaseDamage
		{
			get { return m_baseDamage; }
			set { m_baseDamage = value; }
		}

		/// <summary>
		/// The minimum range a target/groundtarget must be above for this siege to fire
		/// </summary>
		public int MinAttackRange;

		/// <summary>
		/// The maximum range a target/groundtarget must be below for this siege to fire
		/// </summary>
		public int MaxAttackRange;
		

		#endregion
		#region public methode
		public void TakeControl(GamePlayer player)
		{
			if (Owner != null && Owner != player)
			{
				player.Out.SendMessage(GetName(0, true) + " is already under control.", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}
			if (player.SiegeWeapon != null && player.SiegeWeapon != this)
			{
				player.Out.SendMessage("You already have a siege weapon under your control.", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}
			if (IsMoving)
			{
				player.Out.SendMessage("You can't take control of a siege weapon while it is moving.", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}

			if (Realm == 0) Realm = player.Realm;
			Owner = player;
			player.SiegeWeapon = this;
			Owner.Out.SendSiegeWeaponInterface(this, SiegeWeaponTimer.TimeUntilElapsed / 100);
			player.Out.SendMessage("You take control of " + GetName(0, false) + ".", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
			StartControlRangeCheck();
			if ((CurrentState & GameSiegeWeapon.eState.Armed) != GameSiegeWeapon.eState.Armed)
				Arm();

		}
		public virtual void ReleaseControl()
		{
			if (Owner == null) return;
			Owner.Out.SendMessage("You are no longer controlling " + GetName(0, false) + ".", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
			Owner.Out.SendSiegeWeaponCloseInterface();
			Owner.SiegeWeapon = null;
			Owner = null;
			StopControlRangeCheck();
			StopMove();
		}

		public override void Die(GameObject killer)
		{
			StopDecay();
			ReleaseControl();
			Delete();
		}

		public virtual void Aim()
		{
			if (!CanUse()) return;
			if(SiegeWeaponTimer.IsAlive || this.IsMoving)
			{
				Owner.Out.SendMessage(GetName(0, true) +" isn't ready to be aimed yet!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}
			if (Owner.TargetObject == null)
			{
				Owner.Out.SendMessage("You must have a target!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			} 
			if (!GameServer.ServerRules.IsAllowedToAttack(Owner, ((GameLiving)Owner.TargetObject), true))
			{
				Owner.Out.SendMessage("You cannot attack that!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			} 

			if (!Owner.TargetInView)
			{
				Owner.Out.SendMessage("Target is not in view!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			} 

			//Range Checks
			if (MinAttackRange != -1 && this.GetDistanceTo(Owner.TargetObject) < MinAttackRange)
			{
				Owner.Out.SendMessage("The " + GetName(0, false) + "'s target location is too close!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}
			if (MaxAttackRange != -1 && this.GetDistanceTo(Owner.TargetObject) > MaxAttackRange)
			{
				Owner.Out.SendMessage("The " + GetName(0, false) + "'s target is too far away to reach!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}

			CurrentState &= ~eState.Aimed;
			SetGroundTarget(Owner.TargetObject.X, Owner.TargetObject.Y, Owner.TargetObject.Z);
			TargetObject = Owner.TargetObject;
			SiegeWeaponTimer.CurrentAction = SiegeTimer.eAction.Aiming;
            Heading = GetHeading( GroundTarget );
			PreAction();
			if (Owner != null)
			{
				Owner.Out.SendMessage(GetName(0, true) + " is turning to your target. (" + (GetActionDelay(SiegeTimer.eAction.Aiming) / 1000).ToString("N") + "s)", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}

		public void Arm()
		{
			if (!CanUse()) return;
			CurrentState &= ~eState.Armed;
			SiegeWeaponTimer.CurrentAction = SiegeTimer.eAction.Arming;
			PreAction();
			if (Owner != null)
			{//You prepare the cauldron of boiling oil for firing. (15.0s until armed)
				Owner.Out.SendMessage("You prepare " + GetName(0, false) + " for firing. (" + (GetActionDelay(SiegeTimer.eAction.Arming) / 1000).ToString("N") + "s until armed)", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}

		}
		public void Move()
		{
			if (!CanUse()) return;
			if (!m_enableToMove) return;
			if (Owner == null || Owner.GroundTarget == null) return;
            if ( !this.IsWithinRadius( Owner.GroundTarget, 1000 ) )
			{
				Owner.Out.SendMessage("Ground target is too far away to move to!", EChatType.CT_System,
									  EChatLoc.CL_SystemWindow);
				return;
			}
            
            if (!Owner.GroundTargetInView)
			{
				Owner.Out.SendMessage("Ground target is out of sight!", EChatType.CT_System,
									  EChatLoc.CL_SystemWindow);
				return;
			}
   
			if (Owner.GroundTarget.Z > this.Z + 100)
			{
				Owner.Out.SendMessage("Ground target too high!", EChatType.CT_System,
					EChatLoc.CL_SystemWindow);
				return;
			}
   
			//let's check if we are trying to move too close to a door, if we are, don't move
			foreach (GameDoorBase door in Owner.CurrentRegion.GetDoorsInRadius(Owner.GroundTarget, (ushort)(AttackRange - 50)))
			{
				if (door is GameKeepDoor)
				{
					Owner.Out.SendMessage("You can't move a ram that close to a door!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
			}
   
			//unarmed & unaim siege weapon
			CurrentState &= ~eState.Armed;
			TargetObject = null;
			CurrentState &= ~eState.Aimed;
			WalkTo(Owner.GroundTarget, 100);
		}

		public void StopMove()
		{
			StopMoving();
		}

		public void Load(int ammo)
		{
			AmmoSlot = (ushort)ammo;
		}
		public void Aimed()
		{
			if (!CanUse()) return;
			CurrentState |= eState.Aimed;
			if (Owner != null)
			{
				Owner.Out.SendMessage("Your " + Name + " is now aimed!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
		public void Armed()
		{
			if (!CanUse()) return;
			CurrentState |= eState.Armed;
			if (Owner != null)
			{
				Owner.Out.SendMessage("Your " + Name + " is now armed!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
		public virtual void Fire()
		{
			if (!CanUse()) return;
			if (CurrentState != eState.Ready)
			{
				if (Owner != null)
				{
					Owner.Out.SendMessage("The " + Name + " is not ready to fire yet!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				return;
			}
			if (TargetObject != null)
				SetGroundTarget(TargetObject.X, TargetObject.Y, TargetObject.Z);
			if (GroundTarget == null)
				return;

			//Range Checks
			if (MinAttackRange != -1 && this.GetDistanceTo(GroundTarget) < MinAttackRange)
			{
				Owner.Out.SendMessage("Your target is too close to this siege weapon!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}
			if (MaxAttackRange != -1 && this.GetDistanceTo(GroundTarget) > MaxAttackRange)
			{
				Owner.Out.SendMessage("Your target is too far away to this siege weapon!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}

			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(MakeDelayedDamage), GetActionDelay(SiegeTimer.eAction.Fire));
			BroadcastFireAnimation(GetActionDelay(SiegeTimer.eAction.Fire));
			if (Owner != null)
				Owner.Out.SendMessage("You fire " + GetName(0, false) + "!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			Arm();
		}

		private int MakeDelayedDamage(EcsGameTimer callingTimer)
		{
			DoDamage();
			return 0;
		}

		/// <summary>
		/// Calculates the damage based on the target type (door, siege, player)
		/// <summary>
		public virtual int CalcDamageToTarget(GameLiving target)
		{
			return BaseDamage;
		}

		public virtual void DoDamage()
		{
		}
		/*	slot:48 level:90 value1:0x00 value2:0x00 hand:0x00 damageType:0x00 objectType:0x29 weight:2    con:100 dur:0   qual:0   bonus:0  model:0x0A3C color:0x0000 effect:0x00 unk1_172:1 "2 greek fire"
			slot:49 level:90 value1:0x00 value2:0x00 hand:0x00 damageType:0x00 objectType:0x29 weight:2    con:100 dur:0   qual:0   bonus:0  model:0x0A3D color:0x0000 effect:0x00 unk1_172:1 "2 ice ball"
			index:0  unk1:0x5A00 unk2:0x0000 unk3:0x2900 unk4:0x0264 unk5:0x00 unk6:0x0000 model:0x0A3D unk7:0x0001 unk7:0x0000 name:"2 ice ball")
			index level value1  value2 hand  objecttype damagetype weight conc dur qual bonnus model color effect
		*/

		public void TryRepair()
		{
			if (Owner == null) return;
			if (TimesRepaired <= 3)
			{
				if (Owner.GetCraftingSkillValue(ECraftingSkill.WoodWorking) < 301)
				{
					Owner.Out.SendMessage("You must have woodworking skill to repair a siege weapon.", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
					return;
				}
			}
			else
			{
				this.Owner.Out.SendMessage("The siegeweapon has decayed beyond repairs!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}

			RepairCommand repairCommand = new RepairCommand();
			if (!repairCommand.PreFireChecks(Owner, this)) return;
			repairCommand.StartRepair(Owner, this);
		}
		public bool Repair(int amount)
		{
			if (TimesRepaired <= 3)
			{
				// if (Owner.GetCraftingSkillValue(eCraftingSkill.WoodWorking) < 301)
				// {
				// 	Owner.Out.SendMessage("You must have woodworking skill to repair a siege weapon.", eChatType.CT_Say, eChatLoc.CL_SystemWindow);
				// 	return false;
				// }
				TimesRepaired += 1;
				Health += amount;
				return true;
			}
			else
			{
				// this.Owner.Out.SendMessage("The siegeweapon has decayed beyond repairs!", eChatType.CT_Say, eChatLoc.CL_SystemWindow);
				return false;
			}
		}

		public void salvage()
		{
			if (Owner.GetCraftingSkillValue(ECraftingSkill.SiegeCrafting) == -1)
			{
				Owner.Out.SendMessage("You must be a Siege weapon crafter to salvage it.", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return;
			}
			Owner.SalvageSiegeWeapon(this);
		}
		#endregion
		#region private methods
		private void BroadcastAnimation()
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSiegeWeaponAnimation(this);
			}
		}
		private void BroadcastFireAnimation(int timer)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSiegeWeaponFireAnimation(this, timer);
				//				player.Out.SendEmoteAnimation(this, (eEmote)201); // on trebuchet fire external ammo ?
			}
		}
		protected int[] ActionDelay;
		private ushort m_effect;
		private bool m_enableToMove;

		/// <summary>
		/// delay to do action in Ms
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public int GetActionDelay(SiegeTimer.eAction action)
		{
			if (action == SiegeTimer.eAction.Fire && GroundTarget != null)
                return (int)( ActionDelay[(int)action] + this.GetDistanceTo( GroundTarget ) );
			
			int delay = ActionDelay[(int)action];
			//TODO: better to use a property here - discuss to implement one? dunnow if siegespeed is used at another place.
			if (Owner != null && Owner.EffectList.CountOfType<BannerOfBesiegingEffect>() > 0)
			{
				BannerOfBesiegingEffect eff = Owner.EffectList.GetOfType<BannerOfBesiegingEffect>();
				if (eff != null)
					delay = (int)(delay * (1 - 0.06 * eff.Effectiveness));

			}
			return delay;
		}

		public Boolean CanUse()
		{
			if (Owner == null)
				return false;
			Owner.Stealth(false);
			if (!Owner.IsAlive || Owner.IsMezzed || Owner.IsStunned)
			{
				this.Owner.Out.SendMessage("You can't use this siegeweapon now!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return false;
			}
			if (Health <= DecayedHp)
			{
				this.Owner.Out.SendMessage("The siegeweapon needs to be repaired!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				return false;
			}
			if (!this.IsWithinRadius(this.Owner, SIEGE_WEAPON_CONTROLE_DISTANCE))
			{
				Owner.Out.SendMessage("You are too far from your siege equipment to control it any longer!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}

			if (Owner.Realm != this.Realm)
			{
				Owner.Out.SendMessage($"This siege equipment is owned by an enemy realm!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}
				
			
			return true;
		}

		public void PreAction()
		{
			if (SiegeWeaponTimer.IsAlive)
			{
				SiegeWeaponTimer.Stop();
				if (Owner != null)
					Owner.Out.SendSiegeWeaponCloseInterface();
			}
			SiegeWeaponTimer.Start(GetActionDelay(SiegeWeaponTimer.CurrentAction));
			if (Owner != null)
			{
				if(this is GameSiegeRam) //Ram Siege Interface is 2 seconds fast for some reason. adding 2 seconds to the action delay for rams
					Owner.Out.SendSiegeWeaponInterface(this, (GetActionDelay(SiegeWeaponTimer.CurrentAction) + 2000) / 100);
				else
				Owner.Out.SendSiegeWeaponInterface(this, GetActionDelay(SiegeWeaponTimer.CurrentAction) / 100);
			}
			BroadcastAnimation();
		}

		private void StartControlRangeCheck()
		{
			if (ObjectState != eObjectState.Active)
				return;
			lock (m_controlRangeTimerLock)
			{
				if (m_controlRangeTimer == null)
				{
					m_controlRangeTimer = new EcsGameTimer(this);
					m_controlRangeTimer.Callback = new EcsGameTimer.EcsTimerCallback(ControlRangeTimerCallback);
				}
				else if (m_controlRangeTimer.IsAlive)
					return;
				m_controlRangeTimer.Start(1500);
			}
		}

		private void StopControlRangeCheck()
		{
			lock (m_controlRangeTimerLock)
			{
				if (m_controlRangeTimer == null)
					return;
				m_controlRangeTimer.Stop();
				m_controlRangeTimer = null;
			}
		}

		private int ControlRangeTimerCallback(EcsGameTimer callingTimer)
		{
			if(Owner==null)
			{
				StopControlRangeCheck();
				return 0;
			}
			if(this.GetDistanceTo(Owner)>SIEGE_WEAPON_CONTROLE_DISTANCE)
			{
				ReleaseControl();
				return 0;
			}
			return 1500;
		}
		#endregion
		#region override function
		public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
		{
			//todo check if bullet
			return base.ReceiveItem(source, item);
		}
		public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer)
			{
				damageAmount /= 30;
				criticalAmount /= 30;
			}
			base.TakeDamage(source, damageType, damageAmount, criticalAmount);
		}

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			if (Owner == player)
				return false;

			TakeControl(player);
			return true;
		}
		public override void LoadFromDatabase(DataObject obj)
		{
			base.LoadFromDatabase(obj);
			if (!(obj is DbItemTemplate)) return;
			DbItemTemplate item = (DbItemTemplate)obj;
            this.TranslationId = item.TranslationId;
			this.Name = item.Name;
            this.ExamineArticle = item.ExamineArticle;
            this.MessageArticle = item.MessageArticle;
			this.Model = (ushort)item.Model;
		}

		public bool EnableToMove
		{
			set { m_enableToMove = value; }
			get { return m_enableToMove; }
		}

		public override bool AddToWorld()
		{
			if (!base.AddToWorld()) return false;
			StartDecay();
			TimesRepaired = 0;
			return true;
		}
		public override bool RemoveFromWorld()
		{
			if (!base.RemoveFromWorld()) return false;
			StopDecay();
			return true;
		}

		#endregion
		#region decay
		public override void StartHealthRegeneration()
		{
			//don't regenerate health
		}

		private void StartDecay()
		{
			if (ObjectState != eObjectState.Active)
				return;
			lock (m_decayTimerLock)
			{
				if (m_decayTimer == null)
				{
					m_decayTimer = new EcsGameTimer(this);
					m_decayTimer.Callback = new EcsGameTimer.EcsTimerCallback(DecayTimerCallback);
				}
				else if (m_decayTimer.IsAlive)
					return;
				m_decayTimer.Start(DECAYPERIOD);
			}
		}

		private void StopDecay()
		{
			lock (m_decayTimerLock)
			{
				if (m_decayTimer == null)
					return;
				m_decayTimer.Stop();
				m_decayTimer = null;
			}
		}

		private int DecayTimerCallback(EcsGameTimer callingTimer)
		{
			TakeDamage(this, EDamageType.Natural, DecayDeductHp, 0);
			return DECAYPERIOD;
		}

		#endregion

		private static SpellLine m_SiegeSpellLine;
		public static SpellLine SiegeSpellLine
		{
			get
			{
				if (m_SiegeSpellLine == null)
					m_SiegeSpellLine = new SpellLine("SiegeSpellLine", "Siege Weapon Spells", "unknown", false);

				return m_SiegeSpellLine;
			}
		}
	}
	#endregion
	#region siegeTimer
	public class SiegeTimer : EcsGameTimerWrapperBase
	{
		public enum eAction : byte
		{
			None = 0x00,
			Aiming = 0x01,
			Arming = 0x02,
			Loading = 0x03,
			Fire = 0x04,
		}

		/// <summary>
		/// Constructs a new UseSlotAction
		/// </summary>
		/// <param name="siegeWeapon">The siege weapon</param>
		public SiegeTimer(GameSiegeWeapon siegeWeapon)
			: base(siegeWeapon)
		{
			m_siegeWeapon = siegeWeapon;
		}

		private eAction m_currentAction;
		private GameSiegeWeapon m_siegeWeapon;

		public eAction CurrentAction
		{
			get { return m_currentAction; }
			set { m_currentAction = value; }
		}
		public GameSiegeWeapon SiegeWeapon
		{
			get { return m_siegeWeapon; }
			set { m_siegeWeapon = value; }
		}

		protected override int OnTick(EcsGameTimer timer)
		{
			if (SiegeWeapon.Owner == null)
				return 0;
			switch (CurrentAction)
			{
				case eAction.Arming:
					{
						SiegeWeapon.Armed();
						break;
					}
				case eAction.Aiming:
					{
						SiegeWeapon.Aimed();
						break;
					}
				case eAction.Loading:
					{
						//todo set ammo
						break;
					}
				case eAction.Fire:
					{
						SiegeWeapon.DoDamage();
						break;
					}
				default: break;
			}

			if (SiegeWeapon.Owner != null)
			{
				SiegeWeapon.Owner.Out.SendSiegeWeaponInterface(this.SiegeWeapon, 0);
			}
			if ((SiegeWeapon.CurrentState & GameSiegeWeapon.eState.Armed) != GameSiegeWeapon.eState.Armed)
				SiegeWeapon.Arm();
			
			return 0;
		}
	}
	#endregion
}
/* messages:

You are too far from your siege equipment to control it any longer!
You can't salvage the trebuchet!
The trebuchet's target is too far away to reach!
Your target has moved out of range!
You put the ice ball into the field catapult.
The trebuchet is fully repaired!
That object isn't carryable...
*/