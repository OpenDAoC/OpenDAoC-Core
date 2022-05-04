using System.Collections.Generic;

namespace DOL.GS;

public class AchievementUtils
{
    public abstract class AchievementNames
    {
        public const string Orbs_Earned = "Lifetime Orbs Earned";

        #region PvE

        public const string Dragon_Kills = "Total Dragon Kills";
        public const string Epic_Boss_Kills = "Epic Dungeon Boss Kills";
        public const string Legion_Kills = "Legion Kills";

        #endregion
        
        
        #region RvR
        
        public const string Keeps_Taken = "Keeps Taken";
        public const string Realm_Rank = "Realm Rank";
        public const string Relic_Captures = "Relic Captures";
        
        public const string Players_Killed = "Players Killed";
        public const string Alb_Players_Killed = "Albion Players Killed";
        public const string Hib_Players_Killed = "Hibernia Players Killed";
        public const string Mid_Players_Killed = "Midgard Players Killed";

        public const string Deathblows = "Deathblows";
        public const string Alb_Deathblows = "Albion Deathblows";
        public const string Mid_Deathblows = "Midgard Deathblows";
        public const string Hib_Deathblows = "Hibernia Deathblows";
        
        public const string Solo_Kills = "Solo Kills";
        public const string Hib_Solo_Kills = "Hibernia Solo Kills";
        public const string Mid_Solo_Kills = "Midgard Solo Kills";
        public const string Alb_Solo_Kills = "Albion Solo Kills";

        #endregion
        
        #region Crafting

        public const string Mastered_Crafts = "Number of Crafts Above 1000";

        #endregion

    }


    public List<string> GetAchievementNames()
    {
        List<string> achievements = new List<string>();
        
        //general
        achievements.Add(AchievementNames.Orbs_Earned);
        
        //pve
        achievements.Add(AchievementNames.Dragon_Kills);
        achievements.Add(AchievementNames.Epic_Boss_Kills);
        
        //rvr
        achievements.Add(AchievementNames.Keeps_Taken);
        achievements.Add(AchievementNames.Players_Killed);
        achievements.Add(AchievementNames.Solo_Kills);
        achievements.Add(AchievementNames.Realm_Rank);
        
        //crafting
        achievements.Add(AchievementNames.Mastered_Crafts);

        return achievements;
    }
}