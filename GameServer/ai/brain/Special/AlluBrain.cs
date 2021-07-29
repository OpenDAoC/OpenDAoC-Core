using DOL.GS;

namespace DOL.AI.Brain
{
    public class AlluBrain : StandardMobBrain
	{
		public override void Think()
		{
			Alluvian mob = Body as Alluvian;

			if (Alluvian.GlobuleNumber < 12)
			{
				mob.SpawnGlobule();
			}
			base.Think();
		}
	}
}
