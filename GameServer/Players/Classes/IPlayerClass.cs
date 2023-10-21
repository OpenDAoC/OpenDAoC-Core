using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Realm;

namespace Core.GS
{
	/// <summary>
	/// Represents a player class
	/// </summary>
	public interface IPlayerClass
	{
		/// <summary>
		/// Unique Class Identifier based on eCharacterClass
		/// </summary>
		int ID { get; }

		/// <summary>
		/// ClassName (Used as Translation Key / Title Translation Key)
		/// </summary>
		string Name { get; }
		/// <summary>
		/// Class Female Name
		/// </summary>
		string FemaleName { get; }

		/// <summary>
		/// Base ClassName if available
		/// </summary>
		string BaseName { get; }

		/// <summary>
		/// Class Profession
		/// </summary>
		string Profession { get; }

		/// <summary>
		/// Class Base Hit Points
		/// </summary>
		int BaseHP { get; }

		/// <summary>
		/// Class Spec Points Multiplier
		/// </summary>
		int SpecPointsMultiplier { get; }

		/// <summary>
		/// This is specifically used for adjusting spec points as needed for new training window
		/// For standard DOL classes this will simply return the standard spec multiplier
		/// </summary>
		int AdjustedSpecPointsMultiplier { get; }

		/// <summary>
		/// Class Primary Raising Stat
		/// </summary>
		EStat PrimaryStat { get; }

		/// <summary>
		/// Class Secondary Raising Stat
		/// </summary>
		EStat SecondaryStat { get; }
		
		/// <summary>
		/// Class Tertiary Raising Stat
		/// </summary>
		EStat TertiaryStat { get; }
		
		/// <summary>
		/// Class Mana Stat Used for Spell
		/// </summary>
		EStat ManaStat { get; }
		
		/// <summary>
		/// Class Weapon Skill Base
		/// </summary>
		int WeaponSkillBase { get; }
		
		/// <summary>
		/// Class Weapon Skill Ranged Base
		/// </summary>
		int WeaponSkillRangedBase { get; }

		/// <summary>
		/// Class Type
		/// </summary>
		EPlayerClassType ClassType { get; }

		/// <summary>
		/// Instance Attached GamePlayer
		/// </summary>
		GamePlayer Player { get; }

		/// <summary>
		/// The maximum number of pulsing spells the class can have active simultaneously
		/// </summary>
		ushort MaxPulsingSpells { get; }

		/// <summary>
		/// Wether this class can use Left Handed Weapon
		/// </summary>
		bool CanUseLefthandedWeapon { get; }
		
		/// <summary>
		/// Group Health Percent Window Override
		/// </summary>
		byte HealthPercentGroupWindow { get; }

		string GetTitle(GamePlayer player, int level);
		void OnLevelUp(GamePlayer player, int previousLevel);
		void OnRealmLevelUp(GamePlayer player);
		void OnSkillTrained(GamePlayer player, Specialization skill);
		IList<string> GetAutotrainableSkills();
		bool HasAdvancedFromBaseClass();
		void Init(GamePlayer player);
		void SetControlledBrain(IControlledBrain controlledBrain);
		void CommandNpcRelease();
		void OnPetReleased();
		bool StartAttack(GameObject attackTarget);
		ShadeEcsAbilityEffect CreateShadeEffect();
		void Shade(bool state);
		bool RemoveFromWorld();
		void Die(GameObject killer);
		void Notify(CoreEvent e, object sender, EventArgs args);
		bool CanChangeCastingSpeed(SpellLine line, Spell spell);
		GameTrainer.eChampionTrainerType ChampionTrainerType();
		List<PlayerRace> EligibleRaces { get; }
	}
}