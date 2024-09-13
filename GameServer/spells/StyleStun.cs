using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Style stun effect spell handler
	/// </summary>
	[SpellHandler(eSpellType.StyleStun)]
	public class StyleStun : StunSpellHandler
	{
		public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
		{
			return new StunECSGameEffect(initParams);
		}
		
		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			NPCECSStunImmunityEffect npcImmune = (NPCECSStunImmunityEffect)EffectListService.GetEffectOnTarget(target, eEffect.NPCStunImmunity);
			if (npcImmune != null)
			{
				int duration = (int)npcImmune.CalculateStunDuration(Spell.Duration);
				return  duration > 1 ? duration : 1;
			}
			else
				return Spell.Duration;
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

		/// <summary>
		/// Determines wether this spell is compatible with given spell
		/// and therefore overwritable by better versions
		/// spells that are overwritable cannot stack
		/// </summary>
		/// <param name="compare"></param>
		/// <returns></returns>
		public override bool IsOverwritable(ECSGameSpellEffect compare)
		{
			if (Spell.EffectGroup != 0 || compare.SpellHandler.Spell.EffectGroup != 0)
				return Spell.EffectGroup == compare.SpellHandler.Spell.EffectGroup;
			if (compare.SpellHandler.Spell.SpellType == eSpellType.Stun) return true;
			return base.IsOverwritable(compare);
		}

		// constructor
		public StyleStun(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
