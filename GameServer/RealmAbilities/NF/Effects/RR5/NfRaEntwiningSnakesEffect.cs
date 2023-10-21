using System;
using System.Collections.Generic;
using Core.Events;

namespace Core.GS.Effects
{
	public class NfRaEntwiningSnakesEffect : TimedEffect
	{
		private GameLiving owner;

		public NfRaEntwiningSnakesEffect()
			: base(20000)
		{
		}

		public override void Start(GameLiving target)
		{
			base.Start(target);
			target.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, this, 1.0 - 50 * 0.01);
			owner = target;
			GamePlayer player = owner as GamePlayer;
			GameEventMgr.AddHandler(target, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));
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
			base.Stop();
			GamePlayer player = owner as GamePlayer;
			GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));
			if (player != null)
			{
				player.Out.SendUpdateMaxSpeed();
			}
			else if (owner.CurrentSpeed > owner.MaxSpeed)
			{
				owner.CurrentSpeed = owner.MaxSpeed;
			}
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

		public override string Name { get { return "Entwining Snakes"; } }

		public override ushort Icon { get { return 3071; } }

		public int SpellEffectiveness
		{
			get { return 0; }
		}

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("A breakable 50 % snare with 20 seconds duration");
				return list;
			}
		}
	}
}