/*
 * 
 * Atlas
 * NPC to turn in characters, track progress, and give rewards
 * 
 */

using System;
using DOL.Events;
using DOL.GS.PacketHandler;
using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS.Scripts
{
    public class ToonTraderNPC : GameNPC
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override bool AddToWorld()
        {
            Name = "Adris";
            GuildName = "Famous Collector";
            Level = 50;
            Size = 60;
            Flags |= eFlags.PEACE;
            return base.AddToWorld();
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Toon trader NPC is loading...");
        }

        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }

        public override bool Interact(GamePlayer player)
        {

            if (!base.Interact(player))
                return false;

            base.TurnTo(player, 5000);

            player.Out.SendMessage($"Hello {player.Name}, my name is {Name} and I'm a collector.\n\n" +
                                   $"You are a wonderful {player.CharacterClass.Name} specimen, I was just looking for one!\n" +
                                   $"Would you mind if I added your {player.RaceName} to my collection?\n\n" +
                                   "I will [reward] you greatly.",
                eChatType.CT_Say, eChatLoc.CL_PopupWindow);

            if (player.Level == 50)
            {
                player.Out.SendMessage("Let me know when you are ready to [trade]", eChatType.CT_Say,
                    eChatLoc.CL_PopupWindow);
            }

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            GamePlayer player = source as GamePlayer;
            
            const string soloKey = "solo_to_50";
            var isSolo = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player?.ObjectId).And(DB.Column("KeyName").IsEqualTo(soloKey)));

            if (player == null)
                return false;

            switch (str)
            {
                case "reward":
                    
                    var orbAmount = 10000;
                    if (player.NoHelp || isSolo != null)
                    {
                        orbAmount = 25000;
                    }

                    player.Out.SendMessage(
                        "For every character that you trade, I will deposit some Atlas Orbs in your Account Vault.",
                        eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    player.Out.SendMessage($"I think your character is a worthy addition to my collection.\n I'll pay {orbAmount} Atlas Orbs for your character.",
                        eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    player.Out.SendMessage(
                        "I will also reward your account some rare [titles], if you help me complete my collection.",
                        eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    return false;

                case "titles":

                    player.Out.SendMessage(
                        "I am missing just a few characters to complete my collection and I am willing to trade my rare titles if you help me!",
                        eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    player.Out.SendMessage("ANY CHARACTER\n\n" +
                                           "1. Trade 5 characters => 'Herculean Beetle'\n" +
                                           "2. Trade 10 characters => 'Sisyphean Beetle'\n"
                        , eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    player.Out.SendMessage("SOLO CHARACTERS\n\n" +
                                           "1. Trade 5 SOLO characters => 'The Punished'\n" +
                                           "2. Trade 10 SOLO characters => 'The Deranged'\n"
                        , eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    player.Out.SendMessage(
                        $"You have traded with me {player.Client.Account.CharactersTraded} times ({player.Client.Account.SoloCharactersTraded}  solo).",
                        eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    return false;

                case "trade":

                    if (player.Level < 50)
                        return false;

                    player.Out.SendMessage(
                        $"This is fantastic! I was just missing a {player.RaceName} {player.CharacterClass.Name}!",
                        eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    player.Out.SendMessage(
                        "Once added to my collection, this character will be reset and moved to my taxidermy laboratory.\n\n" +
                        "You'll be able to hand your possessions to my assistant before bidding farewell to your character.",
                        eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    player.Out.SendMessage("Are you ready to [trade this character]?", eChatType.CT_Say,
                        eChatLoc.CL_PopupWindow);

                    return false;

                case "trade this character":
                    
                    if (player.Boosted){
                        player.Out.SendMessage("At the moment I'm only interested in characters that started from Level 1, sorry.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        return false;
                    }

                    player.Out.SendCustomDialog(
                        "Are you sure you want to trade this character?",
                        new CustomDialogResponse(CharacterTradeResponseHandler));

                    return false;

                default:
                    return false;
            }
        }

        protected virtual void CharacterTradeResponseHandler(GamePlayer player, byte response)
        {
            if (response == 1)
            {
                {
                    const string soloKey = "solo_to_50";
                    var isSolo = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player?.ObjectId).And(DB.Column("KeyName").IsEqualTo(soloKey)));
                    
                    var orbAmount = 10000;
                    player.Client.Account.CharactersTraded++;
                    
                    if (player.NoHelp || isSolo != null)
                    {
                        orbAmount = 25000;
                        player.Client.Account.SoloCharactersTraded++;
                    }
                    
                    player.Name = "DELETEME" + player.Name;
                    player.Reset();

                    player.MoveTo(249, 47400, 48686, 25000, 2085);

                    AtlasROGManager.GenerateOrbAmount(player, orbAmount);

                    GameServer.Database.SaveObject(player.Client.Account);

                }
            }
            else
            {
                player.Out.SendMessage("Come back if you change your mind!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            }
        }
    }
}

#region title
namespace DOL.GS.PlayerTitles
{
    public class TradeAnyTitleOne : SimplePlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "Herculean Beetle";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Herculean Beetle";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Herculean Beetle title!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return player.Client.Account.CharactersTraded >= 5;
        }
    }
    
    public class TradeAny10Title : SimplePlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "Sisyphean Beetle";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Sisyphean Beetle";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Sisyphean Beetle title!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return player.Client.Account.CharactersTraded >= 10;
        }
    }
    
    public class TradeSolo5Title : SimplePlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "The Punished";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "The Punished";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the The Punished title!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return player.Client.Account.SoloCharactersTraded >= 5;
        }
    }
    
    public class TradeSolo10Title : SimplePlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "The Deranged";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "The Deranged";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the The Deranged title!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return player.Client.Account.SoloCharactersTraded >= 10;
        }
    }
    
}
#endregion