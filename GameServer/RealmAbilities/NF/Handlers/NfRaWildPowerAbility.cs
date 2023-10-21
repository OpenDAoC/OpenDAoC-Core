using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities
{
	public class NfRaWildPowerAbility : RaPropertyEnhancer
	{
		public NfRaWildPowerAbility(DbAbility dba, int level)
			: base(dba, level, EProperty.CriticalSpellHitChance)
		{
		}

		protected override string ValueUnit { get { return "%"; } }

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;
			if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
			{
				switch (level)
				{
						case 1: return 3;
						case 2: return 6;
						case 3: return 9;
						case 4: return 13;
						case 5: return 17;
						case 6: return 22;
						case 7: return 27;
						case 8: return 33;
						case 9: return 39;
						default: return 39;
				}
			}
			else
			{
				switch (level)
				{
						case 1: return 3;
						case 2: return 9;
						case 3: return 17;
						case 4: return 27;
						case 5: return 39;
						default: return 39;
				}
			}
		}
	}


	/// <summary>
	/// Mastery of Magery ability, adds to effectivenes of damage spells
	/// </summary>
	public class MasteryOfMageryAbility : RaPropertyEnhancer
	{
		public MasteryOfMageryAbility(DbAbility dba, int level)
			: base(dba, level, EProperty.SpellDamage)
		{
		}

		protected override string ValueUnit { get { return "%"; } }

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;
			if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
			{
				switch (level)
				{
						case 1: return 2;
						case 2: return 3;
						case 3: return 4;
						case 4: return 6;
						case 5: return 8;
						case 6: return 10;
						case 7: return 12;
						case 8: return 14;
						case 9: return 16;
						default: return 16;
				}
			}
			else
			{
				switch (level)
				{
						case 1: return 2;
						case 2: return 4;
						case 3: return 7;
						case 4: return 11;
						case 5: return 15;
						default: return 15;
				}
			}
		}
	}


	/// <summary>
	/// Wild healing ability, critical heal chance bonus to heal spells (SpellHandler checks for it)
	/// </summary>
	public class WildHealingAbility : RaPropertyEnhancer
	{
		public WildHealingAbility(DbAbility dba, int level)
			: base(dba, level, EProperty.CriticalHealHitChance)
		{
		}

		protected override string ValueUnit { get { return "%"; } }

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;
			if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
			{
				switch (level)
				{
						case 1: return 3;
						case 2: return 6;
						case 3: return 9;
						case 4: return 13;
						case 5: return 17;
						case 6: return 22;
						case 7: return 27;
						case 8: return 33;
						case 9: return 39;
						default: return 39;
				}
			}
			else
			{
				switch (level)
				{
						case 1: return 3;
						case 2: return 9;
						case 3: return 17;
						case 4: return 27;
						case 5: return 39;
						default: return 39;
				}
			}
		}
	}

	/// <summary>
	/// Mastery of healing ability, adds to heal spells (SpellHandler checks for it)
	/// </summary>
	public class MasteryOfHealingAbility : RaPropertyEnhancer
	{
		public MasteryOfHealingAbility(DbAbility dba, int level)
			: base(dba, level, EProperty.HealingEffectiveness)
		{
		}

		protected override string ValueUnit { get { return "%"; } }

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;
			if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
			{
				switch (level)
				{
						case 1: return 2;
						case 2: return 4;
						case 3: return 6;
						case 4: return 9;
						case 5: return 12;
						case 6: return 16;
						case 7: return 20;
						case 8: return 25;
						case 9: return 30;
						default: return 30;
				}
			}
			else
			{
				switch (level)
				{
						case 1: return 2;
						case 2: return 5;
						case 3: return 12;
						case 4: return 19;
						case 5: return 28;
						default: return 28;
				}
			}
		}
	}

	/// <summary>
	/// Mastery of focus ability, adds to spell-level for resist bonus (SpellHandler checks for it)
	/// </summary>
	public class MasteryOfFocusAbility : RaPropertyEnhancer
	{
		public MasteryOfFocusAbility(DbAbility dba, int level) : base(dba, level, EProperty.SpellLevel) { }

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;
			if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
			{
				switch (level)
				{
						case 1: return 3;
						case 2: return 6;
						case 3: return 9;
						case 4: return 13;
						case 5: return 17;
						case 6: return 22;
						case 7: return 27;
						case 8: return 33;
						case 9: return 39;
						default: return 39;
				}
			}
			else
			{
				switch (level)
				{
						case 1: return 3;
						case 2: return 9;
						case 3: return 17;
						case 4: return 27;
						case 5: return 39;
						default: return 39;
				}
			}
		}

		public override bool CheckRequirement(GamePlayer player)
		{
			return player.Level >= 40;
		}
	}
}
