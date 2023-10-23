using Core.GS.Enums;

namespace Core.GS.AI
{
	public class ProcPetBrain : StandardMobBrain, IControlledBrain
	{
		private GameLiving m_owner;
		private GameLiving m_target;

		public ProcPetBrain(GameLiving owner)
		{
			m_owner = owner;
			if (owner.TargetObject as GameLiving != null)
				m_target = m_owner.TargetObject as GameLiving;
			AggroLevel = 100;
			IsMainPet = false;
        }

        public virtual GameNpc GetNPCOwner()
        {
            return null;
        }
        public virtual GameLiving GetLivingOwner()
        {
            GamePlayer player = GetPlayerOwner();
            if (player != null)
                return player;

            GameNpc npc = GetNPCOwner();
            if (npc != null)
                return npc;

            return null;
        }

		public override int ThinkInterval { get { return 1500; } }

		public override void Think() { AttackMostWanted(); }

		public void SetAggressionState(EAggressionState state) { }

		public override void AttackMostWanted()
		{
			if (!IsActive || m_target == null) return;
			GameLiving target = m_target;
			if (target != null && target.IsAlive)
				Body.StartAttack(target);
			else
			{
				m_target = null;
				Body.LastAttackTickPvP = 0;
				Body.LastAttackTickPvE = 0;
			}
		}

		#region IControlledBrain Members
		public EWalkState WalkState { get { return EWalkState.Stay; } }
		public EAggressionState AggressionState { get { return EAggressionState.Aggressive; } set { } }
		public GameLiving Owner { get { return m_owner; } }
		public void Attack(GameObject target) { }
		public void Disengage() { }
		public void Follow(GameObject target) { }
		public void FollowOwner() { }
		public void Stay() { }
		public void ComeHere() { }
		public void Goto(GameObject target) { }
		public void UpdatePetWindow() { }
		public GamePlayer GetPlayerOwner() { return m_owner as GamePlayer; }
		public bool IsMainPet { get { return false; } set { } }
		#endregion
	}
}
