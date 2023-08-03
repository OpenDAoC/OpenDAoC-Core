using System;
using System.Collections;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
	[SpellHandlerAttribute("DamageSpeedDecreaseNoVariance")]
    public class DmgSpeedDecreaseNoVarHandler : DmgSpeedDecreaseHandler
	{
		public override double CalculateDamageBase(GameLiving target)
		{
			return Spell.Damage;
		}
		public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
		{
			min = 1.00;
			max = 1.00;
		}
        public DmgSpeedDecreaseNoVarHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}

namespace DOL.GS.Spells
{
    /// <summary>
    /// 
    /// </summary>
    [SpellHandlerAttribute("DirectDamageNoVariance")]
    public class DirectDmgNoVarHandler : DirectDmgHandler
    {
		public override double CalculateDamageBase(GameLiving target)
        {
            return Spell.Damage;
        }
        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            min = 1.00;
            max = 1.00;
        }
        public DirectDmgNoVarHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}

namespace DOL.GS.Spells
{
	/// <summary>
	/// UnresistableStun 
	/// </summary>
	[SpellHandlerAttribute("UnresistableStun")]
	public class IrresistibleStunHandler : StunSpellHandler
	{

		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (effect.Owner == null) return 0;

			base.OnEffectExpires(effect, noMessages);

			return 0;
		}
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			return Spell.Duration;
		}
		public IrresistibleStunHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}