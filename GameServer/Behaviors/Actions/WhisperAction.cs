using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Behaviour.Actions
{
    [Action(ActionType = EActionType.Whisper,DefaultValueQ=EDefaultValueConstants.NPC)]
    public class WhisperAction : AAction<string,GameNPC>
    {

        public WhisperAction(GameNPC defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.Whisper, p, q)
        {       
        }


        public WhisperAction(GameNPC defaultNPC, String message, GameNPC npc)
            : this(defaultNPC, (object)message, (object)npc) { }
        


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            String message = BehaviorUtil.GetPersonalizedMessage(P, player);
            Q.TurnTo(player);
            Q.Whisper(player, message);            
        }
    }
}