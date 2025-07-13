namespace DOL.GS
{
    public class NecromancerShadeECSGameEffect : ShadeECSGameEffect
    {
        protected int _timeRemaining = -1;

        public NecromancerShadeECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override long GetRemainingTimeForClient()
        {
            return _timeRemaining < 0 ? 0 : _timeRemaining * 1000;
        }

        public void SetTetherTimer(int seconds)
        {
            _timeRemaining = seconds;
        }
    }
}
