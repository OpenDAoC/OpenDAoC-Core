using DOL.AI.Brain;

namespace DOL.GS.Scripts
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
            Flags |= eFlags.PEACE;

            return base.AddToWorld();
        }

        #endregion AddToWorld

    }
    
 }

