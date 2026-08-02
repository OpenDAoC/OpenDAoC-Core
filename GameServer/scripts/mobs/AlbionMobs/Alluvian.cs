using DOL.AI.Brain;

namespace DOL.GS
{
	public class Alluvian : GameNPC
	{
		public Alluvian() : base()
		{
			SetOwnBrain(new AlluvianBrain());
		}

		/// <summary>
		/// Spawns globules into the world, total of 12. this can be tweaked.
		/// </summary>
		public AlluvianGlobule SpawnGlobule()
		{
			AlluvianGlobule globulespawn = new AlluvianGlobule();
			globulespawn.Model = 928;
			globulespawn.Size = 40;
			globulespawn.Level = (byte)Util.Random(3, 4);
			globulespawn.Name = "alluvian globule";
			globulespawn.CurrentRegionID = 51;
			globulespawn.Heading = Heading;
			globulespawn.Realm = 0;
			globulespawn.MaxSpeedBase = 191;
			globulespawn.GuildName = string.Empty;
			globulespawn.X = X;
			globulespawn.Y = Y;
			globulespawn.Z = 3083;
			globulespawn.RespawnInterval = -1;
			globulespawn.BodyType = 4;
			globulespawn.RoamingRange = 500;
			globulespawn.Flags |= eFlags.FLYING;
			AlluvianGlobuleBrain brain = new AlluvianGlobuleBrain();
			brain.AggroLevel = 70;
			brain.AggroRange = 500;
			globulespawn.SetOwnBrain(brain);
			globulespawn.SetStats();
			globulespawn.AddToWorld();
			brain.WalkFromSpawn();
			return globulespawn;
		}
	}
}
