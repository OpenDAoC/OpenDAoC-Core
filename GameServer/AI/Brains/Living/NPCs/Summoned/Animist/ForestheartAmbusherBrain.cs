using DOL.GS;

namespace DOL.AI.Brain
{
	public class ForestheartAmbusherBrain : ControlledNpcBrain
	{
		public ForestheartAmbusherBrain(GameLiving owner) : base(owner)
		{
			AggroLevel = 100;
			AggroRange = 2000;
			IsMainPet = false;
		}

		public override int AggroRange => _aggroRange;
		public override eAggressionState AggressionState => eAggressionState.Aggressive;
		public override bool CheckSpells(eCheckSpellType type) { return false; }
		public override GamePlayer GetPlayerOwner() { return m_owner as GamePlayer; }
	}
}
