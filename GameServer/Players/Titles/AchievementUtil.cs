using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;

namespace Core.GS.Players.Titles;

public class AchievementUtil
{
    public abstract class AchievementName
    {
        public const string Orbs_Earned = "Lifetime Orbs Earned";
        
        public const string Carapace_Farmed = "Lifetime Carapace Dropped";

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
        achievements.Add(AchievementName.Orbs_Earned);
        
        //pve
        achievements.Add(AchievementName.Dragon_Kills);
        achievements.Add(AchievementName.Epic_Boss_Kills);
        achievements.Add(AchievementName.Legion_Kills);
        
        //rvr
        achievements.Add(AchievementName.Realm_Rank);
        achievements.Add(AchievementName.Relic_Captures);
        achievements.Add(AchievementName.Keeps_Taken);
        
        achievements.Add(AchievementName.Players_Killed);
        achievements.Add(AchievementName.Alb_Players_Killed);
        achievements.Add(AchievementName.Hib_Players_Killed);
        achievements.Add(AchievementName.Mid_Players_Killed);
        
        achievements.Add(AchievementName.Alb_Deathblows);
        achievements.Add(AchievementName.Hib_Deathblows);
        achievements.Add(AchievementName.Mid_Deathblows);
        
        achievements.Add(AchievementName.Solo_Kills);
        achievements.Add(AchievementName.Alb_Solo_Kills);
        achievements.Add(AchievementName.Hib_Solo_Kills);
        achievements.Add(AchievementName.Mid_Solo_Kills);

        //crafting
        achievements.Add(AchievementName.Mastered_Crafts);

        return achievements;
    }

    public static IList<string> GetAchievementInfoForPlayer(GamePlayer player)
    {
        List<string> temp = new List<string>();
        temp.Clear();

        var achievements = CoreDb<DbAchievement>.SelectObjects(DB.Column("AccountID")
            .IsEqualTo(player.Client.Account.ObjectId));

        if (achievements == null) return temp;

        //need to do this to avoid displaying realm as _FirstRealm or _LastRealm
        Dictionary<int, string> realmDict = new Dictionary<int, string>();
        realmDict.Add(1, "Albion");
        realmDict.Add(2, "Midgard");
        realmDict.Add(3, "Hibernia");

        List<DbAchievement> HibAchievements = new List<DbAchievement>();
        List<DbAchievement> MidAchievements= new List<DbAchievement>();
        List<DbAchievement> AlbAchievements = new List<DbAchievement>();
        
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
    
    public static List<DbAchievement> GetAchievementInfoForPlayerPerRealm(GamePlayer player, int realm)
    {
        var achievements = CoreDb<DbAchievement>.SelectObjects(DB.Column("AccountID")
            .IsEqualTo(player.Client.Account.ObjectId));

        if (achievements == null) return null;
        
        var RealmAchievements = new List<DbAchievement>();
        
        foreach (var achievement in achievements)
        {
            if (achievement.Realm != realm) continue;
            RealmAchievements.Add(achievement);
        }
        
        return RealmAchievements;
    }

    public static bool CheckPlayerCredit(string mob, GamePlayer player, int realm)
    {
        var achievements = CoreDb<DbAchievement>.SelectObjects(DB.Column("AccountID")
            .IsEqualTo(player.Client.Account.ObjectId));
        var hasCredit = false;
        
        var achievementMob = Regex.Replace(mob, @"\s+", "").ToLower();

        // foreach (var achievement in achievements)
        // {
        //     if (achievement.Realm != realm) continue;
        //     if (achievement.AchievementName.ToLower() == $"{achievementMob}-credit") hasCredit = true;
        // }

        var credit = achievements.Where(achievement => achievement.Realm == realm)
            .Where(achievement => achievement.AchievementName.ToLower() == $"{achievementMob}-credit");

        if (credit.Any()) hasCredit = true;

        return hasCredit;
    }
    
    public static bool CheckAccountCredit(string mob, GamePlayer player)
    {
        var achievements = CoreDb<DbAchievement>.SelectObjects(DB.Column("AccountID")
            .IsEqualTo(player.Client.Account.ObjectId));
        var hasCredit = false;
        
        var achievementMob = Regex.Replace(mob, @"\s+", "").ToLower();
        
        var credit = achievements.Where(achievement => achievement.AchievementName.ToLower() == $"{achievementMob}-credit");

        if (credit.Any()) hasCredit = true;

        return hasCredit;
    }
}