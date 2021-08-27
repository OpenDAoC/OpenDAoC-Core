using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Base "Mastery of" class.
	/// </summary>
	public class AtlasOF_MasteryRA : RAPropertyEnhancer
	{
		public AtlasOF_MasteryRA(DBAbility dba, int level, eProperty property)
			: base(dba, level, property)
		{
		}

        public override int GetAmountForLevel(int level)
        {
            if (level < 1) return 0;

            switch (level)
            {
                case 1: return 3;
                case 2: return 6;
                case 3: return 9;
                case 4: return 12;
                case 5: return 15;
                default: return 15;
            }
        }

        public override int CostForUpgrade(int level)
		{
			switch (level)
			{
                case 0: return 1;
				case 1: return 3;
				case 2: return 6;
				case 3: return 10;
				case 4: return 14;
				default: return 1000;
			}
		}

        protected bool HasAugDex2(GamePlayer player)
        {
            AtlasOF_RADexterityEnhancer augdex = player.GetAbility<AtlasOF_RADexterityEnhancer>();
            if (augdex == null)
                return false;

            return player.CalculateSkillLevel(augdex) > 1;
        }
    }

	/// <summary>
	/// Mastery of Pain ability
	/// </summary>
	public class AtlasOF_MasteryOfPain : AtlasOF_MasteryRA
	{
		public AtlasOF_MasteryOfPain(DBAbility dba, int level)
			: base(dba, level, eProperty.CriticalMeleeHitChance)
		{
		}

		protected override string ValueUnit { get { return "%"; } }
        public override bool CheckRequirement(GamePlayer player) { return HasAugDex2(player); }
    }

    /// <summary>
    /// Mastery of Parry ability
    /// </summary>
    public class AtlasOF_MasteryOfParrying : AtlasOF_MasteryRA
	{
		public AtlasOF_MasteryOfParrying(DBAbility dba, int level)
			: base(dba, level, eProperty.ParryChance)
		{
		}

		protected override string ValueUnit { get { return "%"; } }
        public override bool CheckRequirement(GamePlayer player) { return HasAugDex2(player); }
    }

    /// <summary>
    /// Mastery of Blocking ability
    /// </summary>
    public class AtlasOF_MasteryOfBlocking : AtlasOF_MasteryRA
	{
		public AtlasOF_MasteryOfBlocking(DBAbility dba, int level)
			: base(dba, level, eProperty.BlockChance)
		{
		}

		protected override string ValueUnit { get { return "%"; } }
        public override bool CheckRequirement(GamePlayer player) { return HasAugDex2(player); }
    }
}
