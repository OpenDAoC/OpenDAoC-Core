using System.Collections.Generic;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS.Spells
{
	/// <summary>
	/// All Stats buff
	/// </summary>
	[SpellHandler("AllRegenBuff")]
	public class AllRegenBuffSpell : PropertyChangingSpell
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
			

			pomSpellHandler.StartSpell(target);
			endSpellHandler.StartSpell(target);
			healthConSpellHandler.StartSpell(target);

			return true;
		}
        public override EProperty Property1 => EProperty.PowerRegenerationRate;
        public override EProperty Property2 => EProperty.EnduranceRegenerationRate;
        public override EProperty Property3 => EProperty.HealthRegenerationRate;



        // constructor
        public AllRegenBuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}

	/// <summary>
	/// All Stats buff
	/// </summary>
	[SpellHandler("BeadRegen")]
	public class BeadRegenSpell : PropertyChangingSpell 
	{
		public static List<int> BeadRegenList = new List<int> {31057,31056,31055};
		private int pomID = 31057;
		private int endID = 31056;
		private int healID = 31055;

		public override bool StartSpell(GameLiving target)
		{
            if (Caster.CurrentZone.IsRvR)
            {
				if (Caster is GamePlayer p)
					p.Out.SendMessage("You cannot use this item in an RvR zone.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
				return false;
            }
			if (Caster.Level > 49)
            {
				if (Caster is GamePlayer p)
					p.Out.SendMessage("You are too powerful for this item's effects.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
				return false;
			}

			if(Caster.ControlledBrain != null && Caster.ControlledBrain is NecromancerPetBrain necroPet && necroPet.Body.InCombatInLast(5000))
            {
				if (Caster is GamePlayer p)
					p.Out.SendMessage("Your pet must be out of combat for 5 seconds to use this.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
				return false;
			}
				
			target = Caster;
			SpellLine potionEffectLine = SkillBase.GetSpellLine(GlobalSpellsLines.Potions_Effects);

			Spell pomSpell = SkillBase.FindSpell(pomID, potionEffectLine);
			SpellHandler pomSpellHandler = ScriptMgr.CreateSpellHandler(target, pomSpell, potionEffectLine) as SpellHandler;

			Spell endSpell = SkillBase.FindSpell(endID, potionEffectLine);
			SpellHandler endSpellHandler = ScriptMgr.CreateSpellHandler(target, endSpell, potionEffectLine) as SpellHandler;

			Spell healSpell = SkillBase.FindSpell(healID, potionEffectLine);
			SpellHandler healthConSpellHandler = ScriptMgr.CreateSpellHandler(target, healSpell, potionEffectLine) as SpellHandler;

			pomSpellHandler.StartSpell(target);
			endSpellHandler.StartSpell(target);
			healthConSpellHandler.StartSpell(target);

			if(Caster.ControlledBrain != null && Caster.ControlledBrain is NecromancerPetBrain necrop)
            {
				SpellHandler petHealHandler = ScriptMgr.CreateSpellHandler(necrop.Body, healSpell, potionEffectLine) as SpellHandler;
				petHealHandler.StartSpell(necrop.Body);
			}

			return true;
		}
		public override EProperty Property1 => EProperty.PowerRegenerationRate;
		public override EProperty Property2 => EProperty.EnduranceRegenerationRate;
		public override EProperty Property3 => EProperty.HealthRegenerationRate;



		// constructor
		public BeadRegenSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
