namespace Core.GS.AI;

public class PygmyGoblinTanglerBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public PygmyGoblinTanglerBrain() : base()
    {
        ThinkInterval = 1500;
    }
    private bool BringGoblins = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
            BringGoblins = false;

        if(HasAggro && Body.TargetObject != null)
        {
            if(!BringGoblins)
            {
                foreach(GameNpc npc in Body.GetNPCsInRadius(500))
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if(npc != null && npc.IsAlive && npc.Name.ToLower() == "pygmy goblin" && npc.Brain is not ControlledNpcBrain && npc.Brain is StandardMobBrain brain)
                    {
                        if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
                            brain.AddToAggroList(target, 10);
                    }
                }
                BringGoblins = true;
            }
        }
        base.Think();
    }
}