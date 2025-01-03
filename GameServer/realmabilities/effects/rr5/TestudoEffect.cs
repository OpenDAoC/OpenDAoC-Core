using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
	/// <summary>
	/// Mastery of Concentration
	/// </summary>
	public class TestudoEffect : TimedEffect
	{
		private GameLiving owner;

		public TestudoEffect()
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
			GameEventMgr.AddHandler(target, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));
			GameEventMgr.AddHandler(target, GameLivingEvent.AttackFinished, new DOLEventHandler(attackEventHandler));
			owner.OnMaxSpeedChange();
		}

		private void OnAttack(DOLEvent e, object sender, EventArgs arguments)
		{
			GameLiving living = sender as GameLiving;
			if (living == null) return;
			DbInventoryItem shield = living.ActiveLeftWeapon;
			if (shield == null)
				return;
			if (shield.Object_Type != (int)eObjectType.Shield)
				return;
			if (living.TargetObject == null)
				return;
			if (living.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
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
				((GamePlayer)living).Out.SendMessage("Your Testudo Stance reduces the damage by " + (absorb+critic) + " points", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
			if (ad.Attacker is GamePlayer)
				((GamePlayer)ad.Attacker).Out.SendMessage(living.Name + "'s Testudo Stance reducec your damage by " + (absorb+critic) + " points", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

		}

		protected void attackEventHandler(DOLEvent e, object sender, EventArgs args)
		{
			if (args == null) return;
			AttackFinishedEventArgs ag = args as AttackFinishedEventArgs;
			if (ag == null) return;
			if (ag.AttackData == null) return;
			switch (ag.AttackData.AttackResult)
			{
				case eAttackResult.Blocked:
				case eAttackResult.Evaded:
				case eAttackResult.Fumbled:
				case eAttackResult.HitStyle:
				case eAttackResult.HitUnstyled:
				case eAttackResult.Missed:
				case eAttackResult.Parried:
					Stop(); break;
			}

		}

		public override void Stop()
		{
			GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));
			GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackFinished, new DOLEventHandler(attackEventHandler));
			base.Stop();
			owner.OnMaxSpeedChange();
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
