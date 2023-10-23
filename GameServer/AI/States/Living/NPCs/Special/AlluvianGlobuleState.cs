using System;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.AI;

public class AlluvianGlobuleStateIdle : StandardNpcStateIdle
{
    public AlluvianGlobuleStateIdle(AlluvianGlobuleBrain brain) : base(brain)
    {
        StateType = EFsmStateType.IDLE;
    }

    public override void Enter()
    {
        Console.WriteLine($"{_brain.Body} is entering ALLUVIAN IDLE");
        base.Enter();
    }

    public override void Think()
    {
        if (_brain is AlluvianGlobuleBrain brain && brain.CheckStorm())
        {
            if (!brain.hasGrown)
                brain.Grow(); //idle
        }

        base.Think();
    }
}

public class AlluvianGlobuleStateRoaming : StandardNpcStateRoaming
{
    public AlluvianGlobuleStateRoaming(AlluvianGlobuleBrain brain) : base(brain)
    {
        StateType = EFsmStateType.ROAMING;
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
            _brain.Body.WalkTo(new Point3D(544196 + Util.Random(1, 3919), 514980 + Util.Random(1, 3200), 3140 + Util.Random(1, 540)), 80);
        }

        base.Think();
    }
}