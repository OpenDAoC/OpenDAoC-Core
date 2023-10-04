using DOL.GS;

namespace DOL.AI.Brain;

/// <summary>
/// Interface for controllable brains
/// </summary>
public interface IControlledBrain
{
    EWalkState WalkState { get; }
    EAggressionState AggressionState { get; set; }
    GameNPC Body { get; }
    GameLiving Owner { get; }
    void Attack(GameObject target);
    void Disengage();
    void Follow(GameObject target);
    void FollowOwner();
    void Stay();
    void ComeHere();
    void Goto(GameObject target);
    void UpdatePetWindow();
    GamePlayer GetPlayerOwner();
    GameNPC GetNPCOwner();
    GameLiving GetLivingOwner();
    void SetAggressionState(EAggressionState state);
    bool IsMainPet { get; set; }
}