using System;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Packets;
using Core.GS.Packets.Server;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.CustomDialog)]
public class CustomDialogAction : AAction<string, CustomDialogResponse>
{               

    public CustomDialogAction(GameNpc defaultNPC, Object p, Object q)
        : base(defaultNPC, EActionType.CustomDialog, p, q)
    {                
    }


    public CustomDialogAction(GameNpc defaultNPC, string message, CustomDialogResponse customDialogResponse)
        : this(defaultNPC,  (object)message, (object)customDialogResponse) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

        string message = BehaviorUtil.GetPersonalizedMessage(P, player);            
        player.Out.SendCustomDialog(message, Q);
    }
}