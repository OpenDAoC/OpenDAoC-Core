using DOL.GS;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Defines walk state when brain is not in combat
	/// </summary>
	public enum eWalkState
	{
		/// <summary>
		/// Follow the owner
		/// </summary>
		Follow,
		/// <summary>
		/// Don't move if not in combat
		/// </summary>
		Stay,
		ComeHere,
		GoTarget,
	}

	/// <summary>
	/// Defines aggression level of the brain
	/// </summary>
	public enum eAggressionState
	{
		/// <summary>
		/// Attack any enemy in range
		/// </summary>
		Aggressive,
		/// <summary>
		/// Attack anything that attacks brain owner or owner of brain owner
		/// </summary>
		Defensive,
		/// <summary>
		/// Attack only on order
		/// </summary>
		Passive,
	}

	/// <summary>
	/// Interface for controllable brains
	/// </summary>
	public interface IControlledBrain
	{
        eWalkState WalkState { get; }
        eAggressionState AggressionState { get; set; }
        GameNPC Body { get; }
        GameLiving Owner { get; }
        void Attack(GameObject target);
        void Disengage();
        void ResumeWalkState();
        void CheckAggressionStateOnPlayerOrder();
        void Follow(GameObject target);
        void FollowOwner();
        void Stay();
        void ComeHere();
        void Goto(GameObject target);
        void UpdatePetWindow();
        GamePlayer GetPlayerOwner();
        GameNPC GetNPCOwner();
        GameLiving GetLivingOwner();
        void SetAggressionState(eAggressionState state);
        bool IsMainPet { get; set; }
	}
}
