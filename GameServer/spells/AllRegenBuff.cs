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
	/// All Stats buff
	/// </summary>
	[SpellHandlerAttribute("AllRegenBuff")]
	public class AllRegenBuff : PropertyChangingSpell
	{
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
        public override eProperty Property1 => eProperty.PowerRegenerationRate;
        public override eProperty Property2 => eProperty.EnduranceRegenerationRate;
        public override eProperty Property3 => eProperty.HealthRegenerationRate;



        // constructor
        public AllRegenBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}

	/// <summary>
	/// All Stats buff
	/// </summary>
	[SpellHandlerAttribute("BeadRegen")]
	public class BeadRegen : PropertyChangingSpell {
		private int pomID = 31057;
		private int endID = 31056;
		private int healID = 31055;

		public override bool StartSpell(GameLiving target)
		{
            if (Caster.CurrentZone.IsRvR)
            {
				if (Caster is GamePlayer p)
					p.Out.SendMessage("You cannot use this item in an RvR zone.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				return false;
            }
			if (Caster.Level > 30)
            {
				if (Caster is GamePlayer p)
					p.Out.SendMessage("You are too powerful for this item's effects.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				return false;
			}

			if(Caster.ControlledBrain != null && Caster.ControlledBrain is AI.Brain.NecromancerPetBrain necroPet && necroPet.Body.InCombatInLast(15000))
            {
				if (Caster is GamePlayer p)
					p.Out.SendMessage("Your pet must be out of combat for 15 seconds to use this.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				return false;
			}
				
			target = Caster;
			SpellLine potionEffectLine = SkillBase.GetSpellLine(GlobalSpellsLines.Potions_Effects);

			Spell pomSpell = SkillBase.GetSpellByID(pomID);
			SpellHandler pomSpellHandler = ScriptMgr.CreateSpellHandler(target, pomSpell, potionEffectLine) as SpellHandler;

			Spell endSpell = SkillBase.GetSpellByID(endID);
			SpellHandler endSpellHandler = ScriptMgr.CreateSpellHandler(target, endSpell, potionEffectLine) as SpellHandler;

			Spell healSpell = SkillBase.GetSpellByID(healID);
			SpellHandler healthConSpellHandler = ScriptMgr.CreateSpellHandler(target, healSpell, potionEffectLine) as SpellHandler;

			pomSpellHandler.StartSpell(target);
			endSpellHandler.StartSpell(target);
			healthConSpellHandler.StartSpell(target);

			if(Caster.ControlledBrain != null && Caster.ControlledBrain is AI.Brain.NecromancerPetBrain necrop)
            {
				SpellHandler petHealHandler = ScriptMgr.CreateSpellHandler(necrop.Body, healSpell, potionEffectLine) as SpellHandler;
				petHealHandler.StartSpell(necrop.Body);
			}

			return true;
		}
		public override eProperty Property1 => eProperty.PowerRegenerationRate;
		public override eProperty Property2 => eProperty.EnduranceRegenerationRate;
		public override eProperty Property3 => eProperty.HealthRegenerationRate;



		// constructor
		public BeadRegen(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
