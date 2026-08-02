using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI.Brain
{
	/// <summary>
	/// The brains for alluvian mobs. No need to manually assign this.
	/// /mob create DOL.GS.Alluvian and this will be attached automatically.
	/// </summary>
	public class AlluvianBrain : StandardMobBrain
	{
		public List<AlluvianGlobule> Globules { get; } = new();

		public override void Think()
		{
			if (Body is Alluvian mob)
			{
				Globules.RemoveAll(globule => globule == null || !globule.IsAlive);

				if (Globules.Count < 12)
					Globules.Add(mob.SpawnGlobule());
			}

			base.Think();
		}
	}
}
