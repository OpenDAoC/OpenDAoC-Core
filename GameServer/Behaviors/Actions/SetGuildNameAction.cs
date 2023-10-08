using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;

namespace DOL.GS.Behaviour.Actions
{
    [Action(ActionType = EActionType.SetGuildName,DefaultValueQ=EDefaultValueConstants.NPC)]
    public class SetGuildNameAction : AAction<string,GameNPC>
    {               
                
        public SetGuildNameAction(GameNPC defautNPC,  Object p, Object q)
            : base(defautNPC, EActionType.SetGuildName, p, q)
        { }

        public SetGuildNameAction(GameNPC defaultNPC, string guildName, GameNPC npc)
            : this(defaultNPC, (object)guildName, (object)npc) { }

        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {            
            Q.GuildName = P;
        }
    }
}