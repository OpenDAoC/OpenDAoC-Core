using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Keeps;
using Core.GS.PacketHandler;

namespace Core.GS
{
	public class GameSiegeRam : GameSiegeWeapon
	{
		public GameSiegeRam()
			: base()
		{
			MeleeDamageType = EDamageType.Crush;
			Name = "siege ram";

			//AmmoType = 0x3B00;
			//this.Effect = 0x8A1;
			AmmoType = 0x26;
			this.Model = 0xA2A;//0xA28
			//TODO find all value for ram
			ActionDelay = new int[]
			{
				0,//none
				5000,//aiming
				10000,//arming
				0,//loading
				1100//fireing
			};//en ms
		}

		//Set the maxium rams allowed to attack a target at the same time.
		private const int MAX_RAMS_ATTACKING_TARGET = 2;
		public override ushort Type()
		{
			return 0x9602;
		}



		public override int MAX_PASSENGERS
		{
			get
			{
				switch (Level)
				{
					case 0:
						return 2;
					case 1:
						return 6;
					case 2:
						return 8;
					case 3:
						return 12;
				}
				return Level * 3;
			}
		}

		public override int SLOT_OFFSET
		{
			get
			{
				return 1;
			}
		}

		public override void Aim()
		{
			if (Owner.TargetObject == null) return;
			//Only allow rams to attack keep or relic doors 
			if (!(Owner.TargetObject is GameKeepDoor) && !(Owner.TargetObject is GameRelicDoor))
			{
				Owner.Out.SendMessage("Rams can only attack doors!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			//Range Check
			if (!this.IsWithinRadius(Owner.TargetObject, AttackRange))
			{
				if(Owner != null)
					Owner.Out.SendMessage("You are too far away to attack " + Owner.TargetObject.Name, EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			//Limit 2 Rams aimed at door at a time
			//Check # of rams on the target
			int ramsAimedAtTarget=0;
			foreach (GameNpc npc in TargetObject.GetNPCsInRadius(600))
			{
				if(npc is GameSiegeRam ram)
				{
					if (ram != this && ram.TargetObject == Owner.TargetObject)
					{
						ramsAimedAtTarget++;
					}
				}
			}

			if (ramsAimedAtTarget >= MAX_RAMS_ATTACKING_TARGET)
			{
				if(Owner != null)
					Owner.Out.SendMessage("Too many rams already attacking   " + TargetObject?.Name, EChatType.CT_System,EChatLoc.CL_SystemWindow);
				return;
			}

			base.Aim();

		}

		public override void Fire()
		{
			GameLiving target = (TargetObject as GameLiving);
			if(target != null && !target.IsAlive)
			{

				if(Owner != null)
					Owner.Out.SendMessage(target.Name + " is already destroyed!" , EChatType.CT_System,EChatLoc.CL_SystemWindow);
				return;
			}
			
			base.Fire();

		}

		public override void DoDamage()
		{
			GameLiving target = (TargetObject as GameLiving);
			if (target == null)
			{
				if(Owner != null)
					Owner.Out.SendMessage("Select a target first.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			//Only allow rams to attack keep or relic doors 
			if (!(target is GameKeepDoor) && !(target is GameRelicDoor))
			{
				if(Owner != null)
					Owner.Out.SendMessage("Rams can only attack doors!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			//todo good  distance check
			if (!this.IsWithinRadius(target, AttackRange))
			{
				if(Owner != null)
					Owner.Out.SendMessage("You are too far away to attack " + target.Name, EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			int damageAmount = CalcDamageToTarget(target);

			AttackData ad = new AttackData();
			ad.Attacker = this;
			ad.Target = target;
			ad.AttackType = EAttackType.Ranged;
			ad.AttackResult = EAttackResult.HitUnstyled;
			ad.Damage = damageAmount;
			ad.DamageType = MeleeDamageType;
			
			target.TakeDamage(this, EDamageType.Crush, damageAmount, 0);
			target.OnAttackedByEnemy(ad);
		
			if(Owner != null)
			{
				Owner.OnAttackEnemy(ad);
				Owner.Out.SendMessage("The " + this.Name + " hits " + target.Name + " for " + damageAmount + " damage!", EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
				MessageUtil.SystemToArea(this, GetName(0, false) + " hits " + target.GetName(0, true), EChatType.CT_OthersCombat, Owner);
			}
			base.DoDamage();
		}

		public override bool RiderMount(GamePlayer rider, bool forced)
		{
			if (!base.RiderMount(rider, forced))
				return false;
			UpdateRamStatus();
			return true;
		}

		public override bool RiderDismount(bool forced, GamePlayer player)
		{
			if (!base.RiderDismount(forced, player))
				return false;
			if (player.SiegeWeapon == this)
				ReleaseControl();
			UpdateRamStatus();
			return true;
		}

		public override void ReleaseControl()
		{
			TargetObject=null; //reset aimed object when released. Prevent empty/bugged rams from taking space on the ram limit per door.
			CurrentState &= ~eState.Aimed;
			
			base.ReleaseControl();
			foreach (GamePlayer player in CurrentRiders)
				player.DismountSteed(true);
		}

		public void UpdateRamStatus()
		{
			//speed of reload/arming changed by number of riders
			ActionDelay[2] = GetReloadDelay;
		}

		private int GetReloadDelay
		{
			get
			{
				//custom formula
				return 10000 + ((Level + 1) * 2000) - (int)(10000 * ((double)CurrentRiders.Length / (double)MAX_PASSENGERS));
			}
		}

		public override int CalcDamageToTarget(GameLiving target)
		{
			//return BaseDamage + (int)(((double)BaseDamage / 2.0) * (double)((double)CurrentRiders.Length / (double)MAX_PASSENGERS));
			return BaseDamage + (BaseDamage/2 * CurrentRiders.Length);
		}

		public override int BaseDamage
		{
			get
			{
				int damageAmount = 0;
				switch (Level)
				{
					case 0:
						//damageAmount = 200;
						damageAmount = 100;
						break;
					case 1:
						//damageAmount = 300;
						damageAmount = 125;
						break;
					case 2:
						//damageAmount = 450;
						damageAmount = 150;
						break;
					case 3:
						//damageAmount = 750;
						damageAmount = 200;
						break;
				}
				return damageAmount;
			}
		}

		public override int AttackRange
		{
			get
			{
				switch (Level)
				{
					case 0: return 300;
					case 1: return 400;
					case 2:
					case 3: return 500;
					default: return 500;
				}
			}
		}

		public override short MaxSpeed
		{
			get
			{
				double speed = 10.0 + 5.0 * Level + 50.0 * CurrentRiders.Length / MAX_PASSENGERS;

				foreach (GamePlayer player in CurrentRiders)
				{
					RealmAbilities.RaPropertyEnhancer ab = player.GetAbility<RealmAbilities.OfRaLifterAbility>();

					if (ab != null)
						speed *= 1 + ab.Amount / 100;
				}

				return (short) speed;
			}
		}
	}
}
