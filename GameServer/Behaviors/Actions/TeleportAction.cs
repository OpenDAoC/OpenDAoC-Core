using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Behaviour.Actions
{
    [Action(ActionType = EActionType.Teleport,DefaultValueQ=0)]
    public class TeleportAction : AAction<GameLocation,int>
    {               

        public TeleportAction(GameNpc defaultNPC,  Object p, Object q)
            : base(defaultNPC, EActionType.Teleport, p, q)
        {            
            }


        public TeleportAction(GameNpc defaultNPC,  GameLocation location, int fuzzyRadius)
            : this(defaultNPC,  (object)location, (object)fuzzyRadius) { }
        


        public override void Perform(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            GameLocation location = P;
            int radius = Q;

            if (location.Name != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.TeleportAction.TeleportedToLoc", player, location.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            }

            location.X += Util.Random(-radius, radius);
            location.Y += Util.Random(-radius, radius);
            player.MoveTo(location.RegionID, location.X, location.Y, location.Z, location.Heading);
        }
    }
}