using System;
using Core.Events;
using Core.GS.Behaviour.Attributes;

namespace Core.GS.Behaviour.Actions
{
    [Action(ActionType = EActionType.Whisper,DefaultValueQ=EDefaultValueConstants.NPC)]
    public class WhisperAction : AAction<string,GameNpc>
    {

        public WhisperAction(GameNpc defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.Whisper, p, q)
        {       
        }


        public WhisperAction(GameNpc defaultNPC, String message, GameNpc npc)
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