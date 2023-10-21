using DOL.GS;

namespace DOL.AI.Brain;

public class SkeaghsheeBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public SkeaghsheeBrain() : base()
    {
        AggroLevel = 0;//he is neutral
        AggroRange = 800;
        ThinkInterval = 1500;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166165);
            Body.ParryChance = npcTemplate.ParryChance;
        }
        if(Body.TargetObject != null && HasAggro)
        {
            float angle = Body.TargetObject.GetAngle(Body);
            if (angle >= 160 && angle <= 200)
            {
                Body.styleComponent.NextCombatStyle = Skeaghshee.behind;//do backstyle when angle allow it
                Body.styleComponent.NextCombatBackupStyle = Skeaghshee.behindFollowUp;
            }
            else
            {
                Body.ParryChance = 15;
                Body.styleComponent.NextCombatStyle = Skeaghshee.afterParry;//do backstyle when angle allow it
                Body.styleComponent.NextCombatBackupStyle = Skeaghshee.taunt;
            }
        }
        base.Think();
    }
}