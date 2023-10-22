using System.Reflection;
using Core.GS.Enums;
using Core.GS.Languages;
using log4net;

namespace Core.GS.World;

public class ZoneBonus
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    
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
    public static string GetBonusMessage(GamePlayer player, int bonusAmount, EZoneBonusType type)
    {
        System.Globalization.NumberFormatInfo format = System.Globalization.NumberFormatInfo.InvariantInfo;
        string bonusXP = bonusAmount.ToString("N0", format);
        
        switch (type)
        {
            case EZoneBonusType.XP:
                return LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalXP", bonusXP);
            case EZoneBonusType.RP:
                return LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalRP", bonusXP);
            case EZoneBonusType.BP:
                return LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalBP", bonusXP);
            case EZoneBonusType.COIN:
                return LanguageMgr.GetTranslation(player.Client.Account.Language, "ZoneBonus.AdditionalCoin");
            default: return "No Bonus Type Found";
        }
    } 
    #endregion

}