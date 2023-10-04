using DOL.GS;

namespace DOL.AI.Brain;

public class ForestheartAmbusherBrain : ControlledNpcBrain
{
	public ForestheartAmbusherBrain(GameLiving owner) : base(owner)
	{
		AggroLevel = 100;
		AggroRange = 2000;
		IsMainPet = false;
	}

	public override int AggroRange => _aggroRange;
	public override EAggressionState AggressionState => EAggressionState.Aggressive;
	public override bool CheckSpells(ECheckSpellType type) { return false; }
	public override GamePlayer GetPlayerOwner() { return m_owner as GamePlayer; }
}