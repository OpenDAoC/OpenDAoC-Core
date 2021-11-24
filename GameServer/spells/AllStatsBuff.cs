/*
 * Atlas
 *
 */
using System;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using System.Collections.Generic;

namespace DOL.GS.Spells
{
	/// <summary>
	/// All Stats buff
	/// </summary>
	[SpellHandlerAttribute("AllStatsBarrel")]
	public class AllStatsBarrel : SingleStatBuff
	{
		public static List<int> BuffList = new List<int> {8090,8091,8094,8092,8095,8093/*,8071*/};
		private int strengthID = 8090;
		private int conID = 8091;
		private int strenghtConID = 8094;
		private int dexID = 8092;
		private int dexQuickID = 8095;
		private int acuityID = 8093;
		private int hasteID = 8071;

        public override bool StartSpell(GameLiving target)
        {
			SpellLine potionEffectLine = SkillBase.GetSpellLine(GlobalSpellsLines.Potions_Effects);

			Spell strengthSpell = SkillBase.FindSpell(strengthID, potionEffectLine);
			SpellHandler strenghtSpellHandler = ScriptMgr.CreateSpellHandler(target, strengthSpell, potionEffectLine) as SpellHandler;

			Spell conSpell = SkillBase.FindSpell(conID, potionEffectLine);
			SpellHandler conSpellHandler = ScriptMgr.CreateSpellHandler(target, conSpell, potionEffectLine) as SpellHandler;

			Spell strengthConSpell = SkillBase.FindSpell(strenghtConID, potionEffectLine);
			SpellHandler strenghtConSpellHandler = ScriptMgr.CreateSpellHandler(target, strengthConSpell, potionEffectLine) as SpellHandler;

			Spell dexSpell = SkillBase.FindSpell(dexID, potionEffectLine);
			SpellHandler dexSpellHandler = ScriptMgr.CreateSpellHandler(target, dexSpell, potionEffectLine) as SpellHandler;

			Spell dexQuickSpell = SkillBase.FindSpell(dexQuickID, potionEffectLine);
			SpellHandler dexQuickSpellHandler = ScriptMgr.CreateSpellHandler(target, dexQuickSpell, potionEffectLine) as SpellHandler;

			Spell acuitySpell = SkillBase.FindSpell(acuityID, potionEffectLine);
			SpellHandler acuitySpellHandler = ScriptMgr.CreateSpellHandler(target, acuitySpell, potionEffectLine) as SpellHandler;

			//Spell hasteSpell = SkillBase.FindSpell(hasteID, potionEffectLine);
			//SpellHandler hasteSpellHandler = ScriptMgr.CreateSpellHandler(target, hasteSpell, potionEffectLine) as SpellHandler;

			strenghtSpellHandler.StartSpell(target);
			conSpellHandler.StartSpell(target);
			strenghtConSpellHandler.StartSpell(target);
			dexSpellHandler.StartSpell(target);
			dexQuickSpellHandler.StartSpell(target);
			acuitySpellHandler.StartSpell(target);
			//hasteSpellHandler.StartSpell(target);

			return true;
		}
        public override eProperty Property1 => eProperty.Strength;

        public override eProperty Property2 => eProperty.Constitution;

        public override eProperty Property3 => eProperty.Dexterity;

        public override eProperty Property4 => eProperty.Quickness;

        public override eProperty Property5 => eProperty.Acuity;

        // constructor
        public AllStatsBarrel(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
