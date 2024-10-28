namespace DOL.GS.RealmAbilities
{
    public static class AtlasRAHelpers
    {
        /// <summary>
        /// 6 stat points per level (Augmented Str, Dex, etc).
        /// </summary>
        public static int GetStatEnhancerAmountForLevel(int level)
        {
            if (level < 1) return 0;

            switch (level)
            {
                case 1: return 6;
                case 2: return 12;
                case 3: return 18;
                case 4: return 24;
                case 5: return 30;
                default: return 30;
            }
        }

        /// <summary>
        /// 3% per level.
        /// </summary>
        public static int GetPropertyEnhancer3AmountForLevel(int level)
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

        /// <summary>
        /// 5% per level.
        /// </summary>
        public static int GetPropertyEnhancer5AmountForLevel(int level)
        {
            if (level < 1) return 0;

            switch (level)
            {
                case 1: return 5;
                case 2: return 10;
                case 3: return 15;
                case 4: return 20;
                case 5: return 25;
                default: return 25;
            }
        }

        
        /// <summary>
        /// Shared by almost all passive OF Realm Abilities.
        /// </summary>
        public static int GetCommonUpgradeCostFor5LevelsRA(int currentLevel)
        {
            switch (currentLevel)
            {
                case 0: return 1;
                case 1: return 3;
                case 2: return 6;
                case 3: return 10;
                case 4: return 14;
                default: return 1000;
            }
        }

        /// <summary>
        /// Shared by almost all active OF Realm Abilities (that have more than one level).
        /// </summary>
        public static int GetCommonUpgradeCostFor3LevelsRA(int currentLevel)
        {
            switch (currentLevel)
            {
                case 0: return 3;
                case 1: return 6;
                case 2: return 10;
                default: return 1000;
            }
        }

        public static int GetAugDexLevel(GameLiving living)
        {
            AtlasOF_RADexterityEnhancer augDex = living.GetAbility<AtlasOF_RADexterityEnhancer>();
            if (augDex == null)
                return 0;

            return living.CalculateSkillLevel(augDex);
        }

        public static int GetAugStrLevel(GameLiving living)
        {
            AtlasOF_RAStrengthEnhancer augStr = living.GetAbility<AtlasOF_RAStrengthEnhancer>();
            if (augStr == null)
                return 0;

            return living.CalculateSkillLevel(augStr);
        }

        public static int GetAugConLevel(GameLiving living)
        {
            AtlasOF_RAConstitutionEnhancer augCon = living.GetAbility<AtlasOF_RAConstitutionEnhancer>();
            if (augCon == null)
                return 0;

            return living.CalculateSkillLevel(augCon);
        }

        public static int GetAugAcuityLevel(GameLiving living)
        {
            AtlasOF_RAAcuityEnhancer augAcuity = living.GetAbility<AtlasOF_RAAcuityEnhancer>();

            if (augAcuity == null)
                return 0;

            return living.CalculateSkillLevel(augAcuity);
        }

        public static int GetAugQuiLevel(GameLiving living)
        {
            AtlasOF_RAQuicknessEnhancer augQui = living.GetAbility<AtlasOF_RAQuicknessEnhancer>();

            if (augQui == null)
                return 0;

            return living.CalculateSkillLevel(augQui);
        }

        public static int GetSerenityLevel(GameLiving living)
        {
            AtlasOF_SerenityAbility raSerenity = living.GetAbility<AtlasOF_SerenityAbility>();

            if (raSerenity == null)
                return 0;

            return living.CalculateSkillLevel(raSerenity);
        }

        public static int GetFirstAidLevel(GameLiving player)
        {
            AtlasOF_FirstAid raFirstAid = player.GetAbility<AtlasOF_FirstAid>();

            if (raFirstAid == null)
                return 0;

            return player.CalculateSkillLevel(raFirstAid);
        }

        public static int GetLongshotLevel(GameLiving living)
        {
            AtlasOF_Longshot raLongshot = living.GetAbility<AtlasOF_Longshot>();

            if (raLongshot == null)
                return 0;

            return living.CalculateSkillLevel(raLongshot);
        }
    }
}
