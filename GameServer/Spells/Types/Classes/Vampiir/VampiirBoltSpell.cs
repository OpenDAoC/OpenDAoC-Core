using System;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
	[SpellHandler("VampiirBolt")]
	public class VampiirBoltSpell : SpellHandler
	{
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (Caster.InCombat == true)
			{
				MessageToCaster("You cannot cast this spell in combat!", EChatType.CT_SpellResisted);
				return false;
			}
			return base.CheckBeginCast(selectedTarget);
		}
		public override bool StartSpell(GameLiving target)
		{
			foreach (GameLiving targ in SelectTargets(target))
			{
				DealDamage(targ);
			}

			return true;
		}

		private void DealDamage(GameLiving target)
		{
			int ticksToTarget = m_caster.GetDistanceTo(target) * 100 / 85; // 85 units per 1/10s
			int delay = 1 + ticksToTarget / 100;
			foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSpellEffectAnimation(m_caster, target, m_spell.ClientEffect, (ushort)(delay), false, 1);
			}
			BoltOnTargetAction bolt = new BoltOnTargetAction(Caster, target, this);
			bolt.Start(1 + ticksToTarget);
		}

		public override void FinishSpellCast(GameLiving target)
		{
			if (target is Keeps.GameKeepDoor || target is Keeps.GameKeepComponent)
			{
				MessageToCaster("Your spell has no effect on the keep component!", EChatType.CT_SpellResisted);
				return;
			}
			base.FinishSpellCast(target);
		}

		protected class BoltOnTargetAction : EcsGameTimerWrapperBase
		{
			protected readonly GameLiving m_boltTarget;
			protected readonly VampiirBoltSpell m_handler;

			public BoltOnTargetAction(GameLiving actionSource, GameLiving boltTarget, VampiirBoltSpell spellHandler) : base(actionSource)
			{
				if (boltTarget == null)
					throw new ArgumentNullException("boltTarget");
				if (spellHandler == null)
					throw new ArgumentNullException("spellHandler");
				m_boltTarget = boltTarget;
				m_handler = spellHandler;
			}

			protected override int OnTick(EcsGameTimer timer)
			{
				GameLiving target = m_boltTarget;
				GameLiving caster = (GameLiving) timer.Owner;

				if (target == null || target.CurrentRegionID != caster.CurrentRegionID || target.ObjectState != GameObject.eObjectState.Active || !target.IsAlive)
					return 0;

				int power ;

				if (target is GameNpc || target.Mana > 0)
				{
					if (target is GameNpc)
						power = (int) Math.Round(target.Level * (double) m_handler.Spell.Value * 2 / 100);
					else 
						power = (int) Math.Round(target.MaxMana * ((double) m_handler.Spell.Value / 250));

					if (target.Mana < power)
						power = target.Mana;

					caster.Mana += power;

					if (target is GamePlayer)
					{
						target.Mana -= power;
						((GamePlayer)target).Out.SendMessage(caster.Name + " takes " + power + " power!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
					}

					if (caster is GamePlayer)
					{
						((GamePlayer)caster).Out.SendMessage("You receive " + power + " power from " + target.Name + "!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					}
				}
				else
					((GamePlayer)caster).Out.SendMessage("You did not receive any power from " + target.Name + "!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);

				//Place the caster in combat
				if (target is GamePlayer)
					caster.LastAttackTickPvP = caster.CurrentRegion.Time;
				else
					caster.LastAttackTickPvE = caster.CurrentRegion.Time;
				
				//create the attack data for the bolt
				AttackData ad = new AttackData();
				ad.Attacker = caster;
				ad.Target = target;
				ad.DamageType = EDamageType.Heat;
				ad.AttackType = EAttackType.Spell;
				ad.AttackResult = EAttackResult.HitUnstyled;
				ad.SpellHandler = m_handler;
				target.OnAttackedByEnemy(ad);
				
				target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, caster);

				return 0;
			}
		}

		public VampiirBoltSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
