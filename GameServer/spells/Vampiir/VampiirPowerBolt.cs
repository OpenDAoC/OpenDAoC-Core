using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.VampiirBolt)]
	public class VampiirBoltSpellHandler : SpellHandler
	{
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (Caster.InCombat == true)
			{
				MessageToCaster("You cannot cast this spell in combat!", eChatType.CT_SpellResisted);
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

		protected class BoltOnTargetAction : ECSGameTimerWrapperBase
		{
			protected readonly GameLiving m_boltTarget;
			protected readonly VampiirBoltSpellHandler m_handler;

			public BoltOnTargetAction(GameLiving actionSource, GameLiving boltTarget, VampiirBoltSpellHandler spellHandler) : base(actionSource)
			{
				if (boltTarget == null)
					throw new ArgumentNullException("boltTarget");
				if (spellHandler == null)
					throw new ArgumentNullException("spellHandler");
				m_boltTarget = boltTarget;
				m_handler = spellHandler;
			}

			protected override int OnTick(ECSGameTimer timer)
			{
				GameLiving target = m_boltTarget;
				GameLiving caster = (GameLiving) timer.Owner;

				if (target == null || target.CurrentRegionID != caster.CurrentRegionID || target.ObjectState != GameObject.eObjectState.Active || !target.IsAlive)
					return 0;

				int power ;

				if (target is GameNPC || target.Mana > 0)
				{
					if (target is GameNPC)
						power = (int) Math.Round(target.Level * (double) m_handler.Spell.Value * 2 / 100);
					else 
						power = (int) Math.Round(target.MaxMana * ((double) m_handler.Spell.Value / 250));

					if (target.Mana < power)
						power = target.Mana;

					caster.Mana += power;

					if (target is GamePlayer)
					{
						target.Mana -= power;
						((GamePlayer)target).Out.SendMessage(caster.Name + " takes " + power + " power!", eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow);
					}

					if (caster is GamePlayer)
					{
						((GamePlayer)caster).Out.SendMessage("You receive " + power + " power from " + target.Name + "!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
					}
				}
				else
					((GamePlayer)caster).Out.SendMessage("You did not receive any power from " + target.Name + "!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

				//Place the caster in combat
				if (target is GamePlayer)
					caster.LastAttackTickPvP = caster.CurrentRegion.Time;
				else
					caster.LastAttackTickPvE = caster.CurrentRegion.Time;
				
				//create the attack data for the bolt
				AttackData ad = new AttackData();
				ad.Attacker = caster;
				ad.Target = target;
				ad.DamageType = eDamageType.Heat;
				ad.AttackType = AttackData.eAttackType.Spell;
				ad.AttackResult = eAttackResult.HitUnstyled;
				ad.SpellHandler = m_handler;
				target.OnAttackedByEnemy(ad);
				
				target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, caster);

				return 0;
			}
		}

		public VampiirBoltSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
