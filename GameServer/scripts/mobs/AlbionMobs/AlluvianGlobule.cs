namespace DOL.GS
{
    public class AlluvianGlobule : GameNPC
	{
		/// <summary>
		/// Don't allow respawn, these are spawned from the globule
		/// </summary>
		public override void StartRespawn() { }

		/// <summary>
		/// Roam the lake that Alluvian spawns in, at varying depths
		/// </summary>
		public override void Roam(short speed)
		{
			WalkTo(new Point3D(544196 + Util.Random(1, 3919), 514980 + Util.Random(1, 3200), 3140 + Util.Random(1, 540)), 80);
		}

		/// <summary>
		/// Don't allow saving to the DB. Otherwise, we make way too many
		/// Do you want Alluvian Globules? Because that's how you get Alluvian Globuals
		/// </summary>
		public override void SaveIntoDatabase() { }
	}
}
