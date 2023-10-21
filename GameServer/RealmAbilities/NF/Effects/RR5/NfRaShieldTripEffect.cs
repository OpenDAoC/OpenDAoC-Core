using System;
using System.Collections.Generic;
using Core.Events;
using Core.GS.Enums;

namespace Core.GS.Effects
{
	public class NfRaShieldTripDisarmEffect : TimedEffect
	{
		public NfRaShieldTripDisarmEffect()
			: base(15000)
		{
			;
		}

		private GameLiving owner;

		public override void Start(GameLiving target)
		{
			base.Start(target);
			owner = target;
			//target.IsDisarmed = true;
            target.DisarmedTime = target.CurrentRegion.Time + m_duration;
			target.attackComponent.StopAttack();

		}

		public override string Name { get { return "Shield Trip"; } }

		public override ushort Icon { get { return 3045; } }

		public override void Stop()
		{
			//owner.IsDisarmed = false;
			base.Stop();
		}

		public int SpellEffectiveness
		{
			get { return 100; }
		}

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Disarms you for 15 seconds!");
				return list;
			}
		}
	}

	public class NfRaShieldTripRootEffect : TimedEffect
	{
		private GameLiving owner;


		public NfRaShieldTripRootEffect()
			: base(10000)
		{
		}

		public override void Start(GameLiving target)
		{
			base.Start(target);
			target.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, this, 1.0 - 99 * 0.01);
			owner = target;
			GameEventMgr.AddHandler(target, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));
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

		public override void Stop()
		{
			owner.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, this);
			GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));
			GamePlayer player = owner as GamePlayer;
			if (player != null)
			{
				player.Out.SendUpdateMaxSpeed();
			}
			else
			{
				owner.CurrentSpeed = owner.MaxSpeed;
			}
			base.Stop();
		}

		protected virtual void OnAttacked(CoreEvent e, object sender, EventArgs arguments)
		{
			AttackedByEnemyEventArgs attackArgs = arguments as AttackedByEnemyEventArgs;
			if (attackArgs == null) return;
			switch (attackArgs.AttackData.AttackResult)
			{
				case EAttackResult.HitStyle:
				case EAttackResult.HitUnstyled:
					Stop();
					break;
			}

		}

		public override string Name { get { return "Shield Trip"; } }

		public override ushort Icon { get { return 7046; } }

		public int SpellEffectiveness
		{
			get { return 0; }
		}

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Root Effect");
				return list;
			}
		}
	}

}