using System;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Style stun effect spell handler
	/// </summary>
	[SpellHandler(eSpellType.StyleStun)]
	public class StyleStun : StunSpellHandler
	{
		public StyleStun(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
		{
			return ECSGameEffectFactory.Create(initParams, static (in ECSGameEffectInitParams i) => new StunECSGameEffect(i));
		}
		
		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			// Override to ignore eProperty.StunDurationReduction.
			double duration = Spell.Duration;

			if (EffectListService.GetEffectOnTarget(target, eEffect.NPCStunImmunity) is NpcStunImmunityEffect immunityEffect)
				duration = immunityEffect.CalculateNewEffectDuration((long) duration);

			return (int) Math.Max(duration, 1);
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
			// http://www.camelotherald.com/more/1749.shtml
			// immunity timer will now be exactly five times the length of the stun
			base.OnEffectExpires(effect, noMessages);
			return Spell.Duration * 5;
		}

		public override bool HasConflictingEffectWith(ISpellHandler compare)
		{
			if (Spell.EffectGroup != 0 || compare.Spell.EffectGroup != 0)
				return Spell.EffectGroup == compare.Spell.EffectGroup;
			if (compare.Spell.SpellType == eSpellType.Stun) return true;
			return base.HasConflictingEffectWith(compare);
		}
	}
}
