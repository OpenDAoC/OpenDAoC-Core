using System;
using DOL.GS;

namespace DOL.AI.Brain
{
    public class AlluvianGlobuleState_IDLE : StandardMobState_IDLE
    {
        public AlluvianGlobuleState_IDLE(AlluvianGlobuleBrain brain) : base(brain)
        {
            StateType = eFSMStateType.IDLE;
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

    public class AlluvianGlobuleState_ROAMING : StandardMobState_ROAMING
    {
        public AlluvianGlobuleState_ROAMING(AlluvianGlobuleBrain brain) : base(brain)
        {
            StateType = eFSMStateType.ROAMING;
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
}
