using System;
using System.Collections.Generic;
using DOL.Events;

namespace DOL.GS.Effects
{
	/// <summary>
	/// Mastery of Concentration
	/// </summary>
	public class EntwiningSnakesEffect : TimedEffect
	{
		private GameLiving owner;


		public EntwiningSnakesEffect()
			: base(20000)
		{

		}

		public override void Start(GameLiving target)
		{
			base.Start(target);
			target.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, this, 1.0 - 50 * 0.01);
			owner = target;
			GameEventMgr.AddHandler(target, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttacked));
			owner.OnMaxSpeedChange();
		}

		public override void Stop()
		{
			owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, this);
			base.Stop();
			GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttacked));
			owner.OnMaxSpeedChange();
		}

		protected virtual void OnAttacked(DOLEvent e, object sender, EventArgs arguments)
		{
			AttackedByEnemyEventArgs attackArgs = arguments as AttackedByEnemyEventArgs;
			if (attackArgs == null) return;
			switch (attackArgs.AttackData.AttackResult)
			{
				case eAttackResult.HitStyle:
				case eAttackResult.HitUnstyled:
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
