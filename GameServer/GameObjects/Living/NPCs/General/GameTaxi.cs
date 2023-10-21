using Core.AI.Brain;

namespace Core.GS
{
	public class GameTaxi : GameNpc
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
