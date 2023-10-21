using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Languages;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.Message)]
public class MessageAction : AAction<string, ETextType>
{
    public MessageAction(GameNpc defaultNPC, object p, object q) : base(defaultNPC, EActionType.Message, p, q) { }

    public MessageAction(GameNpc defaultNPC, string message, ETextType messageType) : this(defaultNPC, message, (object)messageType) { }

    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        string message = BehaviorUtil.GetPersonalizedMessage(P, player);

        switch (Q)
        {
            case ETextType.Dialog:
            {
                player.Out.SendCustomDialog(message, null);
                break;
            }
            case ETextType.Emote:
            {
                player.Out.SendMessage(message, EChatType.CT_Emote, EChatLoc.CL_ChatWindow);
                break;
            }
            case ETextType.Say:
            {
                player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
                break;
            }
            case ETextType.SayTo:
            {
                player.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_PopupWindow);
                break;
            }
            case ETextType.Yell:
            {
                player.Out.SendMessage(message, EChatType.CT_Help, EChatLoc.CL_ChatWindow);
                break;
            }
            case ETextType.Broadcast:
            {
                foreach (GamePlayer otherPlayer in ClientService.GetPlayers())
                    otherPlayer.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);

                break;
            }
            case ETextType.Read:
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.MessageAction.ReadMessage", message), EChatType.CT_Emote, EChatLoc.CL_PopupWindow);
                break;
            }
            case ETextType.None:
                break;
        }
    }
}