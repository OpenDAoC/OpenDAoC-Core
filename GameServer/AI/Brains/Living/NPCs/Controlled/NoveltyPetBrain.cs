using Core.GS.Enums;
using Core.GS.World;

namespace Core.GS.AI.Brains
{
	public class NoveltyPetBrain : ABrain, IControlledBrain
	{
		public const string HAS_PET = "HasNoveltyPet";

		private GamePlayer m_owner;

		public NoveltyPetBrain(GamePlayer owner)
			: base()
		{
			m_owner = owner;
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

		#region Think

		public override int ThinkInterval
		{
			get { return 5000; }
		}

		public override void Think()
		{

			if (m_owner == null || 
				m_owner.IsAlive == false || 
				m_owner.Client.ClientState != EClientState.Playing || 
				Body.IsWithinRadius(m_owner, WorldMgr.VISIBILITY_DISTANCE) == false)
			{
				Body.Delete();
				Body = null;
				if (m_owner != null && m_owner.TempProperties.GetProperty<bool>(HAS_PET, false))
				{
					m_owner.TempProperties.SetProperty(HAS_PET, false);
				}
				m_owner = null;
			}
		}

		#endregion Think

		#region IControlledBrain Members
		public void SetAggressionState(EAggressionState state) { }
		public EWalkState WalkState { get { return EWalkState.Follow; } }
		public EAggressionState AggressionState { get { return EAggressionState.Passive; } set { } }
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
		public override void KillFSM(){ }
		#endregion
	}
}
