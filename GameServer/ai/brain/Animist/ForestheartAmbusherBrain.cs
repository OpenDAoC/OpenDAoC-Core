using DOL.GS;
using log4net;
using System.Reflection;


namespace DOL.AI.Brain
{
	public class ForestheartAmbusherBrain : TheurgistPetBrain, IControlledBrain
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private GameLiving m_owner;
		private GameLiving m_target;
		private bool m_melee = true;
		private bool m_active = true;

		private ushort m_range = 2000;
		public bool Melee { get { return m_melee; } set { m_melee = value; } }
		public ForestheartAmbusherBrain(GameLiving owner) : base(owner)
		{
			if (owner != null)
			{
				m_owner = owner;
			}
			AggroLevel = 100;
			AggroRange = 2000;
			IsMainPet = false;
		}

		public virtual GameNPC GetNPCOwner()
		{
		    return null;
		}
		public virtual GameLiving GetLivingOwner()
		{
		    GamePlayer player = GetPlayerOwner();
		    if (player != null)
				return player;

		    GameNPC npc = GetNPCOwner();
		    if (npc != null)
				return npc;

		    return null;
		}

		public override int ThinkInterval { get { return 1500; } }

		public override void Think() { AttackMostWanted(); }

		public void SetAggressionState(eAggressionState state) { }
		
		public override bool CheckSpells(eCheckSpellType type)
		{
			return false;
		}

		#region IControlledBrain Members
		public eWalkState WalkState { get { return WalkState; } }
		public eAggressionState AggressionState { get { return eAggressionState.Aggressive; } set { } }
		public GameLiving Owner { get { return m_owner; } }
		public void Attack(GameObject target) { }
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
