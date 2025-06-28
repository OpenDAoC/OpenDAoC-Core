using System.Globalization;
using DOL.Language;

namespace DOL.GS
{
    public class ZoneBonus
    {
        public static int GetXPBonus(GamePlayer player)
        {
            return player.CurrentZone.BonusExperience;
        }

        public static int GetRPBonus(GamePlayer player)
        {
            return player.CurrentZone.BonusRealmpoints;
        }

        public static int GetBPBonus(GamePlayer player)
        {
            return player.CurrentZone.BonusBountypoints;
        }

        public static int GetCoinBonus(GamePlayer player)
        {
            return player.CurrentZone.BonusCoin;
        }

        public static string GetBonusMessage(GamePlayer player, long bonusAmount, ZoneBonusType type)
        {
            NumberFormatInfo format = NumberFormatInfo.InvariantInfo;
            string bonusXP = bonusAmount.ToString("N0", format);

            return type switch
            {
                ZoneBonusType.Xp => LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalXP", bonusXP),
                ZoneBonusType.Rp => LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalRP", bonusXP),
                ZoneBonusType.Bp => LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalBP", bonusXP),
                ZoneBonusType.Money => LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalCoin"),
                _ => "No Bonus Type Found",
            };
        }
    }

    public enum ZoneBonusType
    {
        Xp = 0,
        Rp = 1,
        Bp = 2,
        Money = 3,
    }
}
