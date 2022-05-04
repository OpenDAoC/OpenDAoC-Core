namespace GameServer.gameutils;

public class Achievement
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
}