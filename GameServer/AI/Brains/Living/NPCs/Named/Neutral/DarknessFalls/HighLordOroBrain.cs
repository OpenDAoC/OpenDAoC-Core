using System.Reflection;
using Core.GS;
using log4net;

namespace Core.AI.Brain;

#region High Lord Oro
public class HighLordOroBrain : StandardMobBrain
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    bool isPulled;

    public HighLordOroBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 850;
    }

    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (!isPulled)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
            {
                if (npc?.Brain is OroCloneBrain && npc.IsAlive)
                {
                    if (npc.InCombat) continue;
                    AddAggroListTo(npc.Brain as OroCloneBrain); // add to aggro mobs with CryptLordBaf PackageID
                    isPulled = true;
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            isPulled = false;
        }
        else
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(350))
            {
                player.Mana -= (int) (player.MaxMana * 0.05);
                player.UpdateHealthManaEndu();
            }
        }
        base.Think();
    }
}
#endregion High Lord Oro

#region High Lord Oro Clone
public class OroCloneBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    bool isPulled;
    bool isPulled2;

    public OroCloneBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (!isPulled)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
            {
                if (npc?.Brain is OroCloneBrain)
                {
                    if (npc.InCombat || !npc.IsAlive) continue;
                    AddAggroListTo(npc.Brain as OroCloneBrain); // add to aggro mobs with CryptLordBaf PackageID
                    isPulled = true;
                }
            }
        }
        if(!isPulled2)
        {
            foreach (GameNpc npc2 in Body.GetNPCsInRadius(2500))
            {
                if (npc2?.Brain is HighLordOroBrain)
                {
                    if (npc2.InCombat || !npc2.IsAlive) continue;
                    AddAggroListTo(npc2.Brain as HighLordOroBrain); // add to aggro mobs with CryptLordBaf PackageID
                    isPulled2 = true;
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            isPulled = false;
            isPulled2 = false;
        }
        else
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(350))
            {
                player.Mana -= (int) (player.MaxMana * 0.05);
                player.UpdateHealthManaEndu();
            }
        }
        base.Think();
    }
}
#endregion High Lord Oro Clone