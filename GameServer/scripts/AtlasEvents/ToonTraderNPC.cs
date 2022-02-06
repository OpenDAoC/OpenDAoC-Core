/*
 * 
 * Atlas
 * NPC to turn in characters, track progress, and give rewards
 * 
 */

using System;
using System.Collections.Generic;
using DOL.Events;
using DOL.GS.PacketHandler;
using System.Reflection;
using DOL.Database;
using DOL.GS.ServerProperties;
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
            Model = 1198;
            Level = 50;
            Model = 2026;
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
                player.Out.SendMessage("Let me know when you are ready to [trade]", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            }
            
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            GamePlayer player = source as GamePlayer;

            if (player == null)
                return false;
            
            switch(str)
            {
                case "reward":
                    
                    player.Out.SendMessage("For every character that you trade, I will deposit some Atlas Orbs in your Account Vault.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    player.Out.SendMessage(
                        player.NoHelp
                            ? "I think your character is a worthy addition to my collection.\n\n I offer 25.000 Atlas Orbs for your character."
                            : "I like your look, \n\n, I offer 10.000 Atlas Orbs for your character.",
                        eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    player.Out.SendMessage("I will also reward your account some [titles] if you help me complete my collection.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    
                    return false;
                
                case "titles":
                    
                    player.Out.SendMessage("I am missing just a few characters to complete my collection and I am willing to trade my rare titles if you help me!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    
                    player.Out.SendMessage("ANY CHARACTER\n\n"+
                        "1. Trade 5 characters => 'Herculean Beetle'\n" +
                        "2. Trade 10 characters => 'Sisyphean Beetle'\n"
                        , eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    
                    player.Out.SendMessage("SOLO CHARACTERS\n\n"+
                                           "1. Trade 5 SOLO characters => 'The Punished'\n" +
                                           "2. Trade 10 SOLO characters => 'The Deranged'\n"
                        , eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    
                    player.Out.SendMessage($"You currently have traded with me {player.Client.Account.CharactersTraded} times.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    
                    return false;
                
                case "trade":

                    if (player.Level < 50)
                        return false;
                    
                    player.Out.SendMessage($"This is fantastic! I was just missing a {player.RaceName} {player.CharacterClass.Name}!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    
                    player.Out.SendMessage("Once added to my collection, this character will be removed from your account and won't be recoverable.\n\n" +
                        $"I will add the Orbs to your Account Vault on {GlobalConstants.RealmToName(player.Realm)} and keep track of your titles progress.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    
                    player.Out.SendMessage("Are you ready to [trade this character]?", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    
                    return false;
                
                case "trade this character":
                    player.Out.SendMessage("ok", eChatType.CT_Say, eChatLoc.CL_PopupWindow);


                    return false;
                
                default:
                    return false;
            }
        }

    }
}