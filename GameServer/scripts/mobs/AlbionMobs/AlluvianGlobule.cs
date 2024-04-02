namespace DOL.GS
{
    /// <summary>
    /// Alluvian is a mob that spawns Alluvian Globules mobs. With a maximum of 12
    /// Remove the existing Alluvian in the water by Caifelle
    /// </summary>
    public class AlluvianGlobule : GameNPC
	{
		/// <summary>
		/// Don't allow respawn, these are spawned from the globule
		/// </summary>
		public override void StartRespawn() { }

		/// <summary>
		/// Don't allow saving to the DB. Otherwise, we make way too many
		/// Do you want Alluvian Globules? Because that's how you get Alluvian Globuals
		/// </summary>
		public override void SaveIntoDatabase() { }
	}
}
