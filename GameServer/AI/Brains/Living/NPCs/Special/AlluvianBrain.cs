namespace Core.GS.AI;

/// <summary>
/// The brains for alluvian mobs. No need to manually assign this.
/// /mob create Core.GS.Alluvian and this will be attached automatically.
/// </summary>
public class AlluvianBrain : StandardMobBrain
{
	/// <summary>
	/// Determine if we have less than 12, if not, spawn one.
	/// </summary>
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