using System;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
    public class RpTradeInMerchant : GameNPC
    {
        #region Constructor

        public RpTradeInMerchant()
            : base()
        {
            SetOwnBrain(new BlankBrain());
        }

        #endregion Constructor

        #region AddToWorld

        public override bool AddToWorld()
        {

            Level = 75;
            Name = "Void Merchant";
            GuildName = "Realm Point Trade-In";
            Model = 2212;
            Size = 80;
            //TradeItems = new MerchantTradeItems("alpha_respecstones");
            MaxSpeedBase = 0;
            Flags |= eFlags.PEACE;

            return base.AddToWorld();
        }

        #endregion AddToWorld

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            TurnTo(player, 100);

            player.Out.SendMessage("Greetings, " + player.Name + ". I'd be happy to take some realm points off your hands. The process can feel a bit painful, but weathering the trial is not without [prestige].", EChatType.CT_System, EChatLoc.CL_PopupWindow);
            
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;
            if (source is GamePlayer player == false)
                return true;

            if (text.ToLower().Equals("prestige"))
            {
                player.Out.SendCustomDialog(
                    "Your realm points will be set to ZERO. Continue?",
                    new CustomDialogResponse(PrestigeResponseHandler));
            }
            
            return true;
        }

        protected virtual void PrestigeResponseHandler(GamePlayer player, byte response)
        {
            if (response == 1)
            {
                Console.WriteLine($"Before Level: {player.RealmLevel} | Points: {player.RealmPoints}");
                player.RealmLevel = 0;
                player.RealmPoints = 0;
                player.RespecRealm(false);
                player.Out.SendUpdatePlayer();
                player.Out.SendUpdatePoints();
                //player.Achieve();
                Console.WriteLine($"Player RR reset. Level: {player.RealmLevel} | Points: {player.RealmPoints}");
            }
        }
    }
    
 }

