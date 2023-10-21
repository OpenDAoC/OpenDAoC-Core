using System;
using Core.GS;
using Core.GS.PacketHandler;

namespace Core.AI.Brain;

public class KingTuscarBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public KingTuscarBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    #region BroadcastMessage & OnAttackedByEnemy()
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool message2 = false;
    public static bool TuscarRage = false;
    public static bool IsPulled2 = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc != null)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (npc.IsAlive && npc.Brain is QueenKulaBrain brain)
                    {
                        if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
                            brain.AddToAggroList(target, 10);
                    }
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    #endregion
    #region Think()
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162909);
            Body.Strength = npcTemplate.Strength;
            TuscarRage = false;
            IsPulled2 = false;
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
            message2 = false;
        }
        if (Body.InCombat && HasAggro)
        {
            if (message2 == false)
            {
                BroadcastMessage(String.Format("King Tuscar raises his weapon and yells, 'Kula wields the finest weapon I have ever made!" +
                    " And the weapon I forged for myself is almost as good in combat! Death comes swiftly with these two weapons!'"));
                message2 = true;
            }
            if(Body.HealthPercent<=50 && TuscarRage==false)
            {
                BroadcastMessage(String.Format("King Tuscar rages and gains strength from Odin!"));
                TuscarRage = true;
            }
            if (Body.TargetObject != null)
            {
                GameLiving living = Body.TargetObject as GameLiving;
                if(QueenKula.QueenKulaCount == 1)
                {
                    Body.Strength = 350;
                }
                if (QueenKula.QueenKulaCount == 0 || Body.HealthPercent <= 50)
                {
                    Body.Strength = 500;
                }
            }
        }
        base.Think();
    }
    #endregion
}