using System.Reflection;
using Core.GS;
using log4net;

namespace Core.AI.Brain
{
    public class HighLordBaelerdothBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HighLordBaelerdothBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }
        
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            base.Think();
        }
    }
}