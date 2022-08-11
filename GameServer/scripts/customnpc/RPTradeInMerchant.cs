using System;
using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.Events;
using log4net;
using DOL.AI.Brain;


namespace DOL.GS.Scripts
{
    public class RPTradeInMerchant : GameNPC
    {
        #region Constructor

        public RPTradeInMerchant()
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

            player.Out.SendMessage("Greetings, " + player.Name + ". I'd be happy to take some realm points off your hands. The process can feel a bit painful, but weathering the trial is not without [prestige].", eChatType.CT_System, eChatLoc.CL_PopupWindow);
            
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

