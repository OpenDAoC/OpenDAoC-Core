/*
 * Atlas
 *
 */
using System;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Buffs all stats at once, goes into specline bonus category
	/// </summary>	
	public abstract class AllStatsBuff : SingleStatBuff
	{
		//str
		public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;

		//con
		public override eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.BaseBuff;

		//dex
		public override eBuffBonusCategory BonusCategory3 => eBuffBonusCategory.BaseBuff;

		//qui
		public override eBuffBonusCategory BonusCategory4 => eBuffBonusCategory.SpecBuff;

		//acu
		public override eBuffBonusCategory BonusCategory5 => eBuffBonusCategory.SpecBuff;

		/// <summary>
		/// Default Constructor
		/// </summary>
		protected AllStatsBuff(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
		}
	}

	/// <summary>
	/// All Stats buff
	/// </summary>
	[SpellHandlerAttribute("AllStatsBarrel")]
	public class AllStatsBarrel : AllStatsBuff
	{
		public override eProperty Property1 => eProperty.Strength;

		public override eProperty Property2 => eProperty.Constitution;

		public override eProperty Property3 => eProperty.Dexterity;

		public override eProperty Property4 => eProperty.Quickness;

		public override eProperty Property5 => eProperty.Acuity;
		
		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}
		// constructor
		public AllStatsBarrel(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
