﻿using Core.AI.Brain;

namespace Core.GS;

public class Jari : GameNpc
{
	public Jari() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12188);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		//RespawnInterval = Util.Random(3600000, 7200000);

		JariBrain sbrain = new JariBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
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
		base.Die(killer);
    }
}