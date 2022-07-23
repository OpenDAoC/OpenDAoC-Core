namespace DOL.GS
{
	public class Steinolf : GameNPC
	{
		public Steinolf() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166539);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			switch (Util.Random(1, 4))
			{
				case 1:
					SpawnPoint.X = 743068;
					SpawnPoint.Y = 1024473;
					SpawnPoint.Z = 2790;
					Heading = 2809;
					break;
				case 2:
					SpawnPoint.X = 705562;
					SpawnPoint.Y = 999511;
					SpawnPoint.Z = 2652;
					Heading = 1958;
					break;
				case 3:
					SpawnPoint.X = 725142;
					SpawnPoint.Y = 1017118;
					SpawnPoint.Z = 2824;
					Heading = 2083;
					break;
				case 4:
					SpawnPoint.X = 723803;
					SpawnPoint.Y = 1003599;
					SpawnPoint.Z = 2867;
					Heading = 3711;
					break;
			}
			base.Die(killer);
		}
	}
}
