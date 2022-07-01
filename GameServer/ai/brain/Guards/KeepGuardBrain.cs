using System.Reflection;
using DOL.GS;
using DOL.GS.Keeps;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.AI.Brain;

/// <summary>
///     Brain Class for Area Capture Guards
/// </summary>
public class KeepGuardBrain : StandardMobBrain
{
    public const string LAST_LOS_TARGET_PROPERTY = "last_LOS_checkTarget";
    public const string LAST_LOS_TICK_PROPERTY = "last_LOS_checkTick";

    /// <summary>
    ///     Defines a logger for this class.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public GameKeepGuard guard;

    /// <summary>
    ///     Constructor for the Brain setting default values
    /// </summary>
    public KeepGuardBrain()
    {
        AggroLevel = 90;
        AggroRange = 1000;
    }

    public override int ThinkInterval => 500;

    public void SetAggression(int aggroLevel, int aggroRange)
    {
        AggroLevel = aggroLevel;
        AggroRange = aggroRange;
    }

    public override void AttackMostWanted()
    {
        //Commenting out LOS Check on AttackMostWanted - Caused issues with not aggroing or switching aggro target
        // if(Body.TargetObject != null && Body.TargetObject is GamePlayer pl)
        // {
        // 	pl.Out.SendCheckLOS(Body, pl, new CheckLOSResponse(CheckAggroLOS));

        //     if (!AggroLOS) { return; }
        // }

        base.AttackMostWanted();
    }

    /// <summary>
    ///     Actions to be taken on each Think pulse
    /// </summary>
    public override void Think()
    {
        if (guard == null)
            guard = Body as GameKeepGuard;
        if (guard == null)
        {
            Stop();
            base.KillFSM();
            return;
        }

        if (Body.TargetObject != null && Body.TargetObject is GamePlayer pl)
            pl.Out.SendCheckLOS(Body, pl, CheckAggroLOS);

        if (guard is GuardArcher || guard is GuardStaticArcher || guard is GuardLord)
        {
            // Drop aggro and disengage if the target is out of range or out of LoS.
            if (Body.IsAttacking && Body.TargetObject is GameLiving living &&
                Body.IsWithinRadius(Body.TargetObject, AggroRange) == false) //|| !AggroLOS))
            {
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                //Body.StopAttack();
                RemoveFromAggroList(living);
                //Body.TargetObject = null;
            }

            if (guard.attackComponent.AttackState && guard.CanUseRanged) guard.SwitchToRanged(guard.TargetObject);
        }

        ////if we are not doing an action, let us see if we should move somewhere
        //if (guard.CurrentSpellHandler == null && !guard.IsMoving && !guard.attackComponent.AttackState && !guard.InCombat)
        //{
        //	// Tolakram - always clear the aggro list so if this is done by mistake the list will correctly re-fill on next think
        //	ClearAggroList();

        //	if (guard.GetDistanceTo(guard.SpawnPoint, 0) > 50)
        //	{
        //		FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        //		//guard.WalkToSpawn();
        //	}
        //}

        //Eden - Portal Keeps Guards max distance
        if (guard.Level > 200 && !guard.IsWithinRadius(guard.SpawnPoint, 2000))
            FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        //ClearAggroList();
        //guard.WalkToSpawn();
        else if (guard.InCombat == false && guard.IsWithinRadius(guard.SpawnPoint, 6000) == false)
            FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        //ClearAggroList();
        //guard.WalkToSpawn();

        // We want guards to check aggro even when they are returning home, which StandardMobBrain does not, so add checks here
        if (guard.CurrentSpellHandler == null && !guard.attackComponent.AttackState && !guard.InCombat)
        {
            CheckPlayerAggro();
            CheckNPCAggro();

            if (HasAggro && Body.IsReturningHome)
                FSM.SetCurrentState(eFSMStateType.AGGRO);
            //Body.StopMoving();
            //AttackMostWanted();
        }

        base.Think();
    }

