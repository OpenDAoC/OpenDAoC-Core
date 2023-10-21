using System;
using Core.Database;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.AI.Brain;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Keeps;
using Core.Language;

namespace Core.GS.Spells
{
	/// <summary>
	/// Contains all common code for damage add and shield spell handlers
	/// </summary>
	public abstract class ADamageAddSpell : SpellHandler
	{
		/// <summary>
		/// The event type to hook on
		/// </summary>
		protected abstract CoreEvent EventType { get; }

		/// <summary>
		/// The event handler of given event type
		/// </summary>
		public abstract void EventHandler(CoreEvent e, object sender, EventArgs arguments);

		/// <summary>
		/// Holds min damage spread based on spec level caster
		/// had the moment spell was casted
		/// </summary>
		protected int m_minDamageSpread = 50;
		
		/// <summary>
		/// called when spell effect has to be started and applied to targets
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			double duration = Spell.Duration;
			duration *= (1.0 + m_caster.GetModified(EProperty.SpellDuration) * 0.01);
			return (int)duration;
		}
		
		/// <summary>
		/// called when spell effect has to be started and applied to targets
		/// </summary>
		public override bool StartSpell(GameLiving target)
		{
			// set min spread based on spec
			if (Caster is GamePlayer)
			{
				int lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);
				m_minDamageSpread = 50;
				if (Spell.Level > 0)
				{
					m_minDamageSpread += (lineSpec - 1) * 50 / Spell.Level;
					if (m_minDamageSpread > 100) m_minDamageSpread = 100;
					else if (m_minDamageSpread < 50) m_minDamageSpread = 50;
				}
				else
				{
					// For level 0 spells, like realm abilities, always work off of full spec to achieve live like damage amounts.
					// If spec level is used at all it most likely should only be for baseline spells. - tolakram
					m_minDamageSpread = 100;
				}
			}

			return base.StartSpell(target);
		}

		/// <summary>
		/// When an applied effect starts
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			// "Your weapon is blessed by the gods!"
			// "{0}'s weapon glows with the power of the gods!"
			EChatType chatType = EChatType.CT_SpellPulse;
			if (Spell.Pulse == 0)
			{
				chatType = EChatType.CT_Spell;
			}
			bool upperCase = Spell.Message2.StartsWith("{0}");
			MessageToLiving(effect.Owner, Spell.Message1, chatType);
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, 
				effect.Owner.GetName(0, upperCase)), chatType, effect.Owner);
			GameEventMgr.AddHandler(effect.Owner, EventType, new CoreEventHandler(EventHandler));
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
			if (!noMessages && Spell.Pulse == 0)
			{
				// "Your weapon returns to normal."
				// "{0}'s weapon returns to normal."
				bool upperCase = Spell.Message4.StartsWith("{0}");
				MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
				MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, 
					effect.Owner.GetName(0, upperCase)), EChatType.CT_SpellExpires, effect.Owner);
			}
			GameEventMgr.RemoveHandler(effect.Owner, EventType, new CoreEventHandler(EventHandler));
			return 0;
		}

		public override void OnEffectRestored(GameSpellEffect effect, int[] vars)
		{
			GameEventMgr.AddHandler(effect.Owner, EventType, new CoreEventHandler(EventHandler));
		}

		public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
		{
			return OnEffectExpires(effect, noMessages);
		}

		public override DbPlayerXEffect GetSavedEffect(GameSpellEffect e)
		{
			DbPlayerXEffect eff = new DbPlayerXEffect();
			eff.Var1 = Spell.ID;
			eff.Duration = e.RemainingTime;
			eff.IsHandler = true;
			eff.SpellLine = SpellLine.KeyName;
			return eff;
		}

		// constructor
		public ADamageAddSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
	}
}
