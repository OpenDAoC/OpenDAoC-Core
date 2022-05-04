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

        #endregion
        
        
        #region RvR
        
        public const string Keeps_Taken = "Keeps Taken";
        public const string Players_Killed = "Players Killed";
        public const string Solo_Kills = "Solo Kills";
        public const string Realm_Rank = "Realm Rank";

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