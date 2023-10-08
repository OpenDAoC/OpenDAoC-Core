/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler("VampiirBolt")]
	public class VampiirBoltSpellHandler : SpellHandler
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

		public VampiirBoltSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
