using System;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;

namespace Core.GS.Effects
{
	public class NfRaTestudoEffect : TimedEffect
	{
		private GameLiving owner;

		public NfRaTestudoEffect()
			: base(45000)
		{
		}

		public override void Start(GameLiving target)
		{
			base.Start(target);
			owner = target;
			GamePlayer player = target as GamePlayer;
			if (player != null)
			{
				foreach (GamePlayer p in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					p.Out.SendSpellEffectAnimation(player, player, Icon, 0, false, 1);
				}
			}

			target.attackComponent.StopAttack();
			GameEventMgr.AddHandler(target, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
			GameEventMgr.AddHandler(target, GameLivingEvent.AttackFinished, new CoreEventHandler(attackEventHandler));
			if (player != null)
			{
				player.Out.SendUpdateMaxSpeed();
			}
			else
			{
				owner.CurrentSpeed = owner.MaxSpeed;
			}
		}

		private void OnAttack(CoreEvent e, object sender, EventArgs arguments)
		{
			GameLiving living = sender as GameLiving;
			if (living == null) return;
			DbInventoryItem shield = living.Inventory.GetItem(EInventorySlot.LeftHandWeapon);
			if (shield == null)
				return;
			if (shield.Object_Type != (int)EObjectType.Shield)
				return;
			if (living.TargetObject == null)
				return;
			if (living.ActiveWeaponSlot == EActiveWeaponSlot.Distance)
				return;
			if (living.ActiveWeapon.Hand == 1)
				return;
			AttackedByEnemyEventArgs attackedByEnemy = arguments as AttackedByEnemyEventArgs;
			AttackData ad = null;
			if (attackedByEnemy != null)
				ad = attackedByEnemy.AttackData;
			if (ad.Attacker.Realm == 0)
				return;

			if (ad.Damage < 1)
				return;

			int absorb = (int)(ad.Damage * 0.9);
			int critic = (int)(ad.CriticalDamage * 0.9);
			ad.Damage -= absorb;
			ad.CriticalDamage -= critic;
			if (living is GamePlayer)
				((GamePlayer)living).Out.SendMessage("Your Testudo Stance reduces the damage by " + (absorb+critic) + " points", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
			if (ad.Attacker is GamePlayer)
				((GamePlayer)ad.Attacker).Out.SendMessage(living.Name + "'s Testudo Stance reducec your damage by " + (absorb+critic) + " points", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);

		}

		protected void attackEventHandler(CoreEvent e, object sender, EventArgs args)
		{
			if (args == null) return;
			AttackFinishedEventArgs ag = args as AttackFinishedEventArgs;
			if (ag == null) return;
			if (ag.AttackData == null) return;
			switch (ag.AttackData.AttackResult)
			{
				case EAttackResult.Blocked:
				case EAttackResult.Evaded:
				case EAttackResult.Fumbled:
				case EAttackResult.HitStyle:
				case EAttackResult.HitUnstyled:
				case EAttackResult.Missed:
				case EAttackResult.Parried:
					Stop(); break;
			}

		}

		public override void Stop()
		{
			GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
			GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackFinished, new CoreEventHandler(attackEventHandler));
			base.Stop();
			GamePlayer player = owner as GamePlayer;
			if (player != null)
			{
				player.Out.SendUpdateMaxSpeed();
			}
			else
			{
				owner.CurrentSpeed = owner.MaxSpeed;
			}
		}

		public override string Name { get { return "Testudo"; } }

		public override ushort Icon { get { return 3067; } }


		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Warrior with shield equipped covers up and takes 90% less damage for all attacks for 45 seconds. Can only move at reduced speed (speed buffs have no effect) and cannot attack. Using a style will break testudo form. This ability is only effective versus realm enemies.");
				return list;
			}
		}
	}
}