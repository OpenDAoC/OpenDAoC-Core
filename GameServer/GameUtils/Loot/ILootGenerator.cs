namespace Core.GS
{
	public interface ILootGenerator
	{
		/// <summary>
		/// Returns the priority of this lootgenerator,
		/// if priority == 0 can be used together with other generators on the same mob.
		/// If a generator in the list of possibe generators has priority>0 only the generator with the biggest priority will be used.
		/// This can be useful if you want to define a general generator for almost all mobs and define a
		/// special one with priority>0 for a special mob that should use the default generator.
		/// </summary>
		int ExclusivePriority
		{
			get;
			set;
		}

		void Refresh(GameNpc mob);

		/// <summary>
		/// Generates a list of ItemTemplates that this mob should drop
		/// </summary>		
		/// <param name="mob">Mob that drops loot</param>
		/// <param name="killer"></param>
		/// <returns>List of ItemTemplates</returns>
		LootList GenerateLoot(GameNpc mob, GameObject killer);
	}
}