    /// <summary>
    ///     Check Area for Players to attack
    /// </summary>
    public override void CheckPlayerAggro()
    {
        if (Body.AttackState || Body.CurrentSpellHandler != null) return;

        foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
        {
            if (player == null) break;
            if (GameServer.ServerRules.IsAllowedToAttack(Body, player, true))
            {
                if (!Body.IsWithinRadius(player, AggroRange))
                    continue;
                if ((Body as GameKeepGuard).Component != null &&
                    !GameServer.KeepManager.IsEnemy(Body as GameKeepGuard, player, true))
                    continue;
                if (player.EffectList.GetOfType(typeof(NecromancerShadeECSGameEffect)) != null)
                    continue;

                if (Body is GuardStealther == false && player.IsStealthed)
                    continue;
                if (m_aggroTable.ContainsKey(player))
                    continue;

                var lastTarget = (GameObject) Body.TempProperties.getProperty<object>(LAST_LOS_TARGET_PROPERTY, null);
                if (lastTarget != null && lastTarget == player)
                {
                    var lastTick = Body.TempProperties.getProperty<long>(LAST_LOS_TICK_PROPERTY);
                    if (lastTick != 0 &&
                        GameLoop.GameLoopTime - lastTick < Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                        return;
                }

                if (Properties.ENABLE_DEBUG) Body.Say("Want to attack player " + player.Name);
                // PortalKeep Guard ont besoin de ne pas avoir ce check
                if ((Body as GameKeepGuard).IsPortalKeepGuard || Body.Level == 255)
                {
                    AddToAggroList(player, 1, true);
                    return;
                }

                if (Properties.ALWAYS_CHECK_LOS)
                {
                    var lastTick = Body.TempProperties.getProperty<long>(LAST_LOS_TARGET_PROPERTY);
                    lock (LOS_LOCK)
                    {
                        var count = Body.TempProperties.getProperty(NUM_LOS_CHECKS_INPROGRESS, 0);

                        if (count > 5)
                        {
                            // Now do a safety check.  If it's been a while since we sent any check we should clear count
                            if (lastTick == 0 || GameLoop.GameLoopTime - lastTick >
                                Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                                Body.TempProperties.setProperty(NUM_LOS_CHECKS_INPROGRESS, 0);

                            continue;
                        }

                        count++;
                        Body.TempProperties.setProperty(NUM_LOS_CHECKS_INPROGRESS, count);

                        Body.TempProperties.setProperty(LAST_LOS_TARGET_PROPERTY, player);
                        Body.TempProperties.setProperty(LAST_LOS_TICK_PROPERTY, GameLoop.GameLoopTime);
                        m_targetLOSObject = player;
                    }

                    var losChecker = LosChecker(Body, player); // devrait renvoyer "player"

                    if (losChecker != null)
                    {
                        CheckLOS(losChecker, player);
                        TempCheckLOS(losChecker, player, 1, true);
                        losChecker.Out.SendCheckLOS(Body, player, BeforeAddToAggro_CheckLOS);
                    }
                    else
                    {
                        AddToAggroList(player, 1, true);
                        return;
                    }
                }
                else
                {
                    AddToAggroList(player, 1, true);
                }

                // Fin
                return;
            }
        }
    }

    /// <summary>
    ///     Check area for NPCs to attack
    /// </summary>
    public override void CheckNPCAggro()
    {
        if (Body.AttackState || Body.CurrentSpellHandler != null)
            return;

        foreach (GameNPC npc in Body.GetNPCsInRadius((ushort) AggroRange))
        {
            if (npc == null || npc.Brain == null || npc is GameKeepGuard || npc.Brain as IControlledBrain == null)
                continue;

            if (m_aggroTable.ContainsKey(npc))
                continue;

            var player = (npc.Brain as IControlledBrain).GetPlayerOwner();

            if (player == null)
                continue;
            if (GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
            {
                if ((Body as GameKeepGuard).Component != null &&
                    !GameServer.KeepManager.IsEnemy(Body as GameKeepGuard, player, true)) continue;

                if (Properties.ENABLE_DEBUG) Body.Say("Want to attack player " + player.Name + " pet " + npc.Name);
                var lastTarget = (GameObject) Body.TempProperties.getProperty<object>(LAST_LOS_TARGET_PROPERTY, null);
                if (lastTarget != null && lastTarget == npc)
                {
                    var lastTick = Body.TempProperties.getProperty<long>(LAST_LOS_TICK_PROPERTY);
                    if (lastTick != 0 &&
                        GameLoop.GameLoopTime - lastTick < Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                        return;
                }

                if ((Body as GameKeepGuard).IsPortalKeepGuard || Body.Level == 255)
                {
                    AddToAggroList(npc, 1, true);
                    return;
                }

                if (Properties.ALWAYS_CHECK_LOS)
                {
                    var lastTick = Body.TempProperties.getProperty<long>(LAST_LOS_TARGET_PROPERTY);
                    lock (LOS_LOCK)
                    {
                        var count = Body.TempProperties.getProperty(NUM_LOS_CHECKS_INPROGRESS, 0);

                        if (count > 5)
                        {
                            // Now do a safety check.  If it's been a while since we sent any check we should clear count
                            if (lastTick == 0 || GameLoop.GameLoopTime - lastTick >
                                Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                                Body.TempProperties.setProperty(NUM_LOS_CHECKS_INPROGRESS, 0);

                            continue;
                        }

                        count++;
                        Body.TempProperties.setProperty(NUM_LOS_CHECKS_INPROGRESS, count);

                        Body.TempProperties.setProperty(LAST_LOS_TARGET_PROPERTY, npc);
                        Body.TempProperties.setProperty(LAST_LOS_TICK_PROPERTY, GameLoop.GameLoopTime);
                        m_targetLOSObject = npc;
                    }

                    var losChecker = LosChecker(Body, npc);
                    if (losChecker == null)
                    {
                    }
                    else
                    {
                        CheckLOS(losChecker, npc);
                        TempCheckLOS(losChecker, npc, (npc.Level + 1) << 1, true);
                        losChecker.Out.SendCheckLOS(Body, npc, BeforeAddToAggro_CheckLOS);
                    }
                }
                else
                {
                    AddToAggroList(npc, 1, true);
                }

                return;
            }
        }
    }

    public override int CalculateAggroLevelToTarget(GameLiving target)
    {
        GamePlayer checkPlayer = null;
        if (target is GameNPC && (target as GameNPC).Brain is IControlledBrain)
            checkPlayer = ((target as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
        if (target is GamePlayer)
            checkPlayer = target as GamePlayer;
        if (checkPlayer == null)
            return 0;
        if (GameServer.KeepManager.IsEnemy(Body as GameKeepGuard, checkPlayer, true))
            return AggroLevel;
        return 0;
    }

    /*
    public override bool AggroLOS
    {
        get { return true; }
    }*/
}