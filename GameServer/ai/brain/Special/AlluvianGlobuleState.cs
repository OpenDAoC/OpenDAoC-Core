using DOL.AI;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.Movement;
using FiniteStateMachine;
using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using static DOL.AI.Brain.StandardMobBrain;

public class AlluvianGlobuleState_IDLE : StandardMobState_IDLE
{
    public AlluvianGlobuleState_IDLE(FSM fsm, AlluvianGlobuleBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.IDLE;
    }

    public override void Enter()
    {
        Console.WriteLine($"{_brain.Body} is entering ALLUVIAN IDLE");
        base.Enter();
    }

    public override void Think()
    {
        if ((_brain as AlluvianGlobuleBrain).CheckStorm())
        {
            if (!(_brain as AlluvianGlobuleBrain).hasGrown)
            {
                (_brain as AlluvianGlobuleBrain).Grow(); //idle
            }
        }
        base.Think();
    }
}

public class AlluvianGlobuleState_ROAMING : StandardMobState_ROAMING
{
    public AlluvianGlobuleState_ROAMING(FSM fsm, AlluvianGlobuleBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.ROAMING;
    }

    public override void Enter()
    {
        Console.WriteLine($"{_brain.Body} is entering ALLUVIAN ROAM");
        base.Enter();
    }

    public override void Think()
    {
        if (!_brain.Body.attackComponent.AttackState && !_brain.Body.IsMoving && !_brain.Body.InCombat)
        {
            // loc range around the lake that Alluvian spanws.
            _brain.Body.WalkTo(544196 + Util.Random(1, 3919), 514980 + Util.Random(1, 3200), 3140 + Util.Random(1, 540), 80);
        }
        base.Think();
    }
}


