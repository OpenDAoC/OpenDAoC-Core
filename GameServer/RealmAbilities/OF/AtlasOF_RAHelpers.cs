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

        public static int GetAugDexLevel(GamePlayer player)
        {
            AtlasOF_RADexterityEnhancer augDex = player.GetAbility<AtlasOF_RADexterityEnhancer>();
            if (augDex == null)
                return 0;

            return player.CalculateSkillLevel(augDex);
        }

        public static int GetAugStrLevel(GamePlayer player)
        {
            AtlasOF_RAStrengthEnhancer augStr = player.GetAbility<AtlasOF_RAStrengthEnhancer>();
            if (augStr == null)
                return 0;

            return player.CalculateSkillLevel(augStr);
        }

        public static int GetAugConLevel(GamePlayer player)
        {
            AtlasOF_RAConstitutionEnhancer augCon = player.GetAbility<AtlasOF_RAConstitutionEnhancer>();
            if (augCon == null)
                return 0;

            return player.CalculateSkillLevel(augCon);
        }

        public static int GetAugAcuityLevel(GamePlayer player)
        {
            AtlasOF_RAAcuityEnhancer augAcuity = player.GetAbility<AtlasOF_RAAcuityEnhancer>();

            if (augAcuity == null)
                return 0;

            return player.CalculateSkillLevel(augAcuity);
        }

        public static int GetAugQuiLevel(GamePlayer player)
        {
            AtlasOF_RAQuicknessEnhancer augQui = player.GetAbility<AtlasOF_RAQuicknessEnhancer>();

            if (augQui == null)
                return 0;

            return player.CalculateSkillLevel(augQui);
        }

        public static int GetSerenityLevel(GamePlayer player)
        {
            AtlasOF_SerenityAbility raSerenity = player.GetAbility<AtlasOF_SerenityAbility>();

            if (raSerenity == null)
                return 0;

            return player.CalculateSkillLevel(raSerenity);
        }

        public static int GetFirstAidLevel(GamePlayer player)
        {
            AtlasOF_FirstAid raFirstAid = player.GetAbility<AtlasOF_FirstAid>();

            if (raFirstAid == null)
                return 0;

            return player.CalculateSkillLevel(raFirstAid);
        }

        public static int GetLongshotLevel(GamePlayer player)
        {
            AtlasOF_Longshot raLongshot = player.GetAbility<AtlasOF_Longshot>();

            if (raLongshot == null)
                return 0;

            return player.CalculateSkillLevel(raLongshot);
        }
    }
}
