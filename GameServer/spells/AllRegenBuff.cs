using System.Collections.Generic;

namespace DOL.GS.Spells
{
	/// <summary>
	/// All Stats buff
	/// </summary>
	[SpellHandler(eSpellType.AllRegenBuff)]
	public class AllRegenBuff : PropertyChangingSpell
	{
		public static List<int> RegenList = new List<int> {8084,8080,8076};
		private int pomID = 8084;
		private int endID = 8080;
		private int healID = 8076;

		public override bool StartSpell(GameLiving target)
        {
			SpellLine potionEffectLine = SkillBase.GetSpellLine(GlobalSpellsLines.Potions_Effects);

			Spell pomSpell = SkillBase.FindSpell(pomID, potionEffectLine);
			SpellHandler pomSpellHandler = ScriptMgr.CreateSpellHandler(target, pomSpell, potionEffectLine) as SpellHandler;

			Spell endSpell = SkillBase.FindSpell(endID, potionEffectLine);
			SpellHandler endSpellHandler = ScriptMgr.CreateSpellHandler(target, endSpell, potionEffectLine) as SpellHandler;

			Spell healSpell = SkillBase.FindSpell(healID, potionEffectLine);
			SpellHandler healthConSpellHandler = ScriptMgr.CreateSpellHandler(target, healSpell, potionEffectLine) as SpellHandler;

			return pomSpellHandler.StartSpell(target) |
				endSpellHandler.StartSpell(target) |
				healthConSpellHandler.StartSpell(target);
		}

        public override eProperty Property1 => eProperty.PowerRegenerationAmount;
        public override eProperty Property2 => eProperty.EnduranceRegenerationAmount;
        public override eProperty Property3 => eProperty.HealthRegenerationAmount;

        // constructor
        public AllRegenBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
