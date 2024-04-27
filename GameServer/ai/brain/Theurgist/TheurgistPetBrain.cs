using DOL.GS;

namespace DOL.AI.Brain
{
    public class TheurgistPetBrain : ControlledMobBrain
    {
        private GameObject _target;

        public TheurgistPetBrain(GameLiving owner) : base(owner)
        {
            IsMainPet = false;
        }

        public override void Think()
        {
            _target = Body.TargetObject;

            if (_target == null || _target.Health <= 0)
            {
                Body.Die(null);
                return;
            }

            if (!CheckSpells(eCheckSpellType.Offensive))
                Body.StartAttack(_target);
        }

        public override eWalkState WalkState { get => eWalkState.Stay; set { } }
        public override eAggressionState AggressionState { get => eAggressionState.Aggressive; set { } }
        public override void Attack(GameObject target) { }
        public override void Disengage() { }
        public override void Follow(GameObject target) { }
        public override void FollowOwner() { }
        public override void Stay() { }
        public override void ComeHere() { }
        public override void Goto(GameObject target) { }
        public override void UpdatePetWindow() { }
        public override void OnAttackedByEnemy(AttackData ad) { }
    }
}
