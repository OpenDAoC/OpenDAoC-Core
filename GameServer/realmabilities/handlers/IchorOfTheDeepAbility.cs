using System;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.Events;
using DOL.Database;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
	public class IchorOfTheDeepAbility : TimedRealmAbility
	{
		public IchorOfTheDeepAbility(DBAbility dba, int level) : base(dba, level) { }

		private ECSGameTimer m_expireTimerID;
		private ECSGameTimer m_rootExpire;
		private int dmgValue = 0;
		private int duration = 0;
		private GamePlayer caster;

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			caster = living as GamePlayer;
			if (caster == null)
				return;

			// Player must have a target
			if (caster.TargetObject == null)
			{
				caster.Out.SendMessage("You must select a target for this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				caster.DisableSkill(this, 3 * 1000);
				return;
			}

			var target = caster.TargetObject as GameLiving;

			// So they can't use Admins or objects as a target
			if (target == null || !GameServer.ServerRules.IsAllowedToAttack(caster, target, true))
			{
				caster.Out.SendMessage("You have an invalid target!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				caster.DisableSkill(this, 3 * 1000);
				return;
			}

			// Can't target self
			if (caster == target)
			{
				caster.Out.SendMessage("You can't attack yourself!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				caster.DisableSkill(this, 3 * 1000);
				return;
			}

			// Target must be in front of the Player
			if (!caster.IsObjectInFront(target, 150))
			{
				caster.Out.SendMessage(target.Name + " is not in view!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				caster.DisableSkill(this, 3 * 1000);
				return;
			}

			// Target must be alive
			if (!target.IsAlive)
			{
				caster.Out.SendMessage(target.Name + " is dead!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				caster.DisableSkill(this, 3 * 1000);
				return;
			}

			// Target must be within range
			if (!caster.IsWithinRadius(caster.TargetObject, 1875))
			{
				caster.Out.SendMessage(caster.TargetObject.Name + " is too far away!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				caster.DisableSkill(this, 3 * 1000);
				return;
			}

			// Target cannot be an ally or friendly
			if (caster != target && caster.Realm == target.Realm)
			{
				caster.Out.SendMessage("You can't attack a member of your realm!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				caster.DisableSkill(this, 3 * 1000);
				return;
			}

			// Cannot use ability if timer is not expired
			if (m_expireTimerID != null && m_expireTimerID.IsAlive)
			{
				caster.Out.SendMessage("You must wait" + m_expireTimerID.TimeUntilElapsed / 1000 + " seconds to recast this type of ability!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				caster.DisableSkill(this, 3 * 1000);
				return;
			}

			/*
			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				switch (Level)
				{
						case 1: dmgValue = 150; duration = 10000; break;
						case 2: dmgValue = 275; duration = 15000; break;
						case 3: dmgValue = 400; duration = 20000; break;
						case 4: dmgValue = 500; duration = 25000; break;
						case 5: dmgValue = 600; duration = 30000; break;
						default: return;
				}				
			}
				//150 dam/10 sec || 400/20  || 600/30
				switch (Level)
				{
						case 1: dmgValue = 150; duration = 10000; break;
						case 2: dmgValue = 400; duration = 20000; break;
						case 3: dmgValue = 600; duration = 30000; break;
						default: return;
				}
				*/

			// Do the effect and damage if all went well... not sure why this is a timer
			//m_expireTimerID = new ECSGameTimer(caster, new ECSGameTimer.ECSTimerCallback(EndCast), 1);
			//m_expireTimerID.Start();
			EndCast();
		}

		protected virtual int EndCast()
		{
			GameLiving living = caster.TargetObject as GameLiving;

			foreach (GamePlayer i_player in caster.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
			{
				if (i_player == caster)
					i_player.MessageToSelf("You cast " + this.Name + "!", eChatType.CT_Spell);
				else
					i_player.Out.SendMessage(caster.Name + " casts a spell!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
			}

			/*if (living = caster && living.Realm != caster.Realm)
			{
				IchorEffect(living, living);
			}
			else
			{
				timer.Stop();
				timer = null;
				return 0;
			}*/

			// Hit all non-friendly mobs in radius, including the target
			foreach (GameNPC mob in living.GetNPCsInRadius(500))
			{
				IchorEffect(living, mob);
			}

			// Do everything for GamePlayer now
			foreach (GamePlayer aeplayer in living.GetPlayersInRadius(500))
			{
				IchorEffect(living, aeplayer);
			}

			DisableSkill(caster);
			return 0;
		}

		private int CalculateDamageWithFalloff(int initialDamage, GameLiving initTarget, GameLiving aetarget)
		{
			Console.WriteLine($"initial {initialDamage} caster {initTarget} target {aetarget}");
			int modDamage = (int)Math.Round((decimal) (initialDamage * ((500-(initTarget.GetDistance(new Point2D(aetarget.X, aetarget.Y)))) / 500.0)));
			Console.WriteLine($"distance {((500-(initTarget.GetDistance(new Point2D(aetarget.X, aetarget.Y)))) / 500.0)} Mod {modDamage}");
			return modDamage;
		}

		protected virtual int RootExpires(ECSGameTimer timer)
		{
			GameLiving living = timer.TimerOwner as GameLiving;
			if (living != null)
			{
				living.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, this);
				SendUpdates(living);
			}
			timer.Stop();
			timer = null;
			return 0;
		}
		/// <summary>
		/// Sends updates on effect start/stop
		/// </summary>
		/// <param name="owner"></param>
		protected static void SendUpdates(GameLiving owner)
		{
			if (owner.IsMezzed || owner.IsStunned)
				return;

			GamePlayer player = owner as GamePlayer;
			if (player != null)
				player.Out.SendUpdateMaxSpeed();

			GameNPC npc = owner as GameNPC;
			if (npc != null)
			{
				short maxSpeed = npc.MaxSpeed;
				if (npc.CurrentSpeed > maxSpeed)
					npc.CurrentSpeed = maxSpeed;
			}
		}
		/// <summary>
		/// Handles attack on buff owner
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected virtual void OnAttacked(DOLEvent e, object sender, EventArgs arguments)
		{
			AttackedByEnemyEventArgs attackArgs = arguments as AttackedByEnemyEventArgs;
			GameLiving living = sender as GameLiving;
			if (attackArgs == null) return;
			if (living == null) return;

			switch (attackArgs.AttackData.AttackResult)
			{
				case eAttackResult.HitStyle:
				case eAttackResult.HitUnstyled:
					living.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, this);
					SendUpdates(living);
					break;
			}
		}

		protected void IchorEffect(GameLiving centerTarget, GameLiving aoeTarget)
		{
			var living = centerTarget;
			var target = aoeTarget;

			if (living == null || target == null)
				return;

			dmgValue = 400;
			duration = 20000;

			#region Resists and Determination
			var primaryResistModifier = target.GetResist(eDamageType.Spirit);
			var secondaryResistModifier = target.SpecBuffBonusCategory[(int)eProperty.Resist_Spirit];
			var rootdet = ((target.GetModified(eProperty.SpeedDecreaseDurationReduction) - 100) * -1);

			var resistModifier = 0;
			resistModifier += (int)((dmgValue * (double)primaryResistModifier) * -0.01);
			resistModifier += (int)((dmgValue + (double)resistModifier) * (double)secondaryResistModifier * -0.01);

			if (target is GamePlayer)
				dmgValue += resistModifier;
			else if (target is GameNPC)
				dmgValue += resistModifier;

			var rootmodifier = 0;
			rootmodifier += (int)((duration * (double)primaryResistModifier) * -0.01);
			rootmodifier += (int)((duration + (double)primaryResistModifier) * (double)secondaryResistModifier * -0.01);
			rootmodifier += (int)((duration + (double)rootmodifier) * (double)rootdet * -0.01);

			duration += rootmodifier;

			if (duration < 1)
				duration = 1;
			#endregion Resists and Determination

			// Ignore friendly players
			if (target.Realm == caster.Realm || target == caster)
				return;

			if (!GameServer.ServerRules.IsAllowedToAttack(caster, target, true))
				return;

			//GameSpellEffect mez = SpellHandler.FindEffectOnTarget(aeplayer, "Mesmerize");
			ECSGameEffect mez = EffectListService.GetEffectOnTarget(target, eEffect.Mez);
			if (mez != null)
				EffectService.RequestCancelEffect(mez);
				//mez.Cancel(false);

			// Falloff damage
			int dmgWithFalloff = CalculateDamageWithFalloff(dmgValue, living, target);

			target.TakeDamage(caster, eDamageType.Spirit, dmgWithFalloff, 0);
			target.StartInterruptTimer(3000, AttackData.eAttackType.Spell, caster);

			// Spell damage messages
			caster.Out.SendMessage("You hit " + target.GetName(0, false) + " for " + dmgWithFalloff + " damage!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
			// Display damage message to target if any damage is actually caused
			if (dmgWithFalloff > 0 && target is GamePlayer gpTarget)
				gpTarget.Out.SendMessage(caster.Name + " hits you for " + dmgWithFalloff + " damage!", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);

			// Make sure they're not using SoS (needs fixing), Charge, or in Shade form
			var targetCharge = EffectListService.GetEffectOnTarget(target, eEffect.Charge);
			var targetShade = EffectListService.GetEffectOnTarget(target, eEffect.Shade);
			var targetSoS = EffectListService.GetEffectOnTarget(target, eEffect.SpeedOfSound);
			if (targetCharge == null && targetSoS == null && targetShade == null)
			{
				// Send spell message to player if applicable
				if (target is GamePlayer gpMessage)
					gpMessage.Out.SendMessage("Constricting bonds surround your body!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

				// Apply the snare
				target.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, this, 1.0 - 99 * 0.01);
				m_rootExpire = new ECSGameTimer(target, new ECSGameTimer.ECSTimerCallback(RootExpires), duration);
				GameEventMgr.AddHandler(target, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttacked));
				SendUpdates(target);

				// Send root animation and spell message
				foreach (GamePlayer player in living.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					player.Out.SendSpellEffectAnimation(caster, target, 7029, 0, false, 1);

					if (player.IsWithinRadius(target, WorldMgr.INFO_DISTANCE) && player != target)
						player.Out.SendMessage(target.GetName(0, false) + " is surrounded by constricting bonds!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}
			}
			else
				// Send resist animation if they cannot be rooted
				foreach (GamePlayer player in living.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					player.Out.SendSpellEffectAnimation(caster, target, 7029, 0, false, 0);

		}

		public override int GetReUseDelay(int level)
		{
			return 600;
		}
	}
}
