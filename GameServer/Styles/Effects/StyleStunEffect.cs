using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Style stun effect spell handler
	/// </summary>
	[SpellHandler("StyleStun")]
	public class StyleStunEffect : StunSpell
	{
		public override void CreateECSEffect(EcsGameEffectInitParams initParams)
		{
			new StunEcsSpellEffect(initParams);
		}
		
		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			NpcEcsStunImmunityEffect npcImmune = (NpcEcsStunImmunityEffect)EffectListService.GetEffectOnTarget(target, EEffect.NPCStunImmunity);
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
		public override bool IsOverwritable(EcsGameSpellEffect compare)
		{
			if (Spell.EffectGroup != 0 || compare.SpellHandler.Spell.EffectGroup != 0)
				return Spell.EffectGroup == compare.SpellHandler.Spell.EffectGroup;
			if (compare.SpellHandler.Spell.SpellType == ESpellType.Stun) return true;
			return base.IsOverwritable(compare);
		}

		// constructor
		public StyleStunEffect(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}