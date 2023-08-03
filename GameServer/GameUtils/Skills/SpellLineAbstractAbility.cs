﻿using System;
using System.Linq;

using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// Abstract Spell Line Ability Class is a base Class to get Ability Effects based on Spell Line constraints
	/// This Allow Derived Ability to Cast or Enable arbitrary Spells in Database based on Ability Level matched against Spell Line's Level Spells
	/// Using Spell Line Level Increase it can index different Spell Effect Based on Ability Level
	/// </summary>
	public abstract class SpellLineAbstractAbility : AbilityUtil, ISpellCastingAbilityHandler
	{
		/// <summary>
		/// Get this Ability Current Spell for Level in SpellLine
		/// </summary>
		public Spell Spell
		{
			get
			{
				return GetSpellForLevel(Level);
			}
		}
		
		/// <summary>
		/// Get this Ability Current SpellLine (Where Spell Line == Ability KeyName)
		/// </summary>
		public SpellLine SpellLine
		{
			get
			{
				var line = SkillBase.GetSpellLine(KeyName);
				
				if (line != null)
					line.Level = Level;
				
				return line;
			}
		}
		
		/// <summary>
		/// Return This Ability for Interface Implementation
		/// </summary>
		public AbilityUtil Ability
		{
			get
			{
				return this;
			}
		}
		
		/// <summary>
		/// Get spell attached to Ability Level.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		public Spell GetSpellForLevel(int level)
		{
			var line = SpellLine;
			return line != null ? SkillBase.GetSpellList(line.KeyName).FirstOrDefault(spell => spell.Level == level) : null;
		}
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <param name="dba"></param>
		/// <param name="level"></param>
		protected SpellLineAbstractAbility(DbAbilities dba, int level)
			: base(dba, level)
		{
		}
	}
}