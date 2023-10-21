using System;
using System.Collections.Generic;
using DOL.Events;

namespace DOL.GS.Effects
{
	public class NfRaBladeBarrierEffect : TimedEffect
	{
		public NfRaBladeBarrierEffect()
			: base(30000)
		{
			;
		}

		public override void Start(GameLiving target)
		{
			base.Start(target);

			foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				p.Out.SendSpellEffectAnimation(target, target, 7055, 0, false, 1);
			}
            //Commented out for removal: Parry Chance for BladeBarrier is hardcoded in GameLiving.cs in the CalculateEnemyAttackResult method
			//m_owner.BuffBonusCategory4[(int)eProperty.ParryChance] += 90;
			GameEventMgr.AddHandler(target, GameLivingEvent.AttackFinished, new CoreEventHandler(attackEventHandler));
            GameEventMgr.AddHandler(target, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(TakeDamage));

		}

        //[StephenxPimentel]
        //1.108 All Damage Recieved while this effect is active is reduced by 25%.
        public void TakeDamage(CoreEvent e, object sender, EventArgs args)
        {
            if (sender is GameLiving)
            {
                GameLiving living = sender as GameLiving;
                AttackedByEnemyEventArgs eDmg = args as AttackedByEnemyEventArgs;

                if (!living.HasEffect(typeof(NfRaBladeBarrierEffect)))
                {
                    GameEventMgr.RemoveHandler(GameLivingEvent.AttackedByEnemy, TakeDamage);
                    return;
                }

                eDmg.AttackData.Damage -= ((eDmg.AttackData.Damage * 25) / 100);
                eDmg.AttackData.CriticalDamage -=  ((eDmg.AttackData.CriticalDamage * 25) / 100);
                eDmg.AttackData.StyleDamage -= ((eDmg.AttackData.StyleDamage * 25) / 100);
            }
        }
		protected void attackEventHandler(CoreEvent e, object sender, EventArgs args)
		{
			if (args == null) return;
			AttackFinishedEventArgs ag = args as AttackFinishedEventArgs;
			if (ag == null) return;
			if (ag.AttackData == null) return;
			if (ag.AttackData.Attacker != Owner) return;
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

		public override string Name { get { return "Blade Barrier"; } }

		public override ushort Icon { get { return 3054; } }

		public override void Stop()
		{
            //Commented out for removal
			//m_owner.BuffBonusCategory4[(int)eProperty.ParryChance] -= 90;
			base.Stop();
		}

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Grants 90% Parry chance which is broken by an attack");
				return list;
			}
		}
	}
}