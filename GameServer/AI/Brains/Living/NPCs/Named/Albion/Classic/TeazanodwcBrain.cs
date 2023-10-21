using Core.GS.Enums;

namespace Core.GS.AI.Brains;

#region Teazanodwc
public class TeazanodwcBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public TeazanodwcBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    private bool _SpawnAdds = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            _SpawnAdds = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.RespawnInterval == -1 && npc.Brain is TeazanodwcAddBrain)
                            npc.Die(npc);
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if(!_SpawnAdds)
            {
                SpawnAdds();
                _SpawnAdds = true;
            }
        }
        base.Think();
    }
    private void SpawnAdds()
    {
        for (int i = 0; i < Util.Random(4,5); i++)
        {
            TeazanodwcAdd npc = new TeazanodwcAdd();
            npc.X = Body.X + Util.Random(-100, 100);
            npc.Y = Body.Y + Util.Random(-100, 100);
            npc.Z = Body.Z;
            npc.Heading = Body.Heading;
            npc.CurrentRegion = Body.CurrentRegion;
            npc.AddToWorld();
        }
    }
}
#endregion Teazanodwc

#region Teazanodwc add
public class TeazanodwcAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public TeazanodwcAddBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Teazanodwc add