using System.Collections.Generic;
using System.Linq;
using DOL.Database;

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


    public static List<string> GetAchievementNames()
    {
        List<string> achievements = new List<string>();
        
        //general
        achievements.Add(AchievementNames.Orbs_Earned);
        
        //pve
        achievements.Add(AchievementNames.Dragon_Kills);
        achievements.Add(AchievementNames.Epic_Boss_Kills);
        achievements.Add(AchievementNames.Legion_Kills);
        
        //rvr
        achievements.Add(AchievementNames.Realm_Rank);
        achievements.Add(AchievementNames.Relic_Captures);
        achievements.Add(AchievementNames.Keeps_Taken);
        
        achievements.Add(AchievementNames.Players_Killed);
        achievements.Add(AchievementNames.Alb_Players_Killed);
        achievements.Add(AchievementNames.Hib_Players_Killed);
        achievements.Add(AchievementNames.Mid_Players_Killed);
        
        achievements.Add(AchievementNames.Alb_Deathblows);
        achievements.Add(AchievementNames.Hib_Deathblows);
        achievements.Add(AchievementNames.Mid_Deathblows);
        
        achievements.Add(AchievementNames.Solo_Kills);
        achievements.Add(AchievementNames.Alb_Solo_Kills);
        achievements.Add(AchievementNames.Hib_Solo_Kills);
        achievements.Add(AchievementNames.Mid_Solo_Kills);

        //crafting
        achievements.Add(AchievementNames.Mastered_Crafts);

        return achievements;
    }

    public static IList<string> GetAchievementInfoForPlayer(GamePlayer player)
    {
        List<string> temp = new List<string>();
        temp.Clear();

        var achievements = DOLDB<Achievement>.SelectObjects(DB.Column("AccountID")
            .IsEqualTo(player.Client.Account.ObjectId));

        if (achievements == null) return temp;

        //need to do this to avoid displaying realm as _FirstRealm or _LastRealm
        Dictionary<int, string> realmDict = new Dictionary<int, string>();
        realmDict.Add(1, "Albion");
        realmDict.Add(2, "Midgard");
        realmDict.Add(3, "Hibernia");

        List<Achievement> HibAchievements = new List<Achievement>();
        List<Achievement> MidAchievements= new List<Achievement>();
        List<Achievement> AlbAchievements = new List<Achievement>();
        
        foreach (var achievement in achievements)
        {
            switch (achievement.Realm)
            {
                case 1:
                    AlbAchievements.Add(achievement);
                    break;
                case 2:
                    MidAchievements.Add(achievement);
                    break;
                case 3:
                    HibAchievements.Add(achievement);
                    break;
            }
        }

        if (HibAchievements.Count > 0)
        {
            temp.Add("Hibernia: ");
            foreach (var hibAchievement in HibAchievements)
            {
                temp.Add($"{hibAchievement.AchievementName} | {hibAchievement.Count}");
            }
        }
        
        temp.Add($"");

        if (AlbAchievements.Count > 0)
        {
            temp.Add("Albion: ");
            foreach (var albAchievement in AlbAchievements)
            {
                temp.Add($"{albAchievement.AchievementName} | {albAchievement.Count}");
            }
        }
        
        temp.Add($"");
        
        if (MidAchievements.Count > 0)
        {
            temp.Add("Midgard: ");
            foreach (var midAchievement in MidAchievements)
            {
                temp.Add($"{midAchievement.AchievementName} | {midAchievement.Count}");
            }
        }
        
        temp.Add($"");

        return temp;
    }
}