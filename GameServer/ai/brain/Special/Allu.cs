using DOL.AI.Brain;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
