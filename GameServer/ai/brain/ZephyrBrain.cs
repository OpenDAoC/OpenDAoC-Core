using System;
using DOL.GS;

namespace DOL.AI.Brain
{
    public class ZephyrBrain : ABrain
    {
        private Action<GameNPC> _arriveAtTargetCallback;
        private bool _caughtTarget;

        public override int ThinkInterval => 300;

        public ZephyrBrain(Action<GameNPC> arriveAtTargetCallback) : base()
        {
            _arriveAtTargetCallback = arriveAtTargetCallback;
        }

        public override void Think()
        {
            if (!_caughtTarget && Body.IsWithinRadius(Body.TargetObject, 100))
            {
                _caughtTarget = true;
                _arriveAtTargetCallback(Body);
            }
        }

        public override void KillFSM() { }
    }
}
