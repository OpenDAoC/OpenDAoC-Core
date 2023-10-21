using Core.AI.Brain;

namespace Core.GS.AI.Brains;

public class TheurgistPetBrain : ControlledNpcBrain
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

		if (Body.FollowTarget != _target)
		{
			Body.StopFollowing();
			Body.Follow(_target, MIN_ENEMY_FOLLOW_DIST, MAX_ENEMY_FOLLOW_DIST);
		}

		if (!CheckSpells(ECheckSpellType.Offensive))
			Body.StartAttack(_target);
	}

	public override EWalkState WalkState { get => EWalkState.Stay; set { } }
	public override EAggressionState AggressionState { get => EAggressionState.Aggressive; set { } }
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