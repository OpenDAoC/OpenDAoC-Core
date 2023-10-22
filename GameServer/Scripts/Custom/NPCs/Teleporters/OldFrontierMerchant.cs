using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Players.Clients;

namespace Core.GS.Scripts.Custom;

public class OldFrontierMerchant : GameMerchant
{
    #region Constructor

    public OldFrontierMerchant()
        : base()
    {
        SetOwnBrain(new BlankBrain());
    }

    #endregion Constructor

    #region AddToWorld

    public override bool AddToWorld()
    {
        GuildName = "Medallion Merchant";
        Level = 75;
        Flags |= ENpcFlags.PEACE;

        switch (Realm)
        {
            case ERealm.Albion:
                Name = "Sall Fadri";
                Model = 61;
                TradeItems = new MerchantTradeItems("OFMerchant_Alb");
                break;
            case ERealm.Midgard:
                Name = "Gwulla";
                Model = 215;
                TradeItems = new MerchantTradeItems("OFMerchant_Mid");
                break;
            case ERealm.Hibernia:
                Name = "Araisa";
                Model = 342;
                TradeItems = new MerchantTradeItems("OFMerchant_Hib");
                break;
            default:
                break;
        }
     
        MaxSpeedBase = 0;


        return base.AddToWorld();
    }

    #endregion AddToWorld

}

public class OFMerchantHome : GameMerchant
{
    #region Constructor

    public OFMerchantHome()
        : base()
    {
        SetOwnBrain(new BlankBrain());
    }

    #endregion Constructor

    #region AddToWorld

    public override bool AddToWorld()
    {

        GuildName = "Medallion Merchant";
        Level = 75;
        Flags |= ENpcFlags.PEACE;

        switch (Realm)
        {
            case ERealm.Albion:
                Name = "Sall Fadri";
                Model = 61;
                if(CurrentRegionID == 200)
                    TradeItems = new MerchantTradeItems("OFMerchant_Alb_HomeHib");
                else if(CurrentRegionID == 100)
                    TradeItems = new MerchantTradeItems("OFMerchant_Alb_HomeMid");
                else 
                    TradeItems = new MerchantTradeItems("OFMerchant_Alb_Home");
                break;
            case ERealm.Midgard:
                Name = "Gwulla";
                Model = 215;
                if(CurrentRegionID == 200)
                    TradeItems = new MerchantTradeItems("OFMerchant_Mid_HomeHib");
                else if(CurrentRegionID == 1)
                    TradeItems = new MerchantTradeItems("OFMerchant_Mid_HomeAlb");
                else 
                    TradeItems = new MerchantTradeItems("OFMerchant_Mid_Home");
                break;
            case ERealm.Hibernia:
                Name = "Araisa";
                Model = 342;
                if(CurrentRegionID == 100)
                    TradeItems = new MerchantTradeItems("OFMerchant_Hib_HomeMid");
                else if(CurrentRegionID == 1)
                    TradeItems = new MerchantTradeItems("OFMerchant_Hib_HomeAlb");
                else 
                    TradeItems = new MerchantTradeItems("OFMerchant_Hib_Home");
                break;
        }
     
        MaxSpeedBase = 0;
        return base.AddToWorld();
    }

    #endregion AddToWorld


    public override bool Interact(GamePlayer player)
    {
        player.Out.SendMerchantWindow(TradeItems, EMerchantWindowType.Normal);
        return true;
    }
}