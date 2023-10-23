using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Scripts.Custom;

    public class PotionMerchant : GameBountyMerchant
    {
        #region Constructor

        public PotionMerchant()
            : base()
        {
            SetOwnBrain(new BlankBrain());
        }

        #endregion Constructor

        #region AddToWorld

        public override bool AddToWorld()
        {

           
            Level = 60;
            Name = "Potion Merchant";
            GuildName = "Potions";
            Model = 1903;
         
            MaxSpeedBase = 0;
            Realm = 0;
   
            TradeItems = new MerchantTradeItems("potion_merchant");

            return base.AddToWorld();
        }

        #endregion AddToWorld
        public override bool Interact(GamePlayer player)
        {
            TradeItems = new MerchantTradeItems("potion_merchant");
            player.Out.SendMerchantWindow(TradeItems, EMerchantWindowType.Normal);
            return true;
        }
    }