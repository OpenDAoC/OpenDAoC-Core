using System.Reflection;
using DOL.Language;
using log4net;

namespace DOL.GS
{
    public class ZoneBonus
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region eZoneBonusType
        public enum eZoneBonusType
        {
            XP = 0,
            RP = 1,
            BP = 2,
            COIN = 3,
        } 
        #endregion
        #region Get Bonuses Methods
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
        #endregion
        #region Get Bonus Message
        public static string GetBonusMessage(GamePlayer player, int bonusAmount, eZoneBonusType type)
        {
            System.Globalization.NumberFormatInfo format = System.Globalization.NumberFormatInfo.InvariantInfo;
            string bonusXP = bonusAmount.ToString("N0", format);
            
            switch (type)
            {
                case eZoneBonusType.XP:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalXP", bonusXP);
                case eZoneBonusType.RP:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalRP", bonusXP);
                case eZoneBonusType.BP:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalBP", bonusXP);
                case eZoneBonusType.COIN:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalCoin");
                default: return "No Bonus Type Found";
            }
        } 
        #endregion

    }
}
