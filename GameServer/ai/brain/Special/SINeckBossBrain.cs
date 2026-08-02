using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain
{
    public class SINeckBossBrain : StandardMobBrain
    {
        private bool _despawnWarned;

        public override void Think()
        {
            if (!HasAggro && Body is SINeckBoss boss)
                boss.RoarAnnounced = false;

            if (Body.InCombatInLast(45 * 1000))
                _despawnWarned = false;
            else if (!_despawnWarned && Body.InCombatInLast(60 * 1000))
            {
                _despawnWarned = true;
                Body.Say("Face me, or I return to the stone.");
            }

            if (!Body.InCombatInLast(60 * 1000) && Body.InCombatInLast(65 * 1000)) // 60 seconds
            {
                Body.Say("Cowards..");
                Message.MessageToArea(Body, $"{Body.Name} sinks back into the earth and is gone.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
                Body.Delete();
            }
            base.Think();
        }
    }
}