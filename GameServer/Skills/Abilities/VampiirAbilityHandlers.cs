using DOL.Database;

namespace DOL.GS.SkillHandler
{
	/// <summary>
	/// Abstract Vampiir Ability using Level Based Ability to enable stat changing with Ratio Preset.
	/// </summary>
	public abstract class VampiirAbilityHandlers : LevelBasedStatChangingAbility
	{
		/// <summary>
		/// Multiplier for Ability Level to adjust Stats for given Ability 
		/// </summary>
		public abstract int RatioByLevel { get; }
				
		/// <summary>
		/// Return Amount for this Stat Changing Ability
		/// Based on Current Ability Level and Ratio Multiplier
		/// </summary>
		/// <param name="level">Targeted Ability Level</param>
		/// <returns>Stat Changing amount</returns>
		public override int GetAmountForLevel(int level)
		{
			//(+stats every level starting level 6),
			return level < 6 ? 0 : (level - 5) * RatioByLevel;
		}
		
		protected VampiirAbilityHandlers(DbAbility dba, int level, EProperty property)
			: base(dba, level, property)
		{
		}
	}

	/// <summary>
	/// Vampiir Ability for Strength Stat
	/// </summary>
	public class VampiirStrengthAbilityHandler : VampiirAbilityHandlers
	{
		/// <summary>
		/// Ratio Preset to *3
		/// </summary>
		public override int RatioByLevel { get { return 3; } }
		
		public VampiirStrengthAbilityHandler(DbAbility dba, int level)
			: base(dba, level, EProperty.Strength)
		{
		}
	}

	/// <summary>
	/// Vampiir Ability for Strength Stat
	/// </summary>
	public class VampiirDexterityAbilityHandler : VampiirAbilityHandlers
	{
		/// <summary>
		/// Ratio Preset to *3
		/// </summary>
		public override int RatioByLevel { get { return 3; } }

		public VampiirDexterityAbilityHandler(DbAbility dba, int level)
			: base(dba, level, EProperty.Dexterity)
		{
		}
	}

	/// <summary>
	/// Vampiir Ability for Strength Stat
	/// </summary>
	public class VampiirConstitutionAbilityHandler : VampiirAbilityHandlers
	{
		/// <summary>
		/// Ratio Preset to *3
		/// </summary>
		public override int RatioByLevel { get { return 3; } }

		public VampiirConstitutionAbilityHandler(DbAbility dba, int level)
			: base(dba, level, EProperty.Constitution)
		{
		}
	}

	/// <summary>
	/// Vampiir Ability for Strength Stat
	/// </summary>
	public class VampiirQuicknessAbilityHandler : VampiirAbilityHandlers
	{
		/// <summary>
		/// Ratio Preset to *2
		/// </summary>
		public override int RatioByLevel { get { return 2; } }

		public VampiirQuicknessAbilityHandler(DbAbility dba, int level)
			: base(dba, level, EProperty.Quickness)
		{
		}
	}
}
