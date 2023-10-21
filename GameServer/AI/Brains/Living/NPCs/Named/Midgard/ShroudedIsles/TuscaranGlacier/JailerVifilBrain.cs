using System.Collections;
using System.Collections.Generic;
using Core.GS;

namespace Core.AI.Brain;

#region Jailer Vifil
public class JailerVifilBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public JailerVifilBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 2000;
    }

    public static bool IsPulled = false;

    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (IsPulled == false)
        {
            SpawnTunnelGuardians();
            IsPulled = true;
        }
        base.OnAttackedByEnemy(ad);
    }

    public void TeleportPlayer()
    {
        IList enemies = new ArrayList(AggroTable.Keys);
        foreach (GamePlayer player in Body.GetPlayersInRadius(1100))
        {
            if (player != null)
            {
                if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                {
                    if (!AggroTable.ContainsKey(player))
                        AggroTable.Add(player, 1);
                }
            }
        }
        if (enemies.Count == 0)
            return;
        else
        {
            List<GameLiving> damage_enemies = new List<GameLiving>();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null)
                    continue;
                if (!(enemies[i] is GameLiving))
                    continue;
                if (!(enemies[i] as GameLiving).IsAlive)
                    continue;
                GameLiving living = null;
                living = enemies[i] as GameLiving;
                if (living.IsVisibleTo(Body) && Body.TargetInView && living is GamePlayer)
                {
                    damage_enemies.Add(enemies[i] as GameLiving);
                }
            }
            if (damage_enemies.Count > 0)
            {
                GamePlayer PortTarget = (GamePlayer) damage_enemies[Util.Random(0, damage_enemies.Count - 1)];
                if (PortTarget.IsVisibleTo(Body) && Body.TargetInView && PortTarget != null && PortTarget.IsAlive)
                {
                    AggroTable.Remove(PortTarget);
                    AggroTable.Remove(PortTarget);
                    PortTarget.MoveTo(Body.CurrentRegionID, 16631, 58683, 10858, 2191);
                    PortTarget = null;
                }
            }
        }
    }
    public int PortTimer(EcsGameTimer timer)
    {
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DoPortTimer), 5000);
        return 0;
    }
    public int DoPortTimer(EcsGameTimer timer)
    {
        TeleportPlayer();
        spam_teleport = false;
        return 0;
    }
    private bool RemoveAdds = false;
    public static bool spam_teleport = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }

        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
            IsPulled = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(160))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && !npc.InCombat)
                        {
                            if (npc.Brain is JailerAddBrain && npc.RespawnInterval == -1)
                                npc.RemoveFromWorld();
                        }
                    }
                }
                RemoveAdds = true;
            }
        }

        if (Body.TargetObject != null && HasAggro)
        {
            RemoveAdds = false;
            if (spam_teleport == false && Body.TargetObject != null)
            {
                int rand = Util.Random(25000, 45000);
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PortTimer), rand);
                spam_teleport = true;
            }
        }
        base.Think();
    }

    public void SpawnTunnelGuardians()
    {
        JailerAdd Add1 = new JailerAdd();
        Add1.X = 16709;
        Add1.Y = 58973;
        Add1.Z = 10879;
        Add1.CurrentRegion = Body.CurrentRegion;
        Add1.Heading = 2088;
        Add1.AddToWorld();

        JailerAdd Add2 = new JailerAdd();
        Add2.X = 16379;
        Add2.Y = 58954;
        Add2.Z = 10885;
        Add2.CurrentRegion = Body.CurrentRegion;
        Add2.Heading = 2048;
        Add2.AddToWorld();
    }
}
#endregion Jailer Vifil

#region Jailer adds
public class JailerAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public JailerAddBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Jailer adds