using System;
using System.Linq;
using DOL.GS;

namespace DOL.AI.Brain
{
    public class ScoutMobState_AGGRO(StandardMobBrain brain) : StandardMobState_AGGRO(brain)
    {
        private const int STARE_DURATION = 5000;              // How long we stare at our target before doing something.
        private const int FRIENDLY_TO_LOOK_FOR_RADIUS = 3000; // Radius at which we look for a friendly target to report our target to.
        private const int REPORT_RANGE = 150;                 // Minimum distance before we can communicate with our friendly target.
        private const int ADDS_RADIUS = 400;                  // Radius at which friendlies around our friendly target are allowed to join.
        private const int MAX_ADDS = 4;                       // Maximum amount of adds. This doesn't include our friendly target, but includes us.
        private ScoutMobState _state;
        private GameLiving _target;
        private StandardMobBrain _friend;
        private ScoutMobBrain _scoutBrain;
        private long _staringEndTime;

        public override void Enter()
        {
            _scoutBrain = _brain as ScoutMobBrain;

            if (_scoutBrain.IsScoutingInterrupted)
            {
                base.Enter();
                return;
            }

            _target = _brain.GetOrderedAggroList().FirstOrDefault().Item1;

            if (_target == null)
            {
                _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
                return;
            }

            FaceTarget();
            base.Enter();

            void FaceTarget()
            {
                _state = ScoutMobState.FACING_TARGET;
                _brain.Body.StopMoving();
                _brain.Body.TurnTo(_target);

                GamePlayer playerToNotify = null;

                if (_target is GameNPC targetNpc && targetNpc.Brain is ControlledMobBrain targetBrain)
                    targetBrain.GetPlayerOwner();
                else
                    playerToNotify = _target as GamePlayer;

                playerToNotify?.Out.SendMessage($"{_brain.Body.GetName(0, true)} is alerted by your presence.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                _staringEndTime = GameLoop.GameLoopTime + STARE_DURATION;
            }
        }

        public override void Exit()
        {
            _scoutBrain.IsScoutingInterrupted = false;
        }

        public override void Think()
        {
            // Stare at our target => Find a friendly target => Move towards it => Propagate aggro to friendlies around it => Fight.
            // We're supposed to be allowed to be interrupted by attacks at anytime and join the fight alone.

            if (_state is ScoutMobState.FIGHTING || _scoutBrain.IsScoutingInterrupted)
                base.Think();
            else if (_state is ScoutMobState.FACING_TARGET)
            {
                if (ServiceUtils.ShouldTick(_staringEndTime))
                    LookForFriends();
                else
                    StareAtTarget();
            }
            else if (_state is ScoutMobState.REPORTING_TARGET)
                ReportToFriends();

            void StareAtTarget()
            {
                if (!_target.IsAlive || _target.ObjectState != GameObject.eObjectState.Active)
                {
                    _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
                    return;
                }

                _brain.Body.TurnTo(_target);
            }

            void LookForFriends()
            {
                _friend = _brain.GetFriendlyAndAvailableBrainsInRadiusOrderedByDistance(FRIENDLY_TO_LOOK_FOR_RADIUS, 1).FirstOrDefault();

                if (_friend == null)
                {
                    _state = ScoutMobState.FIGHTING;
                    base.Think();
                    return;
                }

                // This may not be enough if the target is moving too fast or if we're thinking too slowly.
                _brain.Body.Follow(_friend.Body, Math.Max(0, REPORT_RANGE - 50), int.MaxValue);
                _state = ScoutMobState.REPORTING_TARGET;
            }

            void ReportToFriends()
            {
                // Our friend may die before we reach it, but it should be fine.
                if (!_brain.Body.IsWithinRadius(_friend.Body, REPORT_RANGE))
                    return;

                _brain.AddAggroListTo(_friend);
                _friend.AttackMostWanted();

                foreach (StandardMobBrain otherFriendlyBrain in _friend.GetFriendlyAndAvailableBrainsInRadiusOrderedByDistance(ADDS_RADIUS, MAX_ADDS))
                {
                    // This includes us.
                    _brain.AddAggroListTo(otherFriendlyBrain);
                    otherFriendlyBrain.AttackMostWanted();
                }

                _state = ScoutMobState.FIGHTING;
            }
        }

        private enum ScoutMobState
        {
            NONE,
            FACING_TARGET,
            REPORTING_TARGET,
            FIGHTING
        }
    }

    public class ScoutMobState_ROAMING(StandardMobBrain brain) : StandardMobState_ROAMING(brain)
    {
        protected override int MinCooldown => 500;
        protected override int MaxCooldown => 500;
    }
}
