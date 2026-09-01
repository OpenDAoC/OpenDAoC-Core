using DOL.GS;

namespace DOL.AI.Brain
{
    public class BdHealerBrain : BdPetBrain
    {
        public BdHealerBrain(GameLiving owner) : base(owner)
        {
            AggroLevel = 0;
            AggroRange = 0;
        }

        public override eAggressionState AggressionState
        {
            get => eAggressionState.Passive;
            set { }
        }

        public override void Attack(GameObject target) { }

        public override void AddToAggroList(GameLiving living, long aggroAmount, bool ignoreConfusion) { }

        public override bool RemoveFromAggroList(GameLiving living)
        {
            return false;
        }

        protected override GameLiving CalculateNextAttackTarget()
        {
            return null;
        }

        public override void AttackMostWanted() { }

        public override void OnOwnerAttacked(AttackData ad) { }
    }
}
