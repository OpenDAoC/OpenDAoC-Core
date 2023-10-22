using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Players.Clients;

namespace Core.GS.Scripts
{
    public class RespecMerchant : GameMerchant
    {
        #region Constructor

        public RespecMerchant()
            : base()
        {
            SetOwnBrain(new BlankBrain());
        }

        #endregion Constructor

        #region AddToWorld

        public override bool AddToWorld()
        {

            Level = 75;
            Name = "Free Respec Stones";
            Model = 136;
            Size = 80;
            TradeItems = new MerchantTradeItems("alpha_respecstones");
            MaxSpeedBase = 0;
            Flags |= ENpcFlags.PEACE;

            return base.AddToWorld();
        }

        #endregion AddToWorld

    }
    
 }

