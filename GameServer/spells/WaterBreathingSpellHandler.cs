using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    // TODO: ToolTip now working, dont know why
	// DB entries are all correct
    [SpellHandler(eSpellType.WaterBreathing)] 
    public class WaterBreathingSpellHandler : SpellHandler
    {
		public override string ShortDescription => $"Target gains the ability to breathe underwater and their swimming speed increases by {Spell.Value}%.";
        public WaterBreathingSpellHandler(GameLiving caster, Spell spell, SpellLine line) 
            : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, static (in i) => new WaterBreathingECSEffect(i));
        }

		protected override int CalculateEffectDuration(GameLiving target)
		{
			double duration = Spell.Duration;
			duration *= 1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01;
			return (int)duration;
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				/*
				<Begin Info: Motivation Sng>
 
				The movement speed of the target is increased.
 
				Target: Group
				Range: 2000
				Duration: 30 sec
				Frequency: 6 sec
				Casting time:      3.0 sec
				
				This spell's effect will not take hold while the target is in combat.
				<End Info>
				*/
				IList<string> list = base.DelveInfo;

				list.Add(" "); //empty line
				list.Add("Allows the target to breathe underwater.");

				return list;
			}
		}
    }
}