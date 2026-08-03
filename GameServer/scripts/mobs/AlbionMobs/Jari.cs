namespace DOL.GS
{
	public class Jari : GameNPC
	{
		public Jari() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12188);
			LoadTemplate(npcTemplate);

			return base.AddToWorld();
		}
		public override void ProcessDeath(GameObject killer)
		{
			switch (Util.Random(1, 2))
			{
				case 1:
					SpawnPoint.X = 490767;
					SpawnPoint.Y = 489129;
					SpawnPoint.Z = 797;
					Heading = 3614;
					break;
				case 2:
					SpawnPoint.X = 504513;
					SpawnPoint.Y = 489595;
					SpawnPoint.Z = 2430;
					Heading = 2646;
					break;
			}
			base.ProcessDeath(killer);
		}
	}
}
