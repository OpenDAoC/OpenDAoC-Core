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
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Events;
using DOL.GS.RealmAbilities;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Spell handler for speed decreasing spells
	/// </summary>
	[SpellHandler("SpeedDecrease")]
	public class SpeedDecreaseSpellHandler : UnbreakableSpeedDecreaseSpellHandler
	{
		private bool crit = false;
		public override void CreateECSEffect(ECSGameEffectInitParams initParams)
		{
			if (crit)
				initParams.Effectiveness *= 2; //critical hit effectiveness needs to be set after duration is calculated to prevent double duration
			new StatDebuffECSEffect(initParams);
		}

        /// <summary>
        /// Apply the effect.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectiveness"></param>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
		{
			// Check for root immunity.
			if (Spell.Value == 99 && target.effectListComponent.Effects.ContainsKey(eEffect.SnareImmunity))
				//FindStaticEffectOnTarget(target, typeof(MezzRootImmunityEffect)) != null)
			{
				MessageToCaster("Your target is immune!", eChatType.CT_System);
				target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
				OnSpellResisted(target);
				return;
			}


			//check for existing effect
			// var debuffs = target.effectListComponent.GetSpellEffects(eEffect.MovementSpeedDebuff);

			// foreach (var debuff in debuffs)
			// {
			// 	if (debuff.SpellHandler.Spell.Value >= Spell.Value)
			// 	{
			// 		// Old Spell is Better than new one
			// 		SendSpellResistAnimation(target);
			// 		this.MessageToCaster(eChatType.CT_SpellResisted, "{0} already has that effect.", target.GetName(0, true));
			// 		MessageToCaster("Wait until it expires. Spell Failed.", eChatType.CT_SpellResisted);
			// 		// Prevent Adding.
			// 		return;
			// 	}
			// }

			if (Caster.HasAbilityType(typeof(AtlasOF_WildArcanaAbility)))
			{
				int criticalchance = this.Caster.DotCriticalChance;

				int randNum = Util.CryptoNextInt(1, 100); //grab our random number
				int critCap = Math.Min(50, criticalchance); //crit chance can be at most  50%

				if (this.Caster is GamePlayer spellCaster && spellCaster.UseDetailedCombatLog && critCap > 0)
				{
					spellCaster.Out.SendMessage($"debuff crit chance: {critCap} random: {randNum}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
				}

				if (critCap > randNum)
				{
					//effectiveness *= 2;
					crit = true;
					if(Caster is GamePlayer c) c.Out.SendMessage($"Your snare is doubly effective!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
				}
			}
			
			base.ApplyEffectOnTarget(target, effectiveness);
		}

		/// <summary>
		/// When an applied effect starts
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			// Cannot apply if the effect owner has a charging effect
			if (effect.Owner.EffectList.GetOfType<ChargeEffect>() != null || effect.Owner.TempProperties.getProperty("Charging", false))
			{
				MessageToCaster(effect.Owner.Name + " is moving too fast for this spell to have any effect!", eChatType.CT_SpellResisted);
				return;
			}
			base.OnEffectStart(effect);
			GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttacked));
			// Cancels mezz on the effect owner, if applied
			//GameSpellEffect mezz = SpellHandler.FindEffectOnTarget(effect.Owner, "Mesmerize");
			ECSGameEffect mezz = EffectListService.GetEffectOnTarget(effect.Owner, eEffect.Mez);
			if (mezz != null)
				EffectService.RequestImmediateCancelEffect(mezz);
				//mezz.Cancel(false);
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttacked));
			return base.OnEffectExpires(effect, noMessages);
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
					//GameSpellEffect effect = FindEffectOnTarget(living, this);
					ECSGameEffect effect = EffectListService.GetEffectOnTarget(living, eEffect.MovementSpeedDebuff);
					if (effect != null)
						EffectService.RequestImmediateCancelEffect(effect);
						//effect.Cancel(false);
					break;
			}
		}

		// constructor
		public SpeedDecreaseSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
