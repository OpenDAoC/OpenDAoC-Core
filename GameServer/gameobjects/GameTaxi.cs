using DOL.AI.Brain;

namespace DOL.GS
{
	/// <summary>
	/// 
	/// </summary>
	public class GameTaxi : GameNPC
	{
		public GameTaxi() : base()
		{
			Model = 449;
			MaxSpeedBase = 650;
			Size = 50;
			Level = 55;
			Name = "horse";
			BlankBrain brain = new BlankBrain();
			SetOwnBrain(brain);
		}
		
		public GameTaxi(INpcTemplate templateid) : base(templateid)
		{
			BlankBrain brain = new BlankBrain();
			SetOwnBrain(brain);
		}

		public override bool IsVisibleToPlayers => true;

		public override int MAX_PASSENGERS
		{
			get
			{
				return 1;
			}
		}

		public override int SLOT_OFFSET
		{
			get
			{
				return 0;
			}
		}
	}
}
