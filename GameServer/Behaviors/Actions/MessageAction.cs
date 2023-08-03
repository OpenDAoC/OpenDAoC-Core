using System;
using System.Collections.Generic;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using DOL.GS.Behaviour;
using DOL.Language;

namespace DOL.GS.Behaviour.Actions
{
    [ActionAttribute(ActionType = EActionType.Message)]
    public class MessageAction : AbstractAction<String,ETextType>
    {

        public MessageAction(GameNpc defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.Message, p, q)
        {                           
        }


        public MessageAction(GameNpc defaultNPC, String message, ETextType messageType)
            : this(defaultNPC, (object)message, (object)messageType) { }
        


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtils.GuessGamePlayerFromNotify(e, sender, args);
            String message = BehaviorUtils.GetPersonalizedMessage(P, player);
            switch (Q)
            {
                case ETextType.Dialog:
                    player.Out.SendCustomDialog(message, null);
                    break;
                case ETextType.Emote:
                    player.Out.SendMessage(message, EChatType.CT_Emote, EChatLoc.CL_ChatWindow);
                    break;
				case ETextType.Say:
					player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
					break;
				case ETextType.SayTo:
					player.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_PopupWindow);
					break;
				case ETextType.Yell:
					player.Out.SendMessage(message, EChatType.CT_Help, EChatLoc.CL_ChatWindow);
					break;
                case ETextType.Broadcast:
                    foreach (GameClient clientz in WorldMgr.GetAllPlayingClients())
                    {
                        clientz.Player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
                    }
                    break;
                case ETextType.Read:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.MessageAction.ReadMessage", message), EChatType.CT_Emote, EChatLoc.CL_PopupWindow);
                    break;  
                case ETextType.None:
                    //nohting
                    break;
            }
        }
    }
}