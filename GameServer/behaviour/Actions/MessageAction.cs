using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Behaviour.Actions
{
    [ActionAttribute(ActionType = eActionType.Message)]
    public class MessageAction : AbstractAction<string, eTextType>
    {
        public MessageAction(GameNPC defaultNPC, object p, object q) : base(defaultNPC, eActionType.Message, p, q) { }

        public MessageAction(GameNPC defaultNPC, string message, eTextType messageType) : this(defaultNPC, message, (object)messageType) { }

        public override void Perform(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviourUtils.GuessGamePlayerFromNotify(e, sender, args);
            string message = BehaviourUtils.GetPersonalizedMessage(P, player);

            switch (Q)
            {
                case eTextType.Dialog:
                {
                    player.Out.SendCustomDialog(message, null);
                    break;
                }
                case eTextType.Emote:
                {
                    player.Out.SendMessage(message, eChatType.CT_Emote, eChatLoc.CL_ChatWindow);
                    break;
                }
                case eTextType.Say:
                {
                    player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                    break;
                }
                case eTextType.SayTo:
                {
                    player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;
                }
                case eTextType.Yell:
                {
                    player.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
                    break;
                }
                case eTextType.Broadcast:
                {
                    foreach (GamePlayer otherPlayer in ClientService.GetPlayers())
                        otherPlayer.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);

                    break;
                }
                case eTextType.Read:
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.MessageAction.ReadMessage", message), eChatType.CT_Emote, eChatLoc.CL_PopupWindow);
                    break;
                }
                case eTextType.None:
                    break;
            }
        }
    }
}
